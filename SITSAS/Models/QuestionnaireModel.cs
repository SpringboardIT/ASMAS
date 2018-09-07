using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class QuestionnairesModel
    {
        public List<Questionnaire> ExistingQuestionnaires { get; set; }
        public AccessRights rights { get; set; }
    }
      public class CreateUpdateQuestionnaireModel
    {
        public bool QuestionnaireExists { get; set; }
        public Questionnaire ExistingQuestionnaire { get; set; }
        public AccessRights rights { get; set; }

        public List<FrequencyProfile> AllFrequencyProfiles { get; set; }
    }

    public class AnswerQuestionnaireModel
    {
        public Questionnaire questionnaire { get; set; }
        public List<QuestionnaireQuestions> questionnaireGroup { get; set; }

        public List<Location> AllLocations { get; set; }
        public bool IsSub { get; set; }

        public Guid IncidentTypeID { get; set; }
        public AccessRights rights { get; set; }

    }
    public class QuestionnaireGroupQuestions
    {
        public QuestionnaireGroup questionnaireGroup { get; set; }
        public List<QuestionnaireGroupCategory> Categories { get; set; }
        public AccessRights rights { get; set; }
    }

    public class QuestionnaireQuestions
    {
        public List<QuestionnaireCategory> Categories { get; set; }
        public AccessRights rights { get; set; }
    }
    public class QuestionnaireCategory
    {
        public Category Category { get; set; }
        public List<QuestionnaireSubCategories> SubCategories { get; set; }
        public AccessRights rights { get; set; }
    }
    public class QuestionnaireGroupCategory
    {
        public Category Category { get; set; }
        public List<QuestionnaireSubCategories> SubCategories { get; set; }
        public AccessRights rights { get; set; }
    }
    public class QuestionnaireSubCategories
    {
        public SubCategory SubCategory { get; set; }
        public List<QuestionWithOrder> questions { get; set; }
        public AccessRights rights { get; set; }
    }

    public class PreviewQuestionnaireModel
    {
        public string Data { get; set; }
    }
   
}