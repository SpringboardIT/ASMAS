using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class DateSelectionModel
    {
        public int SelectedValue { get; set; }
        public Result_Headers header { get; set; }
        public List<QuestionnaireDropDown> DropdownOptions { get; set; }

        public Guid QuestionnaireID { get; set; }
        public Guid LocationID { get; set; }
    }
    public class QuestionnaireDropDown
    {
        public int Value { get; set; }
        public string StrValue { get; set; }
        public bool Disabled { get; set; }
    }
    public class QuestionnairesModel
    {
        public List<Questionnaire> ExistingQuestionnaires { get; set; }
        public AccessRights rights { get; set; }

        public List<Result_Headers> ContinueQuestionnaires { get; set; }

        public Guid ContinueHeaderID { get; set; }
        public Guid ContinueQuestionnaireID { get; set; }
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
        public Result_Headers ExistingHeader { get; set; }
        public List<Result_Answers> ExistingAnswers { get; set; }

        public Guid NewID { get; set; }

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