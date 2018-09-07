using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class QuestionnaireGroupsModel
    {
        public List<QuestionnaireGroup> ExistingQuestionnaireGroups { get; set; }
        public AccessRights rights { get; set; }
    }
      public class CreateUpdateQuestionnaireGroupModel
    {
        public bool QuestionnaireGroupExists { get; set; }
        public QuestionnaireGroup ExistingQuestionnaireGroup { get; set; }
        public List<Questionnaire> AllQuestionnaires { get; set; }
        public AccessRights rights { get; set; }
    }

    public class QuestionnaireGroupQuestionsModel
    {
        public QuestionnaireGroup QuestionnaireGroup { get; set; }
        public List<Question> AllQuestions { get; set; }
        public List<QuestionWithOrder> ExistingMapQuestions { get; set; }
        public AccessRights rights { get; set; }
    }

    public class QuestionnaireQuestionsModel
    {
        public Questionnaire Questionnaire { get; set; }
        public List<Question> AllQuestions { get; set; }
        public List<QuestionWithOrder> ExistingMapQuestions { get; set; }
        public AccessRights rights { get; set; }
    }

    public class QuestionWithOrder
    {
        public int DisplayOrder { get; set; }
        public Question Question { get; set; }
    }
}