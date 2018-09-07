using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class DirectoryUser
    {
        public string Name { get; set; }
        public string SN { get; set; }
        public string SamAccountName { get; set; }
        public string UserPrincipalName { get; set; }
    }

    //public class DirectoryAndPermissionCombined
    //{
    //    public DirectoryUser DirUser { get; set; }
    //    public List<Role_User_PermissionMapping> ListOfPermissionMap { get; set; }
    //    public List<Role> LstRoles { get; set; }
    //    public List<Contract> UserContracts { get; set; }
    //    public List<Location> UserLocations { get; set; }
    //    public List<Product> UserProducts { get; set; }
    //    public List<Contract> AllContracts { get; set; }
    //    public List<Location> AllLocations { get; set; }
    //    public List<Product> AllProducts { get; set; }
    //}

    public class RolesNewModel
    {
        public Role UserRole { get; set; }
        public bool UserRoleExists { get; set; }
    }

    public class AccessRights
    {
        public bool CanAdd { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool HasBeenSet { get; set; }
    }

    public class UserToGroupModel
    {
        public List<DirectoryUser> AllUsers { get; set; }
        public List<DirectoryUser> GroupUsers { get; set; }
        public PermissionGroup Group { get; set; }
    }

    public class UserToRoleModel
    {
        public List<DirectoryUser> AllUsers { get; set; }
        public List<DirectoryUser> RoleUsers { get; set; }
        public Role Role { get; set; }
    }

    
}