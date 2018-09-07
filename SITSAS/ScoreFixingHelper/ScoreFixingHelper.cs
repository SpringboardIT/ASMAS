using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SITSAS.Models;

namespace SITSAS.ScoreFixingHelper
{
    public static class ScoreFixingHelper
    {
        public static void FixScores(Guid LocationID, DateTime Date, string CompletedBy)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Location Location = context.Locations.Where(x => x.ID == LocationID).FirstOrDefault();
                if (Location != null)
                {
                    if (Location.Result_Headers.Count > 0) // otherwise no answers have been given.
                    {

                        DateTime ldNow = DateTime.Now;
                        List<Result_Headers> previousResults = Location.Result_Headers.ToList();
                        SortedList<Guid, string> LatestAnswers = new SortedList<Guid, string>();
                        List<Question> activeQuestions = context.Questions.Where(x => x.StartDate < ldNow && x.EndDate > ldNow && x.Deleted == false).ToList();
                        SystemSetting setting = context.SystemSettings.Where(x => x.Name == "FixScoresID").FirstOrDefault();
                        Guid lgID = new Guid();
                        Guid.TryParse(setting.Value, out lgID);
                        Questionnaire fixScoresQuestionnaire = context.Questionnaires.Where(x => x.ID == lgID).FirstOrDefault();
                        foreach (Result_Headers previousResult in previousResults.OrderByDescending(x => x.SelectedDate))
                        {
                            foreach (Result_Answers previousAnswer in previousResult.Result_Answers.Where(x => x.RawScore != null && x.AnswerID != null).ToList())
                            {
                                if (!LatestAnswers.ContainsKey(previousAnswer.AnswerID.Value))
                                {
                                    LatestAnswers.Add(previousAnswer.AnswerID.Value, previousAnswer.RawAnswer);
                                }
                            }
                            if (activeQuestions.Count == LatestAnswers.Count) //all active questions have been answered so stop. - if not, we have to continue incase one of those questions have been answered
                            {
                                break;
                            }
                        }
                        List<Result_Headers_Fixings> newHeaders = new List<Result_Headers_Fixings>();

                        Result_Headers_Fixings newHeader = new Result_Headers_Fixings();
                        newHeader.ID = Guid.NewGuid();
                        newHeader.CreatedDate = DateTime.Now;
                        newHeader.SelectedDate = Date;
                        newHeader.QuestionnaireID = fixScoresQuestionnaire.ID;
                        newHeader.CompletedBy = CompletedBy;
                        newHeader.LocationID = LocationID;
                        List<Result_Answers_Fixings> newAnswers = new List<Result_Answers_Fixings>();
                        foreach (KeyValuePair<Guid, string> latestAnswer in LatestAnswers)
                        {
                            //check question. if its a subquestionnaire wait until the end. as we will need to recalculate score. 

                            Question thisQuestion = activeQuestions.Where(x => x.Answers.Where(y => y.ID == latestAnswer.Key).ToList().Count > 0).FirstOrDefault();
                            if (thisQuestion.CalculationModel.eNumMapping != (int)eCalculationModels.QuestionnaireResult)
                            {
                                Result_Answers_Fixings newAnswer = new Result_Answers_Fixings();
                                newAnswer.ID = Guid.NewGuid();
                                newAnswer.HeaderID = newHeader.ID;
                                newAnswer.AnswerID = latestAnswer.Key;
                                newAnswer.RawAnswer = latestAnswer.Value;
                                newAnswer.Answer = context.Answers.Where(x => x.ID == latestAnswer.Key).FirstOrDefault();


                                //get previous header but where not a fix (as we need original dates for staleness)
                                Result_Headers previousResult = previousResults.Where(y => y.Result_Answers.Where(a => a.AnswerID == latestAnswer.Key).ToList().Count > 0 && y.QuestionnaireID != lgID).OrderByDescending(x => x.SelectedDate).FirstOrDefault();
                                if (thisQuestion.CalculationModel.eNumMapping != (int)eCalculationModels.TimeSinceARecordedDate)
                                {
                                    //the rest shouldnt change over time
                                    newAnswer.RawScore = previousResult.Result_Answers.FirstOrDefault(x => x.AnswerID == latestAnswer.Key).RawScore;
                                }
                                else
                                {
                                    newAnswer.RawScore = CalculateScoreHelper.CalculateScore(thisQuestion, latestAnswer.Value, context, null, null, null, newAnswer, newHeader);
                                }
                                string liAnswer = ((float)thisQuestion.Weighting.Value / 4).ToString();
                                decimal ldAnswer = 0;
                                if (decimal.TryParse(liAnswer, out ldAnswer))
                                {
                                    newAnswer.WeightedScore = ldAnswer;
                                }
                                else
                                {
                                    newAnswer.WeightedScore = 0;
                                }

                                // new score for time related stuff but the original date it was answered for staleness calculations. 
                                newAnswer.StalenessScore = CalculateScoreHelper.CalculateStalenessScore(newAnswer.RawScore.Value, previousResult.SelectedDate, thisQuestion.StalenessProfile, Date);
                                newAnswers.Add(newAnswer);
                            }

                        }
                        newHeaders.Add(newHeader);
                        //now all questions have been calculated. Get all of those results and tally up for subquestionnaires. 
                        foreach (KeyValuePair<Guid, string> latestAnswer in LatestAnswers)
                        {
                            Question thisQuestion = activeQuestions.Where(x => x.Answers.Where(y => y.ID == latestAnswer.Key).ToList().Count > 0).FirstOrDefault();
                            if (thisQuestion.CalculationModel.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                            {
                                Result_Headers_Fixings newsHeader = new Result_Headers_Fixings();
                                newsHeader.ID = Guid.NewGuid();
                                newsHeader.CreatedDate = DateTime.Now;
                                newsHeader.SelectedDate = Date;
                                newsHeader.QuestionnaireID = fixScoresQuestionnaire.ID;
                                newsHeader.CompletedBy = CompletedBy;
                                newsHeader.LocationID = LocationID;
                                newsHeader.ParentID = newHeader.ID;



                                Result_Answers_Fixings newAnswer = new Result_Answers_Fixings();
                                newAnswer.ID = Guid.NewGuid();
                                newAnswer.HeaderID = newHeader.ID;
                                newAnswer.AnswerID = latestAnswer.Key;

                                //get previous header but where not a fix (as we need original dates for staleness)
                                Result_Headers previousResult = previousResults.Where(y => y.Result_Answers.Where(a => a.AnswerID == latestAnswer.Key).ToList().Count > 0 && y.QuestionnaireID != lgID).OrderByDescending(x => x.SelectedDate).FirstOrDefault();
                                Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == thisQuestion.SubQuestionnaireID.Value).FirstOrDefault();
                                if (questionnaire != null)
                                {

                                    List<QuestionnaireGroup> questionnaireGroups = questionnaire.QuestionnaireGroups.ToList();
                                    int newScore = 0;
                                    foreach (QuestionnaireGroup qGroup in questionnaireGroups)
                                    {
                                        List<DataMapping> dataMapColl = context.DataMappings.Where(x => x.SecondaryID == qGroup.ID.ToString()).ToList();
                                        foreach (DataMapping dataMap in dataMapColl)
                                        {
                                            Result_Answers_Fixings ans = newAnswers.Where(x => x.Answer.QuestionID == new Guid(dataMap.PrimaryID)).FirstOrDefault();
                                            if (ans != null) //retired questions
                                            {
                                                ans.HeaderID = newsHeader.ID;
                                                newScore += ans.RawScore.Value;
                                            }

                                        }
                                    }
                                    newAnswer.RawAnswer = newScore.ToString();
                                    //check critera!

                                    if (thisQuestion.Answers.Count > 0)
                                    {
                                        foreach (Answer ans in thisQuestion.Answers.Where(x => x.Deleted == false).ToList()) //SCORE
                                        {
                                            bool MeetsCritera = false;
                                            if (ans.Answer_ScoreMappings.Count > 0)
                                            {
                                                foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings) //COMPONENT
                                                {
                                                    MeetsCritera = CalculateScoreHelper.CheckCritera(newScore, scoreMap);
                                                    if (!MeetsCritera)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                            if (MeetsCritera)
                                            {

                                                newAnswer.AnswerID = ans.ID;
                                                newAnswer.RawScore = ans.Score;
                                            }
                                        }
                                    }
                                    //newAnswer.RawScore = newScore;
                                    string liAnswer = ((float)thisQuestion.Weighting.Value / 4).ToString();
                                    decimal ldAnswer = 0;
                                    if (decimal.TryParse(liAnswer, out ldAnswer))
                                    {
                                        newAnswer.WeightedScore = ldAnswer;
                                    }
                                    else
                                    {
                                        newAnswer.WeightedScore = 0;
                                    }
                                    newAnswer.StalenessScore = CalculateScoreHelper.CalculateStalenessScore(newAnswer.RawScore.Value, previousResult.SelectedDate, thisQuestion.StalenessProfile, Date);
                                    newAnswers.Add(newAnswer);
                                   
                                }
                                context.Result_Headers_Fixings.Add(newsHeader);

                            }
                        }
                        context.Result_Headers_Fixings.Add(newHeader);
                        context.Result_Answers_Fixings.AddRange(newAnswers);
                        context.SaveChanges();
                    }
               
                }
            }
        }

        public static void FixScores(List<Guid> LocationIDs, DateTime Date, string CompletedBy)
        {
            foreach (Guid LocationID in LocationIDs)
            {
                FixScores(LocationID, Date, CompletedBy);
            }
        }
        public static void FixScores(List<Location> Locations, DateTime Date, string CompletedBy)
        {
            foreach (Location Location in Locations)
            {
                FixScores(Location.ID, Date, CompletedBy);
            }
        }

    }
}