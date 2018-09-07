using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class StalenessProfilesModel
    {
        public List<StalenessProfile> ExistingStalenessProfiles { get; set; }
        public AccessRights rights { get; set; }
        //public List<country> AllCountries { get; set; }
    }
    public class CreateUpdateStalenessProfileModel
    {
        public bool StalenessProfileExists { get; set; }
        public StalenessProfile ExistingStalenessProfile { get; set; }
        public AccessRights rights { get; set; }
        //public List<country> AllCountries { get; set; }
    }
}