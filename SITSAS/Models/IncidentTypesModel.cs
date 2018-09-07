using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class IncidentTypesModel
    {
        public List<IncidentType> ExistingIncidentTypes { get; set; }
        public AccessRights rights { get; set; }
    }

    public class CreateUpdateIncidentTypeModel
    {
        public bool IncidentTypeExists { get; set; }
        public IncidentType ExistingIncidentType { get; set; }
        public List<Questionnaire> AllQuestionnaires { get; set; }
        public AccessRights rights { get; set; }
    }

}