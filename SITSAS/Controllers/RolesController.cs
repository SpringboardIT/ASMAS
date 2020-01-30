using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SITSAS.Models;

namespace SITSAS.Controllers
{

    public class RolesController : Controller
    {
        //
        // GET: /Roles/

        public ActionResult Index()
        {
            List<Role> Model = new List<Role>();
            AccessRights Rights = ContextModel.DetermineAccess();
            using (SITSASEntities Context = new SITSASEntities())
            {
                Model = Context.Roles.Where(x => x.Deleted == false || x.Deleted == null).OrderBy(x => x.RoleName).ToList();
            }
            var tuple = new Tuple<List<Role>, AccessRights>(Model, Rights);
            return View(tuple);
        }

        public ActionResult EditRole(Guid? ID)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    RolesNewModel model = new RolesNewModel();
                    AccessRights Rights = ContextModel.DetermineAccess();
                    model.UserRoleExists = false;
                    if (ID.HasValue)
                    {
                        model.UserRole = context.Roles.Include("Role_Permissions").Where(x => x.RoleID == ID.Value).FirstOrDefault();
                        model.UserRoleExists = true;
                    }
                    else
                    {
                        model.UserRoleExists = false;
                    }

                    var tuple = new Tuple<RolesNewModel, AccessRights>(model, Rights);
                    return View(tuple);
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }

        }

        public ActionResult CreateNewUserRole(FormCollection collection)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    string RoleName = collection["RoleName"];
                    Guid NewRoleGuid = new Guid();
                    string lsRoleID = collection["ExistingUserRole"];
                    Guid.TryParse(lsRoleID, out NewRoleGuid);
                    string cEdit = collection["PermEdit"];
                    string cAdd = collection["PermAdd"];
                    string cView = collection["PermView"];
                    string cDelete = collection["PermDelete"];
                    bool boolEdit = false;
                    bool booladd = false;
                    bool boolView = false;
                    bool boolDelete = false;
                    if (cEdit == null)
                    {
                        boolEdit = false;
                    }
                    else
                    {
                        if (cEdit.ToUpper() == "ON")
                        {
                            boolEdit = true;
                        }
                    }
                    if (cAdd == null)
                    {
                        booladd = false;
                    }
                    else
                    {
                        if (cAdd.ToUpper() == "ON")
                        {
                            booladd = true;
                        }
                    }
                    if (cView == null)
                    {
                        boolView = false;
                    }
                    else
                    {
                        if (cView.ToUpper() == "ON")
                        {
                            boolView = true;
                        }
                    }
                    if (cDelete == null)
                    {
                        boolDelete = false;
                    }
                    else
                    {
                        if (cDelete.ToUpper() == "ON")
                        {
                            boolDelete = true;
                        }
                    }

                    if (NewRoleGuid != new Guid())
                    {
                        Role existingRole = context.Roles.Where(x => x.RoleID == NewRoleGuid).FirstOrDefault();
                        Role_Permissions existingPermission = context.Role_Permissions.Where(x => x.RoleID == NewRoleGuid).FirstOrDefault();
                        bool NewPermissionRequired = false;
                        if (existingPermission == null)
                        {
                            existingPermission = new Role_Permissions();
                            NewPermissionRequired = true;
                        }
                        existingRole.RoleName = RoleName;
                        existingRole.Deleted = false;
                        existingPermission.CanEdit = boolEdit;
                        existingPermission.CanDelete = boolDelete;
                        existingPermission.CanAdd = booladd;
                        existingPermission.CanView = boolView;
                        //AddToAudit("Update user Role: " + existingRole.RoleID);
                        if (NewPermissionRequired == true)
                        {
                            existingPermission.PermissionsID = Guid.NewGuid();
                            existingPermission.RoleID = existingRole.RoleID;
                            context.Role_Permissions.Add(existingPermission);
                        }
                    }
                    else
                    {
                        Role NewRole = new Role();
                        NewRole.RoleID = Guid.NewGuid();
                        NewRole.RoleName = RoleName;
                        NewRole.Deleted = false;
                        context.Roles.Add(NewRole);
                        //AddToAudit("New user Role: " + NewRole.RoleID);
                        Role_Permissions newPermission = new Role_Permissions();
                        newPermission.PermissionsID = Guid.NewGuid();
                        newPermission.RoleID = NewRole.RoleID;
                        newPermission.CanEdit = boolEdit;
                        newPermission.CanAdd = booladd;
                        newPermission.CanDelete = boolDelete;
                        newPermission.CanView = boolView;
                        //AddToAudit("New Permission: " + newPermission.PermissionsID);
                        context.Role_Permissions.Add(newPermission);
                    }
                    context.SaveChanges();
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
            return RedirectToAction("ListOfRoles");

        }



        public ActionResult deleteRoleType(FormCollection form)
        {
            if (ContextModel.AccessToPage())
            {
                Guid RoleID = new Guid();
                string lsRoleID = form["ExistingUserRole"];
                Guid.TryParse(lsRoleID, out RoleID);
                using (SITSASEntities context = new SITSASEntities())
                {
                    if (RoleID != new Guid())
                    {
                        Role existingRole = context.Roles.Include("Permissions").Where(x => x.RoleID == RoleID).FirstOrDefault();
                        existingRole.Deleted = true;
                        //AddToAudit("Delete Role Type: " + existingRole.RoleID);
                    }

                    context.SaveChanges();
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
            return RedirectToAction("ListOfRoles");
        }


        public ActionResult RolesDeleted()
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    List<Role> model = new List<Role>();
                    model = context.Roles.Where(x => x.Deleted == true).ToList();
                    var tuple = new Tuple<List<Role>, AccessRights>(model, Rights);
                    return View(tuple);
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }

        }

        public ActionResult RestoreRoles(Guid? ID)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    RolesNewModel model = new RolesNewModel();
                    model.UserRoleExists = false;
                    if (ID.HasValue)
                    {
                        model.UserRole = context.Roles.Where(x => x.RoleID == ID.Value).FirstOrDefault();
                        model.UserRole.Deleted = false;
                        context.SaveChanges();
                        //AddToAudit("Role Restore: " + model.UserRole.RoleID);
                    }
                    return RedirectToAction("ListOfRoles");
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }

        //public ActionResult AddLocation(string UserID, Guid LocationID)
        //{
        //    if (UserID != null && LocationID != null)
        //    {
        //        using (SITSASEntities context = new SITSASEntities())
        //        {
        //            if (context != null)
        //            {
        //                List<DataMapping> mapping = context.DataMappings.Where(x => x.SecondaryID == LocationID && x.PrimaryID == UserID).ToList();
        //                if (mapping.Count == 0)
        //                {
        //                    DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.UsersToLocation).FirstOrDefault();
        //                    if (Dtype != null)
        //                    {
        //                        Guid mptID = Dtype.ID;
        //                        if (mptID != null)
        //                        {
        //                            DataMapping dMap = new DataMapping();
        //                            dMap.ID = Guid.NewGuid();
        //                            dMap.DataMappingTypeID = mptID;
        //                            dMap.PrimaryID = UserID;
        //                            dMap.SecondaryID = LocationID;
        //                            context.DataMappings.Add(dMap);
        //                            //AddToAudit("New Location Data Mapping: " + dMap.ID);
        //                            context.SaveChanges();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return RedirectToAction("AssignUserRoles", "User", new { SID = UserID });
        //}

        public ActionResult RoleDeleteProduct(string UserID, Guid? ProductID)
        {
            if (ProductID.HasValue)
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    DataMapping mapping = context.DataMappings.Where(x => x.SecondaryID == ProductID.ToString() && x.PrimaryID == UserID).FirstOrDefault();
                    if (mapping != null)
                    {
                        context.DataMappings.Remove(mapping);
                        //AddToAudit("Role Delete Product (Mapping ID): " + mapping.ID);
                        context.SaveChanges();
                    }
                }
            }
            return RedirectToAction("AssignUserRoles", "User", new { SID = UserID });
        }



 


        public ActionResult AddQuestionnaireToGroup(string GroupID, Guid QuestionnaireID)
        {
            if (GroupID != null && QuestionnaireID != null)
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    if (context != null)
                    {
                        List<DataMapping> mapping = context.DataMappings.Where(x => x.SecondaryID == QuestionnaireID.ToString() && x.PrimaryID == GroupID).ToList();
                        if (mapping.Count == 0)
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).FirstOrDefault();
                            if (Dtype != null)
                            {
                                Guid mptID = Dtype.ID;
                                if (mptID != null)
                                {
                                    DataMapping dMap = new DataMapping();
                                    dMap.ID = Guid.NewGuid();
                                    dMap.DataMappingTypeID = mptID;
                                    dMap.PrimaryID = GroupID;
                                    dMap.SecondaryID = QuestionnaireID.ToString();
                                    dMap.OverrideAll = false;
                                    context.DataMappings.Add(dMap);
                                    context.SaveChanges();
                                    //AddToAudit("Add Questionnaire to Group: " + dMap.ID);
                                }
                            }
                        }
                    }
                }
            }
            return RedirectToAction("PermissionGroupCreateNewScreen", "Roles", new { ID = GroupID });
        }

        public ActionResult AddAreaToGroup(string GroupID, Guid AreaID)
        {
            if (GroupID != null && AreaID != null)
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    if (context != null)
                    {
                        List<DataMapping> mapping = context.DataMappings.Where(x => x.SecondaryID == AreaID.ToString() && x.PrimaryID == GroupID).ToList();
                        if (mapping.Count == 0)
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToArea).FirstOrDefault();
                            if (Dtype != null)
                            {
                                Guid mptID = Dtype.ID;
                                if (mptID != null)
                                {
                                    DataMapping dMap = new DataMapping();
                                    dMap.ID = Guid.NewGuid();
                                    dMap.DataMappingTypeID = mptID;
                                    dMap.PrimaryID = GroupID;
                                    dMap.SecondaryID = AreaID.ToString();
                                    dMap.OverrideAll = false;
                                    context.DataMappings.Add(dMap);
                                    context.SaveChanges();
                                    //AddToAudit("Add Area to Group: " + dMap.ID);
                                }
                            }
                        }
                    }
                }
            }
            return RedirectToAction("PermissionGroupCreateNewScreen", "Roles", new { ID = GroupID });
        }

        public ActionResult AddLocationToGroup(string GroupID, Guid LocationID)
        {
            if (GroupID != null && LocationID != null)
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    if (context != null)
                    {
                        List<DataMapping> mapping = context.DataMappings.Where(x => x.SecondaryID == LocationID.ToString() && x.PrimaryID == GroupID).ToList();
                        if (mapping.Count == 0)
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToLocation).FirstOrDefault();
                            if (Dtype != null)
                            {
                                Guid mptID = Dtype.ID;
                                if (mptID != null)
                                {
                                    DataMapping dMap = new DataMapping();
                                    dMap.ID = Guid.NewGuid();
                                    dMap.DataMappingTypeID = mptID;
                                    dMap.PrimaryID = GroupID;
                                    dMap.SecondaryID = LocationID.ToString();
                                    dMap.OverrideAll = false;
                                    context.DataMappings.Add(dMap);
                                    context.SaveChanges();
                                    //AddToAudit("Add Area to Group: " + dMap.ID);
                                }
                            }
                        }
                    }
                }
            }
            return RedirectToAction("PermissionGroupCreateNewScreen", "Roles", new { ID = GroupID });
        }



        /// <summary>
        /// //////////////////////////
        /// </summary>
        /// <returns></returns>
        public ActionResult PermissionGroup()
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    PermissionGroupModel model = new PermissionGroupModel();
                    model.ExistingPermissionGroups = context.PermissionGroups.Where(x => x.Deleted == false && x.Hidden != true).OrderBy(x => x.GroupName).ToList();
                    var tuple = new Tuple<PermissionGroupModel, AccessRights>(model, Rights);
                    return View(tuple);
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }
        public ActionResult PermissionGroupCreateNewScreen(Guid? ID)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities veraContext = new SITSASEntities())
                {


                    using (SITSASEntities context = new SITSASEntities())
                    {
                        AccessRights Rights = ContextModel.DetermineAccess();
                        PermissionGroupNewModel model = new PermissionGroupNewModel();
                        model.PermissionGroupExists = false;
                        model.AllQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).OrderBy(x => x.Name).ToList();
                        model.AllLocations = GetLocationsWhereAreaIsLive(context, false);
                        model.AllAreas = context.Areas.Where(x => x.Deleted == false).OrderBy(x => x.Name).ToList();
                        model.AllQuestionnairesOverride = false;
                        model.AllLocationsOverride = false;
                        model.AssignedQuestionnaires = new List<Questionnaire>();
                        model.AssignedLocations = new List<Location>();
                        model.AssignedAreas = new List<Area>();

                        if (ID.HasValue)
                        {
                            string GroupID = ID.ToString();
                            List<DataMapping> AllGroupMappings = context.DataMappings.Include("DataMappingType").Where(x => x.PrimaryID == GroupID).ToList();

                            model.ExistingPermissionGroup = context.PermissionGroups.Where(x => x.ID == ID.Value).FirstOrDefault();

                            model.HasUsers = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == ID).ToList().Count > 0; //change this to users assigned to group

                            model.AllQuestionnairesOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.PrimaryID == GroupID && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).ToList().Count > 0;
                            model.AllAreasOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.PrimaryID == GroupID && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToArea).ToList().Count > 0;
                            model.AllLocationsOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.PrimaryID == GroupID && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToLocation).ToList().Count > 0;

                            List<DataMapping> contMappings = AllGroupMappings.Where(x => x.OverrideAll == false && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).ToList();
                            List<DataMapping> LocationMappings = AllGroupMappings.Where(x => x.OverrideAll == false && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToLocation).ToList();
                            List<DataMapping> AreaMappings = AllGroupMappings.Where(x => x.OverrideAll == false && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToArea).ToList();
                            foreach (DataMapping contMapping in contMappings)
                            {
                                Questionnaire quest = model.AllQuestionnaires.Where(x => x.ID == new Guid(contMapping.SecondaryID)).FirstOrDefault();
                                if (quest != null)
                                {
                                    model.AssignedQuestionnaires.Add(quest);
                                    model.AllQuestionnaires.Remove(quest);
                                }

                            }

                            foreach (DataMapping contMapping in LocationMappings)
                            {
                                Guid cID = new Guid();
                                Guid.TryParse(contMapping.SecondaryID, out cID);
                                Location con = model.AllLocations.Where(x => x.ID == cID).FirstOrDefault();
                                if (con != null)
                                {
                                    model.AssignedLocations.Add(con);
                                    model.AllLocations.Remove(con);
                                }

                            }

                            foreach (DataMapping contMapping in AreaMappings)
                            {
                                Guid cID = new Guid();
                                Guid.TryParse(contMapping.SecondaryID, out cID);
                                Area con = model.AllAreas.Where(x => x.ID == cID).FirstOrDefault();
                                if (con != null)
                                {
                                    model.AssignedAreas.Add(con);
                                    model.AllAreas.Remove(con);
                                }

                            }


                            model.PermissionGroupExists = true;
                        }
                        else
                        {
                            model.PermissionGroupExists = false;
                        }

                        model.AllPermissionGroups = context.PermissionGroups.Where(x => x.Deleted == false && x.Hidden != true).ToList();
                        if (model.ExistingPermissionGroup != null)
                        {
                            model.AllPermissionGroups.Remove(model.ExistingPermissionGroup);
                        }
                        var tuple = new Tuple<PermissionGroupNewModel, AccessRights>(model, Rights);
                        return View(tuple);
                    }
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }

        public List<Location> GetLocationsWhereAreaIsLive(SITSASEntities context, bool LimitByUser)
        {
            List<Location> model = new List<Location>();
            List<Location> tempLocations = new List<Location>();
            if (LimitByUser)
            {
                tempLocations = context.GetLocationsForUser(ContextModel.GetCurrentUserSID(), false).ToList();
            }
            else
            {
                tempLocations = context.Locations.Where(x => x.Deleted == false).ToList();
            }



            List<Area> DeletedAreas = new List<Area>();
            using (SITSASEntities VERAcontext = new SITSASEntities())
            {
                DeletedAreas = VERAcontext.Areas.Where(x => x.Deleted == true).ToList();
            }
            foreach (Location Location in tempLocations)
            {
                if (DeletedAreas.Where(x => x.ID == Location.AreaID).ToList().Count == 0)
                {
                    model.Add(Location);
                }
            }
            return model;
        }

        public ActionResult AddUsersToGroup(Guid GroupID)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    UserToGroupModel model = new UserToGroupModel();
                    model.Group = context.PermissionGroups.Where(x => x.ID == GroupID).FirstOrDefault();
                    if (HttpContext.Session["DirectoryUsers"] != null)
                    {
                        model.AllUsers = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                    }
                    else
                    {
                        model.AllUsers = ContextModel.GetUsersFromActiveDirectory(context);
                        HttpContext.Session["DirectoryUsers"] = model.AllUsers;

                    }
                    List<PermissionGroup_User_Mapping> Mappings = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == GroupID).ToList();
                    model.GroupUsers = new List<DirectoryUser>();
                    foreach (PermissionGroup_User_Mapping mapping in Mappings)
                    {
                        DirectoryUser user = model.AllUsers.Where(x => x.SN == mapping.UserID).FirstOrDefault();
                        if (user != null)
                        {
                            model.GroupUsers.Add(user);
                            model.AllUsers.Remove(user);
                        }
                    }
                    var tuple = new Tuple<UserToGroupModel, AccessRights>(model, Rights);
                    return View(tuple);
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }


        public ActionResult AddUserToGroup(Guid groupID, string UserID)
        {
            if (!(string.IsNullOrEmpty(UserID)))
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    List<PermissionGroup_User_Mapping> Mappings = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == groupID && x.UserID == UserID).ToList();
                    if (Mappings.Count == 0)
                    {
                        PermissionGroup_User_Mapping mapping = new PermissionGroup_User_Mapping();
                        mapping.MappingID = Guid.NewGuid();
                        mapping.GroupID = groupID;
                        mapping.UserID = UserID;
                        context.PermissionGroup_User_Mapping.Add(mapping);
                        context.SaveChanges();
                        //AddToAudit("Add User to Group Mapping: " + mapping.MappingID);
                    }
                }

            }

            return RedirectToAction("AddUsersToGroup", new { GroupID = groupID });
        }
        public ActionResult AddUsersToRole(Guid RoleID)
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities veracontext = new SITSASEntities())
                {
                    using (SITSASEntities context = new SITSASEntities())
                    {
                        AccessRights Rights = ContextModel.DetermineAccess();
                        UserToRoleModel model = new UserToRoleModel();
                        model.Role = veracontext.Roles.Where(x => x.RoleID == RoleID).FirstOrDefault();

                        bool loadFromActiveDirectory = false;

                        if (HttpContext.Session["DirectoryUsers"] != null)
                        {
                            model.AllUsers = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                            if (model.AllUsers.Count == 0)
                            {
                                loadFromActiveDirectory = true;
                            }
                        }
                        else
                        {
                            loadFromActiveDirectory = true;
                        }
                        if (loadFromActiveDirectory)
                        {
                            model.AllUsers = ContextModel.GetUsersFromActiveDirectory(context);
                            HttpContext.Session["DirectoryUsers"] = model.AllUsers;
                        }
                        List<Role_User_PermissionMapping> Mappings = context.Role_User_PermissionMapping.Where(x => x.RoleID == RoleID).ToList();
                        model.RoleUsers = new List<DirectoryUser>();
                        foreach (Role_User_PermissionMapping mapping in Mappings)
                        {
                            DirectoryUser user = model.AllUsers.Where(x => x.SN == mapping.ObjectSID).FirstOrDefault();
                            if (user != null)
                            {
                                model.RoleUsers.Add(user);
                                //model.AllUsers.Remove(user);
                            }
                        }
                        var tuple = new Tuple<UserToRoleModel, AccessRights>(model, Rights);
                        return View(tuple);
                    }
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }


        public ActionResult AddUserToRole(Guid RoleID, string UserID)
        {
            if (!(string.IsNullOrEmpty(UserID)))
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    List<Role_User_PermissionMapping> Mappings = context.Role_User_PermissionMapping.Where(x => x.RoleID == RoleID && x.ObjectSID == UserID).ToList();
                    if (Mappings.Count == 0)
                    {
                        Role_User_PermissionMapping mapping = new Role_User_PermissionMapping();
                        mapping.PermissionMappingID = Guid.NewGuid();
                        mapping.RoleID = RoleID;
                        mapping.ObjectSID = UserID;
                        context.Role_User_PermissionMapping.Add(mapping);
                        context.SaveChanges();
                        //AddToAudit("Add User to Role Mapping: " + mapping.MappingID);
                    }
                }

            }

            return RedirectToAction("AddUsersToRole", new { RoleID = RoleID });
        }
        public ActionResult RemoveUserFromGroup(Guid groupID, string UserID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                AccessRights Rights = ContextModel.DetermineAccess();
                List<PermissionGroup_User_Mapping> Mappings = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == groupID && x.UserID == UserID).ToList();
                if (Mappings.Count > 0)
                {
                    PermissionGroup_User_Mapping mapping = Mappings.FirstOrDefault();
                    context.PermissionGroup_User_Mapping.Remove(mapping);
                    context.SaveChanges();
                    //AddToAudit("Remove User from Group Mapping: " + mapping.MappingID);
                }


            }
            return RedirectToAction("AddUsersToGroup", new { GroupID = groupID });

        }

        public ActionResult RemoveUserFromRole(Guid RoleID, string UserID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                AccessRights Rights = ContextModel.DetermineAccess();
                List<Role_User_PermissionMapping> Mappings = context.Role_User_PermissionMapping.Where(x => x.RoleID == RoleID && x.ObjectSID == UserID).ToList();
                if (Mappings.Count > 0)
                {
                    Role_User_PermissionMapping mapping = Mappings.FirstOrDefault();
                    context.Role_User_PermissionMapping.Remove(mapping);
                    context.SaveChanges();
                    //AddToAudit("Remove User from Role Mapping: " + mapping.MappingID);
                }


            }
            return RedirectToAction("AddUsersToRole", new { RoleID = RoleID });

        }

        public ActionResult RemoveMapping(string PrimaryID, string SecondaryID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                List<DataMapping> Mappings = context.DataMappings.Where(x => x.PrimaryID == PrimaryID && x.SecondaryID == SecondaryID).ToList();
                if (Mappings.Count > 0)
                {
                    foreach(var map in Mappings)
                    {
                        context.DataMappings.Remove(map);
                    }
                  
                    context.SaveChanges();
                    //AddToAudit("Remove User from Group Mapping: " + mapping.MappingID);
                }


            }
            return RedirectToAction("PermissionGroupCreateNewScreen", new { ID = PrimaryID });

        }
        public ActionResult PermissionGroupDeleted()
        {
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    AccessRights Rights = ContextModel.DetermineAccess();
                    PermissionGroupModel model = new PermissionGroupModel();
                    model.ExistingPermissionGroups = context.PermissionGroups.Where(x => x.Deleted == true).OrderBy(x => x.GroupName).ToList();
                    var tuple = new Tuple<PermissionGroupModel, AccessRights>(model, Rights);
                    return View(tuple);
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }
        }
        public ActionResult RestorePermissionGroup(Guid? ID)
        {
            if (ContextModel.AccessToPage())
            {
                if (ID.HasValue)
                {
                    using (SITSASEntities context = new SITSASEntities())
                    {
                        PermissionGroup PermissionGroup = context.PermissionGroups.Where(x => x.ID == ID.Value).FirstOrDefault();
                        PermissionGroup.Deleted = false;
                        context.SaveChanges();
                        //AddToAudit("Restore Permission Group: " + PermissionGroup.ID);
                    }
                }
            }
            return RedirectToAction("PermissionGroup");
        }
        public ActionResult DeletePermissionGroup(Guid? ID)
        {
            if (ContextModel.AccessToPage())
            {
                if (ID.HasValue)
                {
                    using (SITSASEntities context = new SITSASEntities())
                    {
                        PermissionGroup PermissionGroup = context.PermissionGroups.Where(x => x.ID == ID.Value).FirstOrDefault();
                        PermissionGroup.Deleted = true;
                        context.SaveChanges();
                        //AddToAudit("Delete Permission Group: " + PermissionGroup.ID);
                    }
                }
            }
            return RedirectToAction("PermissionGroup");
        }


        public ActionResult CreateNewPermissionGroup(FormCollection form)
        {
            Guid PermissionGroupID = new Guid();
            if (ContextModel.AccessToPage())
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    string PermissionGroupName = form["PermissionGroupName"];
                    string lsPermissionGroupID = form["ExistingPermissionGroupID"];
                    Guid.TryParse(lsPermissionGroupID, out PermissionGroupID);

                    string lsParentGroup = form["ParentGroupID"];
                    Guid ParentID = new Guid();
                    if (!string.IsNullOrEmpty(lsParentGroup))
                    {
                        Guid.TryParse(lsParentGroup, out ParentID);
                    }

                    if (PermissionGroupID != new Guid())
                    {
                        PermissionGroup existingPermissionGroup = context.PermissionGroups.Where(x => x.ID == PermissionGroupID).FirstOrDefault();
                        existingPermissionGroup.GroupName = PermissionGroupName;
                        existingPermissionGroup.Hidden = false;
                        if (ParentID != new Guid())
                        {
                            existingPermissionGroup.ParentGroupID = ParentID;
                        }
                        if (form.AllKeys.Contains("AllQuestionnairesOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = PermissionGroupID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            
                            context.DataMappings.Add(dmap);
                            //AddToAudit("Update Permission Group: " + dmap.ID);
                            //model.AllContractsOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.DataMappingType.eNumMapping == (int)DataPermissionMappings.GroupToContract).ToList().Count > 0;
                        }
                        else
                        {
                            string lsPermissionGroupId = PermissionGroupID.ToString();
                            List<DataMapping> dms = context.DataMappings.Where(x => x.OverrideAll == true && x.PrimaryID == lsPermissionGroupId && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).ToList();
                            foreach (DataMapping dm in dms)
                            {
                                if (dm != null)
                                {
                                    context.DataMappings.Remove(dm);
                                }
                            }

                        }
                        if (form.AllKeys.Contains("AllLocationsOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToLocation).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = PermissionGroupID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            context.DataMappings.Add(dmap);
                            //AddToAudit("Update Permission Group: " + dmap.ID);
                            //model.AllContractsOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.DataMappingType.eNumMapping == (int)DataPermissionMappings.GroupToContract).ToList().Count > 0;
                        }
                        else
                        {
                            string lsPermissionGroupId = PermissionGroupID.ToString();
                            List<DataMapping> dms = context.DataMappings.Where(x => x.OverrideAll == true && x.PrimaryID == lsPermissionGroupId && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToLocation).ToList();
                            foreach (DataMapping dm in dms)
                            {
                                if (dm != null)
                                {
                                    context.DataMappings.Remove(dm);
                                }
                            }
                        }
                        if (form.AllKeys.Contains("AllAreasOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToArea).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = PermissionGroupID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            context.DataMappings.Add(dmap);
                            //AddToAudit("Update Permission Group: " + dmap.ID);
                            //model.AllContractsOverride = AllGroupMappings.Where(x => x.OverrideAll == true && x.DataMappingType.eNumMapping == (int)DataPermissionMappings.GroupToContract).ToList().Count > 0;
                        }
                        else
                        {
                            string lsPermissionGroupId = PermissionGroupID.ToString();
                            List<DataMapping> dms = context.DataMappings.Where(x => x.OverrideAll == true && x.PrimaryID == lsPermissionGroupId && x.DataMappingType.eNumMapping == (int)eDataMappingType.GroupToArea).ToList();
                            foreach (DataMapping dm in dms)
                            {
                                if (dm != null)
                                {
                                    context.DataMappings.Remove(dm);
                                }
                            }
                        }
                    }
                    else
                    {
                        PermissionGroup newPermissionGroup = new PermissionGroup();
                        newPermissionGroup.ID = Guid.NewGuid();
                        PermissionGroupID = newPermissionGroup.ID;
                        newPermissionGroup.GroupName = PermissionGroupName;
                        newPermissionGroup.Hidden = false;
                        if (ParentID != new Guid())
                        {
                            newPermissionGroup.ParentGroupID = ParentID;
                        }
                        context.PermissionGroups.Add(newPermissionGroup);

                        if (form.AllKeys.Contains("AllQuestionnairesOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToQuestionnaire).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = newPermissionGroup.ID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            context.DataMappings.Add(dmap);
                        }

                        if (form.AllKeys.Contains("AllAreasOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToArea).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = newPermissionGroup.ID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            context.DataMappings.Add(dmap);
                         }
                        if (form.AllKeys.Contains("AllLocationsOverride"))
                        {
                            DataMappingType Dtype = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.GroupToLocation).FirstOrDefault();
                            DataMapping dmap = new DataMapping();
                            dmap.ID = Guid.NewGuid();
                            dmap.DataMappingTypeID = Dtype.ID;
                            dmap.OverrideAll = true;
                            dmap.PrimaryID = newPermissionGroup.ID.ToString();
                            dmap.SecondaryID = new Guid().ToString();
                            context.DataMappings.Add(dmap);
                        }
                    }

                    context.SaveChanges();
                }
            }
            else
            {
                return RedirectToAction("Home", "Index");
            }

            return RedirectToAction("PermissionGroupCreateNewScreen", new { ID = PermissionGroupID });
        }

    }
}