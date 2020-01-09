using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SITSAS.Models
{
    public class ContextModel
    {
        private static String SID1
        {
            get
            {
                return "S-1-5-21-1199947781-3238188910-400929352-4882";
                //return "SNTest";
            }
        }

        //public static string GetDomain(DCLReportingDatabaseEntities context)
        //{
        //    Setting DirectoryDomain = context.Settings.Where(x => x.SettingName == "DirectoryEntryDomain").FirstOrDefault();
        //    return DirectoryDomain.SettingValue;
        //}
        public static List<DirectoryUser> GetUsersFromActiveDirectory(SITSASEntities Context)
        {
            List<DirectoryUser> Model = new List<DirectoryUser>();

                        string DomainName = GetDomain(Context);
            if (DomainName == "Test")
            {
                DirectoryUser Dentry = new DirectoryUser();

                Dentry.Name = "GivenName";
                Dentry.SN = "S-1-5-21-1199947781-3238188910-400929352-4882";
                Dentry.SamAccountName = "samAccountName";
                Dentry.UserPrincipalName = "userPrincipalName";
                Model.Add(Dentry);
                return Model;
            }
            else
            {

                using (var Pcontext = new PrincipalContext(ContextType.Domain, DomainName))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(Pcontext)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                            if (de != null)
                            {
                                DirectoryUser Dentry = new DirectoryUser();
                                if (de.Properties["givenName"] != null && de.Properties["sn"] != null)
                                {

                                    Dentry.Name = de.Properties["givenName"].Value as string;

                                    Dentry.Name = Dentry.Name + " " + de.Properties["sn"].Value as string;
                                }

                                if (de.Properties["objectSid"] != null)
                                {
                                    var sid = new SecurityIdentifier((byte[])de.Properties["objectSid"][0], 0);
                                    if (sid != null)
                                    {
                                        Dentry.SN = sid.ToString();
                                    }
                                }

                                if (de.Properties["samAccountName"] != null)
                                {
                                    Dentry.SamAccountName = de.Properties["samAccountName"].Value as string;
                                }

                                if (de.Properties["userPrincipalName"] != null)
                                {
                                    Dentry.UserPrincipalName = de.Properties["userPrincipalName"].Value as string;
                                }

                                if (!string.IsNullOrEmpty(Dentry.Name.Trim()))
                                {
                                    Model.Add(Dentry);
                                }

                            }

                        }

                    }
                    return Model;
                }
            }
        }

        private static SecurityIdentifier SID
        {
            get
            {
                WindowsIdentity identity = null;

                if (HttpContext.Current == null)
                {
                    identity = WindowsIdentity.GetCurrent();
                }
                else
                {
                    identity = HttpContext.Current.User.Identity as WindowsIdentity;
                }
                return identity.User;
            }
        }
        private static string GetDomain(SITSASEntities context)
        {
            SystemSetting DirectoryDomain = context.SystemSettings.Where(x => x.Name == "DirectoryEntryDomain").FirstOrDefault();

            return DirectoryDomain.Value;
        }

        public static string GetCurrentUserSID()
        {
            string ID = string.Empty;
            string DomainName = string.Empty;
            using (SITSASEntities context = new SITSASEntities())
            {
                DomainName = GetDomain(context);
            }
            if (!string.IsNullOrEmpty(DomainName))
            {

                if (DomainName == "Test")
                {
                    ID = SID1;
                }
                else
                {
                    SecurityIdentifier Identity = SID;

                    if (!(Identity == null))
                    {
                        //var sid = new SecurityIdentifier((byte[])de.Properties["objectSid"][0], 0);                          
                        ID = Identity.Value;
                        //AddToAudit("Found users in domain: SID = " + ID);
                    }

                }
            }
            return ID;
        }

        public static AccessRights DetermineAccess()
        {
            string ID = string.Empty;
            AccessRights Rights = new AccessRights();
            string DomainName = string.Empty;
            using (SITSASEntities context = new SITSASEntities())
            {
                DomainName = GetDomain(context);
            }
            if (!string.IsNullOrEmpty(DomainName))
            {

                if (DomainName == "Test")
                {
                    ID = SID1;
                }
                else
                {
                    SecurityIdentifier Identity = SID;

                    if (!(Identity == null))
                    {
                        //var sid = new SecurityIdentifier((byte[])de.Properties["objectSid"][0], 0);                          
                        ID = Identity.Value;
                        //AddToAudit("Found users in domain: SID = " + ID);
                    }

                }
            }

            if (!(string.IsNullOrEmpty(ID)))
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                 
                        List<Role_User_PermissionMapping> ListOfMappings = new List<Role_User_PermissionMapping>();
                        ListOfMappings = context.Role_User_PermissionMapping.Where(x => x.ObjectSID == ID).ToList();
                        //AddToAudit("SID = " + ID + " permissions found for user: " + ListOfMappings.Count.ToString());
                        if (!(ListOfMappings == null))
                        {
                            List<Role_Permissions> PermissionList = new List<Role_Permissions>();
                        foreach (Role_User_PermissionMapping PM in ListOfMappings)
                        {
                            Role_Permissions Perm = context.Role_Permissions.Where(x => x.RoleID == PM.RoleID).FirstOrDefault();
                            if (Perm != null)
                            {
                                PermissionList.Add(Perm);
                            }
                        }
                            if (PermissionList.Count > 0)
                            {
                                foreach (Role_Permissions liPerm in PermissionList)
                                {
                                    if (liPerm.CanAdd == true)
                                    {
                                        Rights.CanAdd = true;
                                    }
                                    if (liPerm.CanEdit == true)
                                    {
                                        Rights.CanEdit = true;
                                    }
                                    if (liPerm.CanView == true)
                                    {
                                        Rights.CanView = true;
                                    }
                                    if (liPerm.CanDelete == true)
                                    {
                                        Rights.CanDelete = true;
                                    }
                                }
                                Rights.HasBeenSet = true;
                            }
                        }
                }
            }

            return Rights;
        }
        public static AccessRights DetermineAccess(string PageName)
        {
            string ID = string.Empty;
            AccessRights Rights = new AccessRights();
            string DomainName = string.Empty;
            Rights.CanAdd = false;
            Rights.CanDelete = false;
            Rights.CanEdit = false;
            Rights.CanView = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                DomainName = GetDomain(context);
            }
            if (!string.IsNullOrEmpty(DomainName))
            {

                if (DomainName == "Test")
                {
                    ID = SID1;
                }
                else
                {
                    SecurityIdentifier Identity = SID;

                    if (!(Identity == null))
                    {                     
                        ID = Identity.Value;
                    }

                }
            }

            using (SITSASEntities context = new SITSASEntities())
            {
                Page page = context.Pages.Where(x => x.PageName.ToUpper() == PageName.ToUpper()).FirstOrDefault();
                if (page != null)
                {
                    List<Role_User_PermissionMapping> ListOfMappings = new List<Role_User_PermissionMapping>();
                    ListOfMappings = context.Role_User_PermissionMapping.Where(x => x.ObjectSID == ID).ToList();
                    List<Page_Role_Mappings> maps = context.Page_Role_Mappings.Where(x => x.PageID == page.ID).ToList();
                    if (maps != null)
                    {
                        foreach (Role_User_PermissionMapping role in ListOfMappings)
                        {
                            foreach (Page_Role_Mappings map in maps.Where(x => x.RoleID == role.RoleID.ToString()))
                            {
                                if (!Rights.CanAdd)
                                {
                                    Rights.CanAdd = map.CanCreate;
                                }
                                if (!Rights.CanEdit)
                                {
                                    Rights.CanEdit = map.CanUpdate;
                                }
                                if (!Rights.CanDelete)
                                {
                                    Rights.CanDelete = map.CanDelete;
                                }
                                if (!Rights.CanView)
                                {
                                    Rights.CanView = map.CanView;
                                }
                            }
                        }

                    }
                }
                else
                {
                    throw new Exception("Unknown Page " + PageName);
                }
            }







            return Rights;
        }

        public static bool AccessToPage()
        {
            return true;
        }
    }
}