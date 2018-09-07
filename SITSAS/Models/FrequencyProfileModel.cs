using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class FrequencyProfileModel
    {
        public List<FrequencyProfile> ExistingFrequencyProfiles { get; set; }
        public AccessRights rights { get; set; }
    }
    public class CreateUpdateFrequencyProfileModel
    {
        public bool FrequencyProfileExists { get; set; }
        public FrequencyProfile ExistingFrequencyProfile { get; set; }
        public AccessRights rights { get; set; }

        public int Frequency { get; set; }
    }
}