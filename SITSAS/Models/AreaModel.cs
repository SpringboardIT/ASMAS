using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class AreaModel
    {
        public SortedList<Guid, string> ExistingAreas { get; set; }
        public AccessRights rights { get; set; }
    }
    public class CreateUpdateAreaModel
    {
        public bool AreaExists { get; set; }
        public Area ExistingArea { get; set; }
        public AccessRights rights { get; set; }
    }
    public class CreateUpdateRoleModel
    {
        public bool RoleExists { get; set; }
        public Role ExistingRole { get; set; }
        public AccessRights rights { get; set; }
    }
}