using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class PermissionGroupModel
    {
        public List<PermissionGroup> ExistingPermissionGroups { get; set; }

    }

    public class PermissionQuickAssignedContracts
    {
        public string PKey { get; set; }
        public string ContractName { get; set; }
        public Guid ContractID { get; set; }
        public Guid PermissionGroupID { get; set; }
        public string PermissionGroupName { get; set; }
    }

    public class PermissionGroupNewModel
    {
        public bool PermissionGroupExists { get; set; }
        public PermissionGroup ExistingPermissionGroup { get; set; }
        public bool HasUsers { get; set; }

        public List<Questionnaire> AllQuestionnaires { get; set; }
        public bool AllQuestionnairesOverride { get; set; }
        public List<Questionnaire> AssignedQuestionnaires { get; set; }
        public List<Location> AllLocations { get; set; }
        public bool AllLocationsOverride { get; set; }

        public List<Area> AllAreas { get; set; }
        public bool AllAreasOverride { get; set; }
        public List<Location> AssignedLocations { get; set; }

        public List<Area> AssignedAreas { get; set; }

    }

    public class PermissionQuickAssignGroupNewModel
    {
        public IQueryable<PermissionQuickAssignedContracts> GridDef { get; set; }
        public List<PermissionQuickAssignedContracts> AssignedContracts { get; set; }
    }


    public class RolePermissionsModel
    {
        public Guid PageID { get; set; }
        public List<RolePermission> Roles { get; set; }
        public AccessRights rights { get; set; }
    }
    public class RolePermission
    {
        public Role Role { get; set; }
        public Page_Role_Mappings Mapping { get; set; }
    }
}