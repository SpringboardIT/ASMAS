using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SITSAS.Models
{
    public static class CalculateScoreHelper
    {
        public static int CalculateScore(Question question, string RawAnswer, SITSASEntities context, FormCollection form, Result_Answers ranswer = null, Result_Headers header = null, Result_Answers_Fixings franswer = null, Result_Headers_Fixings fheader = null)
        {
            bool IsFixing = false;
            if(header == null && ranswer == null && fheader != null && franswer != null)
            {
                IsFixing = true;
            }

            switch (question.CalculationModel.eNumMapping)
            {
                case (int)eCalculationModels.DropDownLists:
                    {
                        if (question.Answers.Count > 0)
                        {
                            Guid lgAnswer = new Guid();
                            Guid.TryParse(RawAnswer, out lgAnswer);
                            Answer answer = question.Answers.FirstOrDefault(x => x.ID == lgAnswer);
                            if (answer != null)
                            {
                                if (IsFixing)
                                {
                                    franswer.AnswerID = answer.ID;
                                    franswer.RawAnswer = answer.Description;
                                }
                                else
                                {
                                    ranswer.AnswerID = answer.ID;
                                    ranswer.RawAnswer = answer.Description;
                                }

                                return answer.Score;
                            }
                        }

                        break;
                    }
                case (int)eCalculationModels.NumericValue:
                    {
                        int liAnswer = 0;
                        int.TryParse(RawAnswer, out liAnswer);
                        if (question.Answers.Count > 0)
                        {
                            foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList().OrderBy(x => x.Answer_ScoreMappings.FirstOrDefault().Value)) //SCORE
                            {
                                bool MeetsCritera = false;
                                if (ans.Answer_ScoreMappings.Count > 0)
                                {
                                    foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings.OrderBy(x => x.Value)) //COMPONENT
                                    {
                                        MeetsCritera = CheckCritera(liAnswer, scoreMap);
                                    }
                                }
                                if (MeetsCritera)
                                {
                                    if (IsFixing)
                                    {
                                        franswer.AnswerID = ans.ID;
                                    }
                                    else
                                    {
                                        ranswer.AnswerID = ans.ID;
                                    }
                                    return ans.Score;
                                }

                            }
                        }
                        break;
                    }
                case (int)eCalculationModels.QuestionnaireResult:
                    {
                        int Score = 0;
                        if (question.SubQuestionnaireID.HasValue)
                        {
                            Guid SubQuestionnaireID = question.SubQuestionnaireID.Value;
                            if (SubQuestionnaireID != null)
                            {

                                //create sub results header
                                Result_Headers subrHeader = new Result_Headers();
                                subrHeader.ID = Guid.NewGuid();
                                subrHeader.QuestionnaireID = SubQuestionnaireID;
                                if (IsFixing)
                                {
                                    subrHeader.CompletedBy = fheader.CompletedBy;
                                    subrHeader.CreatedDate = fheader.CreatedDate;
                                    subrHeader.LocationID = fheader.LocationID;
                                    subrHeader.SelectedDate = fheader.SelectedDate;
                                    subrHeader.ParentID = fheader.ID;
                                }
                                else
                                {
                                    subrHeader.CompletedBy = header.CompletedBy;
                                    subrHeader.CreatedDate = header.CreatedDate;
                                    subrHeader.LocationID = header.LocationID;
                                    subrHeader.SelectedDate = header.SelectedDate;
                                    subrHeader.ParentID = header.ID;
                                }
                          
                                List<Question> subQuestions = GetQuestionsForQuestionnaireFromForm(form, context, SubQuestionnaireID);
                                foreach (Question subQuestion in subQuestions)
                                {
                                    Result_Answers subranswer = new Result_Answers();
                                    subranswer.ID = Guid.NewGuid();
                                    subranswer.HeaderID = subrHeader.ID;
                                    if (!string.IsNullOrEmpty(form[subQuestion.ID.ToString()]))
                                    {
                                        string lsAnswer = form[subQuestion.ID.ToString()];
                                        subranswer.RawAnswer = lsAnswer;
                                        int subItemScore = CalculateScore(subQuestion, lsAnswer, context, form, subranswer, subrHeader);
                                        subranswer.RawScore = subItemScore;
                                        Score += subItemScore;
                                        if (!string.IsNullOrEmpty(question.ID.ToString() + "-NOTES"))
                                        {
                                            subranswer.Comments = form[question.ID.ToString() + "-NOTES"];
                                            if (subranswer.Comments == "")
                                            {
                                                subranswer.Comments = null; //as we're writing string.empty. 
                                            }
                                        }
                                    }
                                    string liAnswer = ((float)subQuestion.Weighting.Value / 4).ToString();
                                    decimal ldAnswer = 0;
                                    if (decimal.TryParse(liAnswer, out ldAnswer))
                                    {
                                        subranswer.WeightedScore = ldAnswer;
                                    }
                                    else
                                    {
                                        subranswer.WeightedScore = 0;
                                    }
                                    //subranswer.WeightedScore = subranswer.RawScore * (subQuestion.Weighting / 4);
                                    subranswer.StalenessScore =   CalculateScoreHelper.CalculateStalenessScore(subranswer.RawScore.Value, subrHeader.SelectedDate, question.StalenessProfile, DateTime.Now.Date);
                                    context.Result_Answers.Add(subranswer);



                                }
                                context.Result_Headers.Add(subrHeader);
                            }


                        }
                        if (IsFixing)
                        {
                            franswer.RawAnswer = Score.ToString();
                        }
                        else
                        {
                            ranswer.RawAnswer = Score.ToString();
                        }
                      
                        if (question.Answers.Count > 0)
                        {
                            foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList().OrderBy(x => x.Answer_ScoreMappings.FirstOrDefault().Value)) //SCORE
                            {
                                bool MeetsCritera = false;
                                if (ans.Answer_ScoreMappings.Count > 0)
                                {
                                    foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings.OrderBy(x => x.Value)) //COMPONENT
                                    {
                                        MeetsCritera = CheckCritera(Score, scoreMap);
                                        if (!MeetsCritera)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (MeetsCritera)
                                {
                                    if (IsFixing)
                                    {
                                        franswer.AnswerID = ans.ID;
                                    }
                                    else
                                    {
                                        ranswer.AnswerID = ans.ID;
                                    }
                                    return ans.Score;
                                }
                            }
                        }

                        break;

                    }
                case (int)eCalculationModels.TimeSinceARecordedDate:
                    {
                        DateTime ldAnswer = DateTime.MinValue;
                        DateTime.TryParse(RawAnswer, out ldAnswer);
                        if (question.Answers.Count > 0)
                        {
                            foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList().OrderBy(x => x.Answer_ScoreMappings.FirstOrDefault().DateUnit)) //SCORE
                            {
                                bool MeetsCritera = false;
                                if (ans.Answer_ScoreMappings.Count > 0)
                                {
                                    foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings.OrderBy(x => x.DateUnit)) //COMPONENT
                                    {
                                        if (IsFixing)
                                        {
                                            MeetsCritera = CheckCritera(ldAnswer, scoreMap, fheader.SelectedDate);
                                        }
                                        else
                                        {
                                            MeetsCritera = CheckCritera(ldAnswer, scoreMap, header.SelectedDate);
                                        }

                                        if (!MeetsCritera)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (MeetsCritera)
                                {
                                    if (IsFixing)
                                    {
                                        franswer.AnswerID = ans.ID;
                                    }
                                    else
                                    {
                                        ranswer.AnswerID = ans.ID;
                                    }
                                    return ans.Score;
                                }
                            }
                        }
                        break;
                    }
                case (int)eCalculationModels.ManualEntry:
                    {
                        int liAnswer = 0;
                        int.TryParse(RawAnswer, out liAnswer);
                        Answer manAns = question.Answers.Where(x => x.Deleted == true && x.Description == "Manual Entry").FirstOrDefault();
                        if (manAns != null)
                        {
                            if (IsFixing)
                            {
                                franswer.AnswerID = manAns.ID;
                            }
                            else
                            {
                                ranswer.AnswerID = manAns.ID;
                            }
                        }
                        return liAnswer;
                    }
                case (int)eCalculationModels.YesNo:
                    {
                        if (question.Answers.Count > 0)
                        {
                            Guid lgAnswer = new Guid();
                            Guid.TryParse(RawAnswer, out lgAnswer);
                            Answer answer = question.Answers.FirstOrDefault(x => x.ID == lgAnswer);
                            if (answer != null)
                            {
                                if (IsFixing)
                                {
                                    franswer.AnswerID = answer.ID;
                                    franswer.RawAnswer = answer.Description;
                                }
                                else
                                {
                                    ranswer.AnswerID = answer.ID;
                                    ranswer.RawAnswer = answer.Description;
                                }

                                return answer.Score;
                            }
                        }

                        break;
                    }
            }
            //Answer defAns = question.Answers.Where(x => x.Deleted == true && x.Description == "Answer does not meet any calculations.").FirstOrDefault(); //DEFAULT ANSWER
            //if (defAns != null)
            //{
            //    ranswer.AnswerID = defAns.ID;
            //}
            return question.DefaultValue;
        }
        public static bool CheckCritera(DateTime ldAnswer, Answer_ScoreMappings scoreMap, DateTime QuestionnaireDate)
        {
            DateTime myDate = DateTime.MinValue;
            switch (scoreMap.DateUnit)
            {
                case "days":
                    {
                        myDate = QuestionnaireDate.AddDays(scoreMap.Value * -1);
                        break;
                    }
                case "weeks":
                    {
                        myDate = QuestionnaireDate.AddDays((scoreMap.Value * 7) * -1);
                        break;
                    }
                case "months":
                    {
                        myDate = QuestionnaireDate.AddMonths((int)scoreMap.Value * -1);
                        break;
                    }
                case "years":
                    {
                        myDate = QuestionnaireDate.AddYears((int)scoreMap.Value * -1);
                        break;
                    }

            }
            bool MeetsCritera = false;
            //switch (scoreMap.Answer_Operators.eNumMapping)
            //{
                //case (int)eOperator.EqualTo:
                //    {
                //        if (ldAnswer.Date == myDate.Date)
                //        {
                //            MeetsCritera = true;
                //        }
                //        else
                //        {
                //            MeetsCritera = false;
                //        }
                //        break;
                //    }
                //case (int)eOperator.GreaterThan:
                //    {
                //        if (ldAnswer.Date > myDate.Date)
                //        {
                //            MeetsCritera = true;
                //        }
                //        else
                //        {
                //            MeetsCritera = false;
                //        }
                //        break;
                //    }
                //case (int)eOperator.GreaterThanorEqualTo:
                //    {
                        if (ldAnswer.Date >= myDate.Date && ldAnswer.Date <= QuestionnaireDate.Date)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        //break;
                    //}
                    //case (int)eOperator.LessThan:
                    //    {
                    //        if (ldAnswer.Date < myDate.Date)
                    //        {
                    //            MeetsCritera = true;
                    //        }
                    //        else
                    //        {
                    //            MeetsCritera = false;
                    //        }
                    //        break;
                    //    }
                    //case (int)eOperator.LessThanOrEqualTo:
                    //    {
                    //        if (ldAnswer.Date <= myDate.Date)
                    //        {
                    //            MeetsCritera = true;
                    //        }
                    //        else
                    //        {
                    //            MeetsCritera = false;
                    //        }
                    //        break;
                    //    }
            //}
            return MeetsCritera;
        }
        public static bool CheckCritera(int Score, Answer_ScoreMappings scoreMap)
        {
            bool MeetsCritera = false;
            switch (scoreMap.Answer_Operators.eNumMapping)
            {
                case (int)eOperator.EqualTo:
                    {
                        if (Score == scoreMap.Value)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        break;
                    }
                case (int)eOperator.GreaterThan:
                    {
                        if (Score > scoreMap.Value)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        break;
                    }
                case (int)eOperator.GreaterThanorEqualTo:
                    {
                        if (Score >= scoreMap.Value)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        break;
                    }
                case (int)eOperator.LessThan:
                    {
                        if (Score < scoreMap.Value)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        break;
                    }
                case (int)eOperator.LessThanOrEqualTo:
                    {
                        if (Score <= scoreMap.Value)
                        {
                            MeetsCritera = true;
                        }
                        else
                        {
                            MeetsCritera = false;
                        }
                        break;
                    }
            }
            return MeetsCritera;
        }
        public static List<Question> GetQuestionsForQuestionnaireFromForm(FormCollection form, SITSASEntities context, Guid questionnaireID)
        {
            List<Question> model = new List<Question>();
            Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == questionnaireID).FirstOrDefault();
            List<QuestionnaireGroup> questionnaireGroups = new List<QuestionnaireGroup>();

            //foreach (QuestionnaireGroup group in questionnaire.QuestionnaireGroups)
            //{
                List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.SecondaryID == questionnaireID.ToString()).ToList();
                foreach (DataMapping dataMap in dataMaps)
                {
                    Question question = context.Questions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault();
                    if (question != null)
                    {
                        if (form.AllKeys.ToList().Contains(question.ID.ToString()))
                        {
                            model.Add(question);
                        }
                    }
                }
            //}


            return model;
        }

        public static decimal CalculateStalenessScore(decimal Score, DateTime QuestionDate, StalenessProfile profile, DateTime CurrentDate)
        {
            decimal newScore = 0;
            decimal NumberOfDaysSinceAnswer = (decimal)(CurrentDate - QuestionDate).TotalDays;
            if (NumberOfDaysSinceAnswer <= profile.StaticDays)
            {
                if (Score > 0)
                {
                    return Score / Score;
                }
                else
                {
                    return Score;
                }
          
            }
            else
            {
                if (NumberOfDaysSinceAnswer >= profile.DaysUntilFinalScore.Value)
                {
                    if (Score > 0)
                    {
                        return profile.FinalScore.Value / Score;
                    }
                    else
                    {
                        return profile.FinalScore.Value;
                    }
                }
                decimal NumberOfDaysValueChangesOver = (profile.DaysUntilFinalScore.Value - profile.StaticDays.Value); //should be 30!!!
                decimal DifferenceInScores = profile.FinalScore.Value - Score; //should be 4
                decimal DailyIncrements = (DifferenceInScores / NumberOfDaysValueChangesOver); //should be 0.13
                newScore = Score + (DailyIncrements * (NumberOfDaysSinceAnswer - profile.StaticDays.Value));
            }
            if (Score > 0)
            {
                return newScore / Score;
            }
            else
            {
                return newScore;
            }
        }
    }
}