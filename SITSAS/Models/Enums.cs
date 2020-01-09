using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public enum eCalculationModels
    {
        DropDownLists = 1,
        NumericValue = 2,
        TimeSinceARecordedDate = 3,
        QuestionnaireResult = 4,
        ManualEntry = 5,
            YesNo = 6
    }
    public enum eDataMappingType
    {
        QuestionToQuestionaireGroup = 1,
        GroupToQuestionnaire = 2,
        GroupToLocation = 3,
        GroupToArea = 4,
        QuestionToQuestionnaire = 5,
        RoleToPage = 6,
        TaskToUser = 7,
        LocationToQuestionnaire = 8
    }
    public enum eOperator
    {
        GreaterThanorEqualTo = 1,
        LessThan = 2,
        LessThanOrEqualTo = 3,
        GreaterThan = 4,
        EqualTo = 5
    }
    public enum eDateUnit
    {
        Days = 1,
        Weeks = 2,
        Months = 3,
        Years = 4
    }
}

