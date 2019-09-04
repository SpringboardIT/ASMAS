using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class IndexModel
    {
        public List<Location> locations { get; set; }
        public List<Questionnaire> questionnaires { get; set; }
        public Guid SelectedQuestionnaire { get; set; }
        public FrequencyProfile freqProfile { get; set; }
    }
}