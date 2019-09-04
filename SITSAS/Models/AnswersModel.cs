using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class AnswersModel
    {
        public Question question { get; set; }
        public List<Answer> ExistingAnswers { get; set; }

        public SortedList<Guid, string> AnswerDescriptions { get; set; }
        public List<Answer_Operators> Operators { get; set; }

        public int CatchAllScore { get; set; }

        public AccessRights rights { get; set; }

        public List<Question> Questions { get; set; }
    }


    public class HeaderInfo
    {
        public string Username { get; set; }
        public DateTime SelectedDate { get; set; }
        public Location Location { get; set; }
        public Questionnaire Questionnaire { get; set; }
    }
    public class Results
    {
        public Questionnaire questionnaire { get; set; }
        public List<Result_Headers> Header { get; set; }
    }
   
    public class ApprovedResults
    {
        public Questionnaire questionnaire { get; set; }
        public List<Result_Headers_Fixings> Header { get; set; }
    }
}