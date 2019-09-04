using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class QuestionsModel
    {
        public List<Question> ExistingQuestions { get; set; }
        public AccessRights rights { get; set; }
    }
      public class CreateUpdateQuestionModel
    {
        public bool QuestionExists { get; set; }
        public Question ExistingQuestion { get; set; }

        public List<CalculationModel> AllCalculationModels { get; set; }
        public List<SubCategory> AllSubCategories { get; set; }
        
        public List<Questionnaire> AllQuestionnaires { get; set; }


        public List<StalenessProfile> AllStalenessProfiles { get; set; }
        public AccessRights rights { get; set; }

        public bool ShowStalenessProfile { get; set; }

        public Guid DefaultStalenessID { get; set; }

        public List<Question> RetiredQuestions { get; set; }
    }
    public class AnswerOneQuestionModel
    {
        public List<Location> AllLocations { get; set; }
        public List<Question> AllQuestions { get; set; }

        public AccessRights rights { get; set; }

    }

    public class FixScoresModel
    {
        public List<Location> AllLocations { get; set; }
        public AccessRights rights { get; set; }

    }

    public class AnswerOneQuestionForLocationsModel
    {
        public Question question { get; set; }
        public List<Location> Locations { get; set; }
        public DateTime selectedDate { get; set; }

        public SortedList<Guid, Result_Answers> previousAnswers { get; set; }

        public SortedList<Guid, string> allComments { get; set; }
        public AccessRights rights { get; set; }
    }
}