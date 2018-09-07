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
    }

    public class LocationItem
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string AreaName { get; set; }
    }
      public class CreateUpdateLocationModel
    {
        public bool LocationExists { get; set; }
        public Location ExistingLocation { get; set; }
        public List<Area> AllAreas { get; set; }
        public AccessRights rights { get; set; }
    }
}