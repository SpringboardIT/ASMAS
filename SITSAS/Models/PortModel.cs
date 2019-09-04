using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class LocationsModel
    {
        public List<LocationItem> ExistingLocations { get; set; }
        public List<Area> AllAreas { get; set; }
        public AccessRights rights { get; set; }
        public List<Location_PermissionGroupTemplate> AllLocationPermissionGroups { get; set; }
    }

    public class LocationItem
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string AreaName { get; set; }

        public string User { get; set; }

        public SortedList<Guid, string> PermissionGroupToUsername { get; set; }

    }
      public class CreateUpdateLocationModel
    {
        public bool LocationExists { get; set; }
        public Location ExistingLocation { get; set; }
        public List<Area> AllAreas { get; set; }
        public AccessRights rights { get; set; }

        public List<DirectoryUser> AllUsers { get; set; }
        public List<Location_PermissionGroupTemplate> AllLocationPermissionGroups { get; set; }
        public SortedList<Guid, List<string>> PermissionGroupToUsername { get; set; }

    }
    public class ReviewLocationModel
    {
        public Location location { get; set; }
        public List<Result_Headers> headers { get; set; }
        public List<Question> questions { get; set; }
    }
    public class QuestionReviewLocationModel
    {
        public Location location { get; set; }
        public List<Result_Answers> answers { get; set; }
        public Question question { get; set; }
    }
}