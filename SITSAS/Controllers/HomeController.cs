using Aspose.Words;
using Aspose.Words.MailMerging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using SITSAS.Models;

namespace SITSAS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                IndexModel model = new IndexModel();
                model.locations = GetLocationsWhereAreaIsLive(context, true);
                model.questionnaires = context.Questionnaires.Include("Result_Headers").Where(x => x.Deleted == false).ToList();
                if (ID.HasValue)
                {
                    model.SelectedQuestionnaire = ID.Value;
                }
                else
                {
                    model.SelectedQuestionnaire = model.questionnaires.OrderBy(x => x.Name).FirstOrDefault().ID;
                }

                List<Location> tempLocations = new List<Location>();
                tempLocations.AddRange(model.locations);
                List<DataMapping> existingDataMappings = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.LocationToQuestionnaire && x.SecondaryID == model.SelectedQuestionnaire.ToString()).ToList();
                foreach (var location in tempLocations)
                {
                    if (existingDataMappings.FirstOrDefault(x => x.PrimaryID == location.ID.ToString()) == null)
                    {
                        model.locations.Remove(location);
                    }
                }

                
                Guid FrequencyProfileID = model.questionnaires.FirstOrDefault(x => x.ID == model.SelectedQuestionnaire).FrequencyProfileID.Value;
           
                DateTime StartDate = new DateTime(DateTime.Now.Year, 1,1);
                DateTime EndDate = StartDate.AddYears(1);
                model.freqProfile = context.FrequencyProfiles.FirstOrDefault(x => x.ID == FrequencyProfileID);
                model.fpDates = context.FrequencyProfile_Dates.Where(x => x.FrequencyID == model.freqProfile.ID && x.StartDate < EndDate && x.EndDate > StartDate).ToList();

                return View(model);

            }




        }
        public ActionResult Home()
        {
            //using (SITSASEntities context = new SITSASEntities())
            //{
            //    decimal CurrentScore = CalculateScoreHelper.CalculateStalenessScore(5, new DateTime(2017, 01, 01), context.StalenessProfiles.FirstOrDefault(), new DateTime(2017, 01, 9));
            //}
            return View();
        }

        public ActionResult LoadPreviousAnswer(Guid QuestionnaireID, Guid QuestionID, int Year, int Month, Guid LocationID)
        {
            string model = "No Previous Answer";
            using (SITSASEntities context = new SITSASEntities())
            {
                DateTime SelectedDate = new DateTime(Year, Month, 1);
                List<Result_Headers> previousHeaders = context.Result_Headers.Where(x => x.SelectedDate < SelectedDate && x.Submitted == true).OrderByDescending(x => x.SelectedDate).ToList();
                if (previousHeaders != null)
                {
                    foreach (Result_Headers previousHeader in previousHeaders)
                    {
                        Result_Answers previousAnswer = previousHeader.Result_Answers.Where(x => x.Answer.QuestionID == QuestionID).FirstOrDefault();
                        if (previousAnswer != null)
                        {
                            model = "Previous Answer: " + previousAnswer.RawAnswer + " (" + previousHeader.SelectedDate.ToString("MM yyyy") + ")";
                            if (!string.IsNullOrEmpty(previousAnswer.Comments))
                            {
                                model = model + "<br/><br/>Comments:" + previousAnswer.Comments;
                            }
                            break;
                        }
                    }
                }


            }
            return this.Json(model, JsonRequestBehavior.AllowGet);

        }

        public ActionResult ReassignTasks()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                List<DirectoryUser> usrs = null;
                if (HttpContext.Session["DirectoryUsers"] != null)
                {
                    usrs = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                }
                else
                {
                    usrs = ContextModel.GetUsersFromActiveDirectory(context);
                    HttpContext.Session["DirectoryUsers"] = usrs;
                }

                return View(usrs);
            }
        }
        public ActionResult ReassignTasksToNewUser(FormCollection form)
        {
            //from //to
            string OldUser = form["From"];
            string NewUser = form["To"];
            using (SITSASEntities context = new SITSASEntities())
            {
                List<DataMapping> dMaps = context.DataMappings.Where(x => x.SecondaryID == OldUser.ToString() && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();
                foreach (DataMapping dMap in dMaps)
                {
                    dMap.SecondaryID = NewUser;
                }
                context.SaveChanges();
            }
            return RedirectToAction("Tasks");
        }

        public ActionResult Success()
        {

            return View();
        }

        public ActionResult LoadYearsAvailableForQuestionnaire(Guid? HeaderID, Guid? LocationID, Guid? QuestionnaireID)
        {
            DateSelectionModel model = new DateSelectionModel();
            using (SITSASEntities context = new SITSASEntities())
            {

                List<Result_Headers> headers = null;
                List<Result_Headers_Fixings> headers_fixings = null;
                Result_Headers currentHeader = context.Result_Headers.FirstOrDefault(x => x.ID == HeaderID);
                model.header = currentHeader;
                if (currentHeader != null)
                {
                    headers = context.Result_Headers.Where(x => x.QuestionnaireID == currentHeader.QuestionnaireID && x.LocationID == currentHeader.LocationID && x.ID != currentHeader.ID).ToList();
                    headers_fixings = context.Result_Headers_Fixings.Where(x => x.QuestionnaireID == currentHeader.QuestionnaireID && x.LocationID == currentHeader.LocationID).ToList();
                    model.SelectedValue = currentHeader.SelectedDate.Year;
                }
                else
                {

                    model.LocationID = LocationID.Value;
                    model.QuestionnaireID = QuestionnaireID.Value;
                    headers = context.Result_Headers.Where(x => x.QuestionnaireID == QuestionnaireID && x.LocationID == LocationID).ToList();
                    headers_fixings = context.Result_Headers_Fixings.Where(x => x.QuestionnaireID == QuestionnaireID && x.LocationID == LocationID).ToList();
                }
                SortedList<int, int> processedYears = new SortedList<int, int>();
                foreach (Result_Headers header in headers)
                {
                    if (processedYears.ContainsKey(header.SelectedDate.Year))
                    {
                        processedYears[header.SelectedDate.Year] += 1;
                    }
                    else
                    {
                        processedYears.Add(header.SelectedDate.Year, 1);
                    }
                }
                foreach (Result_Headers_Fixings headers_fixing in headers_fixings)
                {
                    if (processedYears.ContainsKey(headers_fixing.SelectedDate.Year))
                    {
                        processedYears[headers_fixing.SelectedDate.Year] += 1;
                    }
                    else
                    {
                        processedYears.Add(headers_fixing.SelectedDate.Year, 1);
                    }
                }

                model.DropdownOptions = new List<QuestionnaireDropDown>();

                int StartYear = 2019;
                int CurrentYear = DateTime.Now.Year;
                for (int i = StartYear; i <= CurrentYear; i++)
                {
                    QuestionnaireDropDown qDD = new QuestionnaireDropDown();
                    qDD.Value = i;
                    if (processedYears.ContainsKey(i))
                    {
                        if (processedYears[i] >= 12)
                        {
                            qDD.Disabled = true;
                        }
                        else
                        {
                            qDD.Disabled = false;
                        }
                    }
                    else
                    {
                        qDD.Disabled = false;
                    }
                    model.DropdownOptions.Add(qDD);
                }


            }

            return PartialView("partYearSelection", model);
        }
        public ActionResult LoadMonthsAvailableForQuestionnaireByYear(Guid? HeaderID, int Year, Guid? LocationID, Guid? QuestionnaireID)
        {
            DateSelectionModel model = new DateSelectionModel();
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Result_Headers> headers = null;
                List<Result_Headers_Fixings> headers_fixings = null;
                Result_Headers currentHeader = context.Result_Headers.FirstOrDefault(x => x.ID == HeaderID);
                model.header = currentHeader;

                if (currentHeader != null)
                {
                    model.SelectedValue = currentHeader.SelectedDate.Month;
                    headers = context.Result_Headers.Where(x => x.QuestionnaireID == currentHeader.QuestionnaireID && x.LocationID == currentHeader.LocationID && x.ID != currentHeader.ID && x.SelectedDate.Year == Year).ToList();
                    headers_fixings = context.Result_Headers_Fixings.Where(x => x.QuestionnaireID == currentHeader.QuestionnaireID && x.LocationID == currentHeader.LocationID && x.SelectedDate.Year == Year).ToList();
                }
                else
                {
                    model.LocationID = LocationID.Value;
                    model.QuestionnaireID = QuestionnaireID.Value;
                    headers = context.Result_Headers.Where(x => x.QuestionnaireID == QuestionnaireID && x.LocationID == LocationID && x.SelectedDate.Year == Year).ToList();
                    headers_fixings = context.Result_Headers_Fixings.Where(x => x.QuestionnaireID == QuestionnaireID && x.LocationID == LocationID && x.SelectedDate.Year == Year).ToList();
                }
                List<int> processedMonths = new List<int>();
                foreach (Result_Headers header in headers)
                {
                    if (!processedMonths.Contains(header.SelectedDate.Month))
                    {
                        processedMonths.Add(header.SelectedDate.Month);
                    }
                }
                foreach (Result_Headers_Fixings headers_fixing in headers_fixings)
                {
                    if (!processedMonths.Contains(headers_fixing.SelectedDate.Month))
                    {
                        processedMonths.Add(headers_fixing.SelectedDate.Month);
                    }
                }
                model.DropdownOptions = new List<QuestionnaireDropDown>();
                for (int i = 1; i <= 12; i++)
                {
                    QuestionnaireDropDown qDD = new QuestionnaireDropDown();
                    qDD.Value = i;
                    if (processedMonths.Contains(i))
                    {
                        qDD.Disabled = true;
                    }
                    else
                    {
                        qDD.Disabled = false;
                    }
                    model.DropdownOptions.Add(qDD);
                }
            }

            return PartialView("partMonthSelection", model);
        }
        #region Areas
        public ActionResult Areas()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                AreaModel model = new AreaModel();
                model.rights = ContextModel.DetermineAccess();
                List<Area> tempLocations = context.GetAreasForUser(ContextModel.GetCurrentUserSID(), false).ToList();

                List<Area> tempAreaColl = context.Areas.Where(x => x.Deleted == false).ToList();
                model.ExistingAreas = new SortedList<Guid, string>();
                foreach (Area count in tempAreaColl)
                {
                    if (tempLocations.Where(x => x.ID == count.ID).ToList().Count > 0)
                    {
                        model.ExistingAreas.Add(count.ID, count.Name);
                    }
                }
                return View(model);
            }
        }
        public ActionResult ApproveQuestionnaire(Guid ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                context.ApproveQuestionnaire(ID);
                return RedirectToAction("ApprovedQuestionnaires");
            }
        }
        public ActionResult CreateUpdateArea(Guid? ID, bool IsPart = false)
        {
            CreateUpdateAreaModel model = new CreateUpdateAreaModel();
            model.rights = ContextModel.DetermineAccess();
            model.AreaExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Area Area = context.Areas.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (Area != null)
                    {
                        model.ExistingArea = Area;
                        model.AreaExists = true;
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }

        public ActionResult DeleteArea(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Area Area = context.Areas.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (Area != null)
                    {
                        Area.Deleted = true;
                        context.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Areas");
        }
        public ActionResult SaveArea(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingAreaID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Area cty = null;
                if (ID != new Guid())
                {
                    cty = context.Areas.Where(x => x.ID == ID).FirstOrDefault();
                    //AddToAudit("Area Updated " + CTY.full_name);
                }
                else
                {
                    cty = new Area();
                    cty.ID = Guid.NewGuid();
                }
                cty.Name = form["Name"];

                if (ID == new Guid())
                {
                    context.Areas.Add(cty);

                }

                context.SaveChanges();
            }
            return RedirectToAction("Areas");
        }
        #endregion 
        #region Categories

        public ActionResult Categories()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                CategoriesModel model = new CategoriesModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingCategories = context.Categories.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                return View(model);
            }
        }
        public ActionResult RetiredCategories()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                CategoriesModel model = new CategoriesModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingCategories = context.Categories.Where(x => x.Deleted == false && x.EndDate < ldNow).ToList();
                return View(model);
            }
        }
        public ActionResult FutureCategories()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                CategoriesModel model = new CategoriesModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingCategories = context.Categories.Where(x => x.Deleted == false && x.EndDate > ldNow && x.StartDate > ldNow).ToList();
                return View(model);
            }
        }

        public ActionResult CreateUpdateCategory(Guid? ID, bool IsPart = false)
        {
            CreateUpdateCategoryModel model = new CreateUpdateCategoryModel();
            model.rights = ContextModel.DetermineAccess();
            model.CategoryExists = false;
            model.AllowEdit = true;
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Category rCategory = context.Categories.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingCategory = rCategory;
                        model.CategoryExists = true;
                        DateTime ldNow = DateTime.Now;
                        if (model.ExistingCategory.EndDate < ldNow)
                        {
                            model.AllowEdit = false;
                        }
                        //if a question in this category has been answered.
                        if (model.ExistingCategory.SubCategories.Where(x => x.Questions.Where(q => q.Answers.Where(a => a.Result_Answers.ToList().Count > 0).ToList().Count > 0).ToList().Count > 0).ToList().Count > 0)
                        {
                            model.rights.CanDelete = false;
                        }
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }




        public ActionResult DeleteCategory(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Category rCategory = context.Categories.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("Categories");
        }
        public ActionResult SaveCategory(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingCategoryID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Category rCat = null;
                if (ID != new Guid())
                {
                    rCat = context.Categories.Where(x => x.ID == ID).FirstOrDefault();
                }
                else
                {
                    rCat = new Category();
                    rCat.ID = Guid.NewGuid();
                }
                if (form.AllKeys.Contains("Name"))
                {
                    rCat.Name = form["Name"];
                    if (form.AllKeys.Contains("SpecialCase"))
                    {
                        rCat.SpecialCase = true;
                    }
                    else
                    {
                        rCat.SpecialCase = false;
                    }
                }
                if (form.AllKeys.Contains("StartDate"))
                {
                    DateTime ldStartDate = DateTime.MinValue;
                    DateTime.TryParse(form["StartDate"], out ldStartDate);
                    rCat.StartDate = ldStartDate;
                }
                if (form.AllKeys.Contains("EndDate"))
                {
                    CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.LCID);
                    ci.Calendar.TwoDigitYearMax = 2099;
                    //Parse the date using our custom culture.

                    string da = form["EndDate"];
                    DateTime ldEndDate = DateTime.ParseExact(form["EndDate"], "dd MMM yy", ci);

                    //DateTime ldEndDate = DateTime.MinValue;
                    //DateTime.TryParse(form["EndDate"], out ldEndDate);
                    rCat.EndDate = ldEndDate;
                }
                if (form.AllKeys.Contains("DisplayName"))
                {
                    rCat.DisplayName = form["DisplayName"];
                }

                if (ID == new Guid())
                {
                    context.Categories.Add(rCat);
                }
                context.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
        #endregion
        #region SubCategories
        public ActionResult SubCategories()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                SubCategoriesModel model = new SubCategoriesModel();
                model.rights = ContextModel.DetermineAccess();
                model.ExistingSubCategories = context.SubCategories.Include("Category").Where(x => x.Deleted == false).OrderBy(x => x.DisplayOrder).ToList();
                return View(model);
            }
        }

        public ActionResult CreateUpdateSubCategory(Guid? ID, bool IsPart = false)
        {
            CreateUpdateSubCategoryModel model = new CreateUpdateSubCategoryModel();
            model.SubCategoryExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    SubCategory rCategory = context.SubCategories.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingSubCategory = rCategory;
                        model.SubCategoryExists = true;
                    }

                }
                DateTime ldNow = DateTime.Now;
                model.AllCategories = context.Categories.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteSubCategory(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    SubCategory rCategory = context.SubCategories.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("SubCategories");
        }
        public ActionResult SaveSubCategory(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingSubCategoryID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                SubCategory dCat = null;
                if (ID != new Guid())
                {
                    dCat = context.SubCategories.Where(x => x.ID == ID).FirstOrDefault();

                }
                else
                {
                    dCat = new SubCategory();
                    dCat.ID = Guid.NewGuid();
                }

                dCat.Name = form["Name"];
                Guid racID = new Guid();
                Guid.TryParse(form["CategoryID"], out racID);

                int liDisplayOrder = 0;
                int.TryParse(form["DisplayOrder"], out liDisplayOrder);
                dCat.DisplayOrder = liDisplayOrder;

                dCat.CategoryID = racID;

                if (form.AllKeys.Contains("DisplayName"))
                {
                    dCat.DisplayName = form["DisplayName"];
                }

                if (ID == new Guid())
                {
                    context.SubCategories.Add(dCat);
                }




                context.SaveChanges();
            }
            return RedirectToAction("SubCategories");
        }
        #endregion

        #region Locations
        public ActionResult Locations(bool ForceRefreshDirectoryUsers = false)
        {
            LocationsModel model = new LocationsModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                model.ExistingLocations = new List<LocationItem>();
                model.AllLocationPermissionGroups = context.Location_PermissionGroupTemplate.ToList();
                List<Location> locations = GetLocationsWhereAreaIsLive(context, true);
                List<DirectoryUser> usrs = null;
                if (HttpContext.Session["DirectoryUsers"] != null && ForceRefreshDirectoryUsers == false)
                {
                    usrs = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                }
                else
                {
                    usrs = ContextModel.GetUsersFromActiveDirectory(context);
                    HttpContext.Session["DirectoryUsers"] = usrs;
                }
                foreach (Location location in locations)
                {
                    LocationItem lItem = new LocationItem();
                    lItem.ID = location.ID;
                    lItem.Name = location.Name;
                    lItem.AreaName = location.Area.Name;
                    //lItem.User = "Not Assigned";
                    //if (location.UserID != null)
                    //{
                    //    DirectoryUser usr = usrs.Where(x => x.SN == location.UserID).FirstOrDefault();
                    //    if (usr != null)
                    //    {
                    //        lItem.User = usr.Name;
                    //    }
                    //}
                    lItem.PermissionGroupToUsername = new SortedList<Guid, string>();
                    foreach (Location_PermissionGroupTemplate permGroupTemplate in model.AllLocationPermissionGroups)
                    {
                        PermissionGroup pGroup = context.PermissionGroups.Where(x => x.TemplateID == permGroupTemplate.ID && x.GroupName.StartsWith(lItem.ID.ToString())).FirstOrDefault();
                        if (pGroup != null)
                        {
                            if (pGroup.PermissionGroup_User_Mapping.ToList() != null)
                            {
                                string usersInGroup = "";
                                foreach (var user in pGroup.PermissionGroup_User_Mapping.ToList())
                                {


                                    DirectoryUser usr = usrs.Where(x => x.SN == user.UserID).FirstOrDefault();
                                    if (usr != null)
                                    {
                                        usersInGroup = usersInGroup + usr.Name + ", ";

                                    }
                                }
                                if (!string.IsNullOrEmpty(usersInGroup))
                                {
                                    lItem.PermissionGroupToUsername.Add(permGroupTemplate.ID, usersInGroup.Trim().Substring(0, usersInGroup.Trim().Length - 1));
                                }
                            }

                        }

                    }
                    model.ExistingLocations.Add(lItem);
                }
                //model.ExistingLocations = context.Locations.Where(x => x.Deleted == false).ToList();

            }
            //using (SITSASEntities context = new SITSASEntities())
            //{
            //    model.AllAreas = context.Areas.ToList();
            //}
            return View(model);
        }

        public ActionResult CreateUpdateLocation(Guid? ID, bool IsPart = false)
        {
            CreateUpdateLocationModel model = new CreateUpdateLocationModel();
            model.rights = ContextModel.DetermineAccess();
            model.LocationExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                List<DirectoryUser> usrs = null;
                if (HttpContext.Session["DirectoryUsers"] != null)
                {
                    usrs = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                }
                else
                {
                    usrs = ContextModel.GetUsersFromActiveDirectory(context);
                    HttpContext.Session["DirectoryUsers"] = usrs;
                }
                model.AllLocationPermissionGroups = context.Location_PermissionGroupTemplate.ToList();
                model.PermissionGroupToUsername = new SortedList<Guid, List<string>>();
                if (ID.HasValue)
                {

                    Location rCategory = context.Locations.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingLocation = rCategory;
                        model.LocationExists = true;
                        CreatePermissionGroupsForLocation(ID.Value, context);
                        model.PermissionGroupToUsername = new SortedList<Guid, List<string>>();
                        foreach (Location_PermissionGroupTemplate permGroupTemplate in model.AllLocationPermissionGroups)
                        {
                            PermissionGroup pGroup = context.PermissionGroups.Where(x => x.TemplateID == permGroupTemplate.ID && x.GroupName.StartsWith(rCategory.ID.ToString())).FirstOrDefault();
                            if (pGroup != null)
                            {
                                if (pGroup.PermissionGroup_User_Mapping.ToList() != null)
                                {
                                    List<string> SelectedValues = new List<string>();
                                    foreach (var userMap in pGroup.PermissionGroup_User_Mapping)
                                    {
                                        SelectedValues.Add(userMap.UserID);
                                    }
                                    if (SelectedValues != null)
                                    {
                                        model.PermissionGroupToUsername.Add(permGroupTemplate.ID, SelectedValues);
                                    }
                                }

                            }

                        }
                    }


                }
            }
            using (SITSASEntities context = new SITSASEntities())
            {
                model.AllAreas = context.Areas.ToList();

                if (HttpContext.Session["DirectoryUsers"] != null)
                {
                    model.AllUsers = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                }
                else
                {
                    model.AllUsers = ContextModel.GetUsersFromActiveDirectory(context);
                    HttpContext.Session["DirectoryUsers"] = model.AllUsers;
                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public void CreatePermissionGroupsForLocation(Guid ID, SITSASEntities context)
        {
            List<Location_PermissionGroupTemplate> template = context.Location_PermissionGroupTemplate.ToList();
            foreach (Location_PermissionGroupTemplate temp in template)
            {
                PermissionGroup pg = context.PermissionGroups.FirstOrDefault(x => x.GroupName == ID + "-" + temp.Name);
                if (pg == null)
                {
                    PermissionGroup newPG = new PermissionGroup();
                    newPG.ID = Guid.NewGuid();
                    newPG.GroupName = ID + "-" + temp.Name;
                    newPG.Hidden = true;
                    newPG.Deleted = false;
                    newPG.TemplateID = temp.ID;
                    context.PermissionGroups.Add(newPG);
                }
            }
            context.SaveChanges();
        }
        public ActionResult DeleteLocation(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Location rCategory = context.Locations.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("Locations");
        }
        public ActionResult SaveLocation(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingLocationID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Location Location = null;
                if (ID != new Guid())
                {
                    Location = context.Locations.Where(x => x.ID == ID).FirstOrDefault();
                }
                else
                {
                    Location = new Location();
                    Location.ID = Guid.NewGuid();

                }
                Location.Name = form["Name"];
                Guid racID = new Guid();
                Guid.TryParse(form["AreaID"], out racID);
                Location.AreaID = racID;
                Location.UserID = form["UserID"];
                if (ID == new Guid())
                {
                    context.Locations.Add(Location);
                }


                var allLocationPermissionGroups = context.Location_PermissionGroupTemplate.ToList();
                foreach (var pGroupTemplate in allLocationPermissionGroups)
                {
                    PermissionGroup pg = context.PermissionGroups.FirstOrDefault(x => x.TemplateID == pGroupTemplate.ID && x.GroupName.StartsWith(Location.ID.ToString()));
                    if (pg != null)
                    {
                        List<PermissionGroup_User_Mapping> permGroupUserColl = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == pg.ID).ToList();
                        context.PermissionGroup_User_Mapping.RemoveRange(permGroupUserColl);
                        if (form.AllKeys.ToList().Contains(pGroupTemplate.ID.ToString()))
                        {

                            List<string> lgEmployeeIDs = SITMVCFormHelper.GetStringCollectionFromForm(form, pGroupTemplate.ID.ToString());
                            foreach (string lgEmployeeID in lgEmployeeIDs)
                            {
                                PermissionGroup_User_Mapping permGroupUser = new PermissionGroup_User_Mapping();
                                permGroupUser.MappingID = Guid.NewGuid();
                                permGroupUser.UserID = lgEmployeeID;
                                permGroupUser.GroupID = pg.ID;
                                context.PermissionGroup_User_Mapping.Add(permGroupUser);
                            }

                        }
                    }
                }


                context.SaveChanges();
            }
            return RedirectToAction("Locations");
        }
        #endregion

        public void CheckLocationPermissionsGroupByName(SITSASEntities context, Location Location, string PermissionName)
        {
            if (Location.UserID != null)
            {
                PermissionGroup PG = context.PermissionGroups.Where(x => x.GroupName == Location.Name && x.Hidden == true).FirstOrDefault();
                if (PG == null)
                {
                    PG = new PermissionGroup();
                    PG.ID = Guid.NewGuid();
                    PG.GroupName = Location.Name;
                    PG.Hidden = true;
                    PG.Deleted = false;
                    context.PermissionGroups.Add(PG);
                    context.SaveChanges();
                    PermissionGroup_User_Mapping PGum = new PermissionGroup_User_Mapping();
                    PGum.GroupID = PG.ID;
                    PGum.MappingID = Guid.NewGuid();
                    PGum.UserID = Location.UserID;
                    context.PermissionGroup_User_Mapping.Add(PGum);
                    //Create User Link
                }
                else
                {
                    PermissionGroup_User_Mapping PGum = context.PermissionGroup_User_Mapping.Where(x => x.GroupID == PG.ID).FirstOrDefault();
                    if (PGum == null)
                    {
                        PGum = new PermissionGroup_User_Mapping();
                        PGum.GroupID = PG.ID;
                        PGum.MappingID = Guid.NewGuid();
                        PGum.UserID = Location.UserID;
                        context.PermissionGroup_User_Mapping.Add(PGum);
                    }
                    else
                    {
                        PGum.UserID = Location.UserID;
                    }
                }
            }
        }
        #region QuestionnaireGroups
        public ActionResult QuestionnaireGroups()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionnaireGroupsModel model = new QuestionnaireGroupsModel();
                model.rights = ContextModel.DetermineAccess();
                List<QuestionnaireGroup> qgColl = context.QuestionnaireGroups.Include("Questionnaire").Where(x => x.Deleted == false).ToList();
                List<Questionnaire> userVisibleQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                model.ExistingQuestionnaireGroups = new List<QuestionnaireGroup>();
                foreach (QuestionnaireGroup qg in qgColl)
                {
                    if (userVisibleQuestionnaires.Contains(qg.Questionnaire))
                    {

                        model.ExistingQuestionnaireGroups.Add(qg);
                    }
                }
                return View(model);
            }
        }

        public ActionResult CreateUpdateQuestionnaireGroup(Guid? ID, bool IsPart = false)
        {
            CreateUpdateQuestionnaireGroupModel model = new CreateUpdateQuestionnaireGroupModel();
            model.rights = ContextModel.DetermineAccess();
            model.QuestionnaireGroupExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    QuestionnaireGroup rCategory = context.QuestionnaireGroups.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingQuestionnaireGroup = rCategory;
                        model.QuestionnaireGroupExists = true;
                    }

                }
                //model.AllQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
                model.AllQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteQuestionnaireGroup(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    QuestionnaireGroup rCategory = context.QuestionnaireGroups.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("QuestionnaireGroups");
        }
        public ActionResult SaveQuestionnaireGroup(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingQuestionnaireGroupID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                QuestionnaireGroup questionnairegroup = null;
                if (ID != new Guid())
                {
                    questionnairegroup = context.QuestionnaireGroups.Where(x => x.ID == ID).FirstOrDefault();

                }
                else
                {
                    questionnairegroup = new QuestionnaireGroup();
                    questionnairegroup.ID = Guid.NewGuid();
                }
                questionnairegroup.Name = form["Name"];
                Guid qID = new Guid();
                Guid.TryParse(form["QuestionnaireID"], out qID);
                questionnairegroup.QuestionnaireID = qID;
                if (ID == new Guid())
                {
                    context.QuestionnaireGroups.Add(questionnairegroup);
                }

                context.SaveChanges();
            }
            return RedirectToAction("QuestionnaireGroups");
        }


        public ActionResult QuestionnaireGroupQuestions(Guid ID)
        {
            QuestionnaireGroupQuestionsModel model = new QuestionnaireGroupQuestionsModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                DateTime ldNow = DateTime.Now;
                model.AllQuestions = context.Questions.Include("CalculationModel").Include("SubCategory").Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                model.QuestionnaireGroup = context.QuestionnaireGroups.Include("Questionnaire").Where(x => x.ID == ID).FirstOrDefault();
                model.ExistingMapQuestions = new List<QuestionWithOrder>();

                List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup).ToList();
                foreach (DataMapping dataMap in dataMaps)
                {
                    List<QuestionnaireGroup> qGroups = context.QuestionnaireGroups.ToList();
                    if (dataMap.PrimaryID == "F7134FF8-DC86-4447-B38B-636CF78E484C")
                    {
                        int stop = 0;
                    }
                    if (model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).ToList().Count > 0) // if is active.
                    {
                        if (dataMap.SecondaryID.ToUpper() == ID.ToString().ToUpper())
                        {
                            QuestionWithOrder qWithOrder = new QuestionWithOrder();
                            qWithOrder.DisplayOrder = dataMap.DisplayOrder;
                            qWithOrder.Question = model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault();
                            if (qWithOrder.Question != null)
                            {
                                model.ExistingMapQuestions.Add(qWithOrder);
                                model.AllQuestions.Remove(model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault());
                            }
                        }
                        else
                        {
                            Guid lgSecondaryID = new Guid();
                            Guid.TryParse(dataMap.SecondaryID, out lgSecondaryID);
                            if (qGroups.FirstOrDefault(x => x.ID == lgSecondaryID).Deleted == false) //this was put in following feedback that deleted QuestionnaireGroup Questions cannot be applied again.
                            {
                                //as questions can only be assigned to one questionnaire!
                                model.AllQuestions.Remove(model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault());
                            }

                        }
                    }
                }
            }
            return View(model);
        }
        public ActionResult QuestionnaireLocations(Guid ID)
        {
            QuestionnaireLocationsModel model = new QuestionnaireLocationsModel();
            using (SITSASEntities context = new SITSASEntities())
            {
               model.AllLocations = GetLocationsWhereAreaIsLive(context, true);
               model.Questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
               model.QuestionnaireLocationMappings = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.LocationToQuestionnaire && x.SecondaryID == ID.ToString()).ToList();

            }
            return View(model);
        }
        public ActionResult SaveQuestionnaireLocations(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string ID = form["QuestionnaireID"];
                List<DataMapping> existingDataMappings = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.LocationToQuestionnaire && x.SecondaryID == ID.ToString()).ToList();
                context.DataMappings.RemoveRange(existingDataMappings);

                foreach(string key in form.AllKeys)
                {
                    if (key != "QuestionnaireID")
                    {
                        string lsLocationID = key;
                        Guid lgLocationID = new Guid();
                        if (Guid.TryParse(lsLocationID, out lgLocationID))
                        {
                            DataMapping dMap = new DataMapping();
                            dMap.ID = Guid.NewGuid();
                            dMap.SecondaryID = ID;
                            dMap.PrimaryID = lgLocationID.ToString();
                            dMap.OverrideAll = false;
                            dMap.DataMappingTypeID = context.DataMappingTypes.FirstOrDefault(x => x.eNumMapping == (int)eDataMappingType.LocationToQuestionnaire).ID;
                            context.DataMappings.Add(dMap);
                        }
                    }
                }
                context.SaveChanges();
                return RedirectToAction("Questionnaires");
            }
        }
        public ActionResult QuestionnaireQuestions(Guid ID)
        {
            QuestionnaireQuestionsModel model = new QuestionnaireQuestionsModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                DateTime ldNow = DateTime.Now;
                model.AllQuestions = context.Questions.Include("CalculationModel").Include("SubCategory").Include("SubCategory.Category").Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                model.Questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
                model.ExistingMapQuestions = new List<QuestionWithOrder>();

                List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire).ToList();
                foreach (DataMapping dataMap in dataMaps)
                {
                    List<Questionnaire> qs = context.Questionnaires.ToList();
                    if (model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).ToList().Count > 0) // if is active.
                    {
                        if (dataMap.SecondaryID.ToUpper() == ID.ToString().ToUpper())
                        {
                            QuestionWithOrder qWithOrder = new QuestionWithOrder();
                            qWithOrder.DisplayOrder = dataMap.DisplayOrder;
                            //get previous answer here
                            qWithOrder.Question = model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault();
                            if (qWithOrder.Question != null)
                            {
                                model.ExistingMapQuestions.Add(qWithOrder);
                                model.AllQuestions.Remove(model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault());
                            }
                        }
                        else
                        {
                            Guid lgSecondaryID = new Guid();
                            Guid.TryParse(dataMap.SecondaryID, out lgSecondaryID);
                            if (qs.FirstOrDefault(x => x.ID == lgSecondaryID).Deleted == false) //this was put in following feedback that deleted Questionnaire Questions cannot be applied again.
                            {
                                //as questions can only be assigned to one questionnaire!
                                model.AllQuestions.Remove(model.AllQuestions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).FirstOrDefault());
                            }

                        }
                    }
                }
            }
            return View(model);
        }
        public ActionResult SortOrderCategory(Guid ID, string Direction)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                List<DataMapping> dataMaps = null;

                Guid PreviousDisplayOrderID = new Guid();
                int PreviousDisplayOrder = 0;
                DateTime ldNow = DateTime.Now;
                List<Category> categories = null;
                if (Direction == "Up")
                {
                    categories = context.Categories.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList().OrderBy(x => x.DisplayOrder).ToList();
                }
                else
                {
                    categories = context.Categories.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList().OrderByDescending(x => x.DisplayOrder).ToList();
                }
                foreach (Category category in categories)
                {
                    if (category.ID == ID)
                        {
                            if (PreviousDisplayOrder > 0) //not the first item
                            {
                                int CurrentDisplayOrder = category.DisplayOrder;
                                category.DisplayOrder = PreviousDisplayOrder;
                            categories.FirstOrDefault(x => x.ID == PreviousDisplayOrderID).DisplayOrder = CurrentDisplayOrder;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            PreviousDisplayOrder = category.DisplayOrder;
                            PreviousDisplayOrderID = category.ID;
                        }
                   
                }


                context.SaveChanges();
            }
            return RedirectToAction("Categories");
        }
        public ActionResult SortOrderQuestionInQuestionnaire(Guid QuestionID, Guid QuestionnaireID, string Direction)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                List<DataMapping> dataMaps = null;

                Guid PreviousDisplayOrderID = new Guid();
                int PreviousDisplayOrder = 0;
                DateTime ldNow = DateTime.Now;
                List<Question> questions = context.Questions.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                if (Direction == "Up")
                {
                    dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.SecondaryID == QuestionnaireID.ToString()).OrderBy(x => x.DisplayOrder).ToList();

                }
                else
                {
                    dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.SecondaryID == QuestionnaireID.ToString()).OrderByDescending(x => x.DisplayOrder).ToList();

                }
                foreach (DataMapping dataMap in dataMaps)
                {
                    if (questions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).ToList().Count > 0)
                    {
                        if (new Guid(dataMap.PrimaryID) == QuestionID)
                        {
                            if (PreviousDisplayOrder > 0) //not the first item
                            {
                                int CurrentDisplayOrder = dataMap.DisplayOrder;
                                dataMap.DisplayOrder = PreviousDisplayOrder;
                                dataMaps.FirstOrDefault(x => x.ID == PreviousDisplayOrderID).DisplayOrder = CurrentDisplayOrder;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            PreviousDisplayOrder = dataMap.DisplayOrder;
                            PreviousDisplayOrderID = dataMap.ID;
                        }
                    }

                }


                context.SaveChanges();
            }
            return RedirectToAction("QuestionnaireQuestions", new { ID = QuestionnaireID });
        }
        public ActionResult SortOrderQuestionInQuestionnaireGroup(Guid QuestionID, Guid QuestionnaireGroupID, string Direction)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                List<DataMapping> dataMaps = null;

                Guid PreviousDisplayOrderID = new Guid();
                int PreviousDisplayOrder = 0;
                DateTime ldNow = DateTime.Now;
                List<Question> questions = context.Questions.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                if (Direction == "Up")
                {
                    dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.SecondaryID == QuestionnaireGroupID.ToString()).OrderBy(x => x.DisplayOrder).ToList();

                }
                else
                {
                    dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.SecondaryID == QuestionnaireGroupID.ToString()).OrderByDescending(x => x.DisplayOrder).ToList();

                }
                foreach (DataMapping dataMap in dataMaps)
                {
                    if (questions.Where(x => x.ID == new Guid(dataMap.PrimaryID)).ToList().Count > 0)
                    {
                        if (new Guid(dataMap.PrimaryID) == QuestionID)
                        {
                            if (PreviousDisplayOrder > 0) //not the first item
                            {
                                int CurrentDisplayOrder = dataMap.DisplayOrder;
                                dataMap.DisplayOrder = PreviousDisplayOrder;
                                dataMaps.FirstOrDefault(x => x.ID == PreviousDisplayOrderID).DisplayOrder = CurrentDisplayOrder;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            PreviousDisplayOrder = dataMap.DisplayOrder;
                            PreviousDisplayOrderID = dataMap.ID;
                        }
                    }

                }


                context.SaveChanges();
            }
            return RedirectToAction("QuestionnaireGroupQuestions", new { ID = QuestionnaireGroupID });
        }
        public ActionResult AddQuestionFromQuestionnaire(FormCollection form)
        {
            Guid gQuestionnaireID = new Guid();
            Guid.TryParse(form["QuestionnaireID"], out gQuestionnaireID);

            Guid gQuestionID = new Guid();
            Guid.TryParse(form["QuestionID"], out gQuestionID);

            //int liDisplayOrder = 0;
            //int.TryParse(form["DisplayOrder"], out liDisplayOrder);
            using (SITSASEntities context = new SITSASEntities())
            {

                DataMapping dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.SecondaryID == gQuestionnaireID.ToString()).OrderByDescending(x => x.DisplayOrder).FirstOrDefault();

                DataMapping dMap = new DataMapping();
                dMap.ID = Guid.NewGuid();
                dMap.PrimaryID = gQuestionID.ToString();
                dMap.SecondaryID = gQuestionnaireID.ToString();
                if (dataMaps != null)
                {
                    dMap.DisplayOrder = dataMaps.DisplayOrder + 10;
                }
                else
                {
                    dMap.DisplayOrder = 10;
                }

                dMap.DataMappingTypeID = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire).FirstOrDefault().ID;
                context.DataMappings.Add(dMap);
                context.SaveChanges();
            }

            return RedirectToAction("QuestionnaireQuestions", new { ID = gQuestionnaireID });
        }
        public ActionResult AddQuestionFromQuestionnaireGroup(FormCollection form)
        {
            Guid gQuestionnaireGroupID = new Guid();
            Guid.TryParse(form["QuestionnaireGroupID"], out gQuestionnaireGroupID);

            Guid gQuestionID = new Guid();
            Guid.TryParse(form["QuestionID"], out gQuestionID);

            //int liDisplayOrder = 0;
            //int.TryParse(form["DisplayOrder"], out liDisplayOrder);
            using (SITSASEntities context = new SITSASEntities())
            {

                DataMapping dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.SecondaryID == gQuestionnaireGroupID.ToString()).OrderByDescending(x => x.DisplayOrder).FirstOrDefault();

                DataMapping dMap = new DataMapping();
                dMap.ID = Guid.NewGuid();
                dMap.PrimaryID = gQuestionID.ToString();
                dMap.SecondaryID = gQuestionnaireGroupID.ToString();
                if (dataMaps != null)
                {
                    dMap.DisplayOrder = dataMaps.DisplayOrder + 10;
                }
                else
                {
                    dMap.DisplayOrder = 10;
                }

                dMap.DataMappingTypeID = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup).FirstOrDefault().ID;
                context.DataMappings.Add(dMap);
                context.SaveChanges();
            }

            return RedirectToAction("QuestionnaireGroupQuestions", new { ID = gQuestionnaireGroupID });
        }

        public ActionResult RemoveQuestionFromQuestionnaireGroup(Guid QuestionnaireGroupID, Guid QuestionID)
        {

            using (SITSASEntities context = new SITSASEntities())
            {

                DataMapping dMap = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.PrimaryID == QuestionID.ToString() && x.SecondaryID == QuestionnaireGroupID.ToString()).FirstOrDefault();
                if (dMap != null)
                {
                    context.DataMappings.Remove(dMap);
                    context.SaveChanges();
                }

            }

            return RedirectToAction("QuestionnaireGroupQuestions", new { ID = QuestionnaireGroupID });
        }

        public ActionResult RemoveQuestionFromQuestionnaire(Guid QuestionnaireID, Guid QuestionID)
        {

            using (SITSASEntities context = new SITSASEntities())
            {

                DataMapping dMap = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.PrimaryID == QuestionID.ToString() && x.SecondaryID == QuestionnaireID.ToString()).FirstOrDefault();
                if (dMap != null)
                {
                    context.DataMappings.Remove(dMap);
                    context.SaveChanges();
                }

            }

            return RedirectToAction("QuestionnaireQuestions", new { ID = QuestionnaireID });
        }
        #endregion

        #region Questionnaires
        public ActionResult Questionnaires()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionnairesModel model = new QuestionnairesModel();
                model.rights = ContextModel.DetermineAccess();
                List<Questionnaire> tempQuestionnaires = context.Questionnaires.Include("FrequencyProfile").Where(x => x.Deleted == false).ToList();
                List<Questionnaire> userQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                model.ExistingQuestionnaires = new List<Questionnaire>();
                foreach (Questionnaire uQuestionnaire in tempQuestionnaires)
                {
                    if (userQuestionnaires.Contains(uQuestionnaire))
                    {
                        model.ExistingQuestionnaires.Add(uQuestionnaire);
                    }

                }

                return View(model);
            }
        }
        public ActionResult AnswerQuestionnaires(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionnairesModel model = new QuestionnairesModel();
                model.rights = ContextModel.DetermineAccess();
                //model.ExistingQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
                model.ExistingQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                model.ContinueQuestionnaires = new List<Result_Headers>();
                string UserName = User.Identity.Name.Substring(User.Identity.Name.LastIndexOf(@"\") + 1, (User.Identity.Name.Length - User.Identity.Name.LastIndexOf(@"\")) - 1);
                model.ContinueQuestionnaires = context.Result_Headers.Include("Questionnaire").Include("Location").Where(x => x.Submitted == false && x.CompletedBy == UserName).ToList();
                if (ID.HasValue)
                {
                    var continues = model.ContinueQuestionnaires.FirstOrDefault(x => x.ID == ID);
                    if (continues != null)
                    {
                        model.ContinueHeaderID = continues.ID;
                        model.ContinueQuestionnaireID = continues.QuestionnaireID;
                    }
                }
                return View(model);
            }
        }
        public ActionResult ReviewQuestionnaires()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionnairesModel model = new QuestionnairesModel();
                model.rights = ContextModel.DetermineAccess();
                //model.ExistingQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
                model.ExistingQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                return View(model);
            }
        }
        public ActionResult ApprovedQuestionnaires()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionnairesModel model = new QuestionnairesModel();
                model.rights = ContextModel.DetermineAccess();
                //model.ExistingQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
                model.ExistingQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                return View(model);
            }
        }
        public ActionResult CreateUpdateQuestionnaire(Guid? ID, bool IsPart = false)
        {
            CreateUpdateQuestionnaireModel model = new CreateUpdateQuestionnaireModel();
            model.rights = ContextModel.DetermineAccess();
            model.QuestionnaireExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                model.AllFrequencyProfiles = context.FrequencyProfiles.Where(x => x.Deleted == false).ToList();
                if (ID.HasValue)
                {
                    Questionnaire rCategory = context.Questionnaires.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingQuestionnaire = rCategory;
                        model.QuestionnaireExists = true;
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteQuestionnaire(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Questionnaire rCategory = context.Questionnaires.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("Questionnaires");
        }
        public ActionResult SaveQuestionnaire(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingQuestionnaireID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Questionnaire questionnaire = null;
                if (ID != new Guid())
                {
                    questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
                }
                else
                {
                    questionnaire = new Questionnaire();
                    questionnaire.ID = Guid.NewGuid();

                }
                questionnaire.FrequencyProfileID = SITMVCFormHelper.GetGuidFromForm(form, "FrequencyProfileID");
                questionnaire.Name = form["Name"];
                if (ID == new Guid())
                {
                    context.Questionnaires.Add(questionnaire);
                }
                context.SaveChanges();
            }
            return RedirectToAction("Questionnaires");
        }
        public ActionResult ShowResults(Guid QuestionnaireID, bool IncidentType = false, Guid? IncidentTypeID = null)
        {
            Results model = new Results();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.questionnaire = context.Questionnaires.Where(x => x.ID == QuestionnaireID).FirstOrDefault();
                if (IncidentType)
                {
                    model.Header = context.Result_Headers.Include("Result_Answers").Include("Result_Answers.Answer").Include("Result_Answers.Answer.Question").Include("Location").Where(x => x.QuestionnaireID == QuestionnaireID && x.ParentID == null && x.IncidentTypeID == IncidentTypeID.Value && x.Submitted == true).ToList();

                }
                else
                {
                    model.Header = context.Result_Headers.Include("Result_Answers").Include("Result_Answers.Answer").Include("Result_Answers.Answer.Question").Include("Location").Where(x => x.QuestionnaireID == QuestionnaireID && x.ParentID == null && x.IncidentTypeID == null && x.Submitted == true).ToList();

                }
            }
            return View(model);
        }
        public ActionResult ShowApprovedResults(Guid QuestionnaireID, bool IncidentType = false, Guid? IncidentTypeID = null)
        {
            ApprovedResults model = new ApprovedResults();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.questionnaire = context.Questionnaires.Where(x => x.ID == QuestionnaireID).FirstOrDefault();
                if (IncidentType)
                {
                    model.Header = context.Result_Headers_Fixings.Include("Result_Answers_Fixings").Include("Result_Answers_Fixings.Answer").Include("Result_Answers_Fixings.Answer.Question").Include("Location").Where(x => x.QuestionnaireID == QuestionnaireID && x.ParentID == null && x.IncidentTypeID == IncidentTypeID.Value).ToList();

                }
                else
                {
                    model.Header = context.Result_Headers_Fixings.Include("Result_Answers_Fixings").Include("Result_Answers_Fixings.Answer").Include("Result_Answers_Fixings.Answer.Question").Include("Location").Where(x => x.QuestionnaireID == QuestionnaireID && x.ParentID == null && x.IncidentTypeID == null).ToList();

                }
            }
            return View(model);
        }

        public ActionResult ShowApprovedQuestionnaireResults(Guid HeaderID, Guid? SubQuestionnaireID)
        {
            ApprovedResults model = new ApprovedResults();
            using (SITSASEntities context = new SITSASEntities())
            {
                if (SubQuestionnaireID.HasValue)
                {
                    model.Header = context.Result_Headers_Fixings.Include("Result_Answers_Fixings").Include("Result_Answers_Fixings.Answer").Include("Result_Answers_Fixings.Answer.Question").Include("Location").Where(x => x.ParentID == HeaderID && x.QuestionnaireID == SubQuestionnaireID).ToList();
                    model.questionnaire = model.Header.FirstOrDefault().Questionnaire;
                }
                else
                {
                    model.Header = context.Result_Headers_Fixings.Include("Result_Answers_Fixings").Include("Result_Answers_Fixings.Answer").Include("Result_Answers_Fixings.Answer.Question").Include("Location").Where(x => x.ID == HeaderID).ToList();
                    model.questionnaire = model.Header.FirstOrDefault().Questionnaire;
                }
            }
            return View(model);
        }
        public ActionResult ShowQuestionnaireResults(Guid HeaderID, Guid? SubQuestionnaireID)
        {
            Results model = new Results();
            using (SITSASEntities context = new SITSASEntities())
            {
                if (SubQuestionnaireID.HasValue)
                {
                    model.Header = context.Result_Headers.Include("Result_Answers").Include("Result_Answers.Answer").Include("Result_Answers.Answer.Question").Include("Result_Answers.Answer.Question.CalculationModel").Include("Location").Where(x => x.ParentID == HeaderID && x.QuestionnaireID == SubQuestionnaireID).ToList();
                    model.questionnaire = model.Header.FirstOrDefault().Questionnaire;
                }
                else
                {
                    model.Header = context.Result_Headers.Include("Result_Answers").Include("Result_Answers.Answer").Include("Result_Answers.Answer.Question").Include("Result_Answers.Answer.Question.CalculationModel").Include("Location").Where(x => x.ID == HeaderID).ToList();
                    model.questionnaire = model.Header.FirstOrDefault().Questionnaire;
                }

            }
            return View(model);
        }
        public ActionResult AnswerQuestionnaire(Guid ID, bool IsSub = false, Guid? IncidentTypeID = null, bool IsPart = false, Guid? HeaderID = null)
        {

            //Create Questionnaire Results here!!!! - pass back to model lineID's (assign tasks to lines)
            AnswerQuestionnaireModel model = new AnswerQuestionnaireModel();
            model.rights = ContextModel.DetermineAccess();
            List<Area> Areas = new List<Area>();
            using (SITSASEntities context = new SITSASEntities())
            {
                Areas = context.Areas.ToList();
            }
            model.AllLocations = new List<Location>();
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Location> tempLocations = GetLocationsWhereAreaIsLive(context, true);
                foreach (Location tempLocation in tempLocations)
                {
                    if (Areas.Where(x => x.ID == tempLocation.AreaID).ToList().Count > 0)
                    {
                        model.AllLocations.Add(tempLocation);
                    }
                }
                FillQuestionnaireModel(ID, context, model, IsSub);
                if (IncidentTypeID.HasValue)
                {
                    model.IncidentTypeID = IncidentTypeID.Value;
                }
                else
                {
                    model.IncidentTypeID = new Guid();
                }

                if (HeaderID != null)
                {
                    model.ExistingHeader = context.Result_Headers.FirstOrDefault(x => x.ID == HeaderID);
                    model.ExistingAnswers = context.Result_Answers.Include("Answer").Where(x => x.HeaderID == HeaderID && x.Answer != null).ToList();
                }

                model.NewID = Guid.NewGuid();
                List<Location> tempTwoLocations = new List<Location>();
                tempTwoLocations.AddRange(model.AllLocations);
                List<DataMapping> existingDataMappings = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.LocationToQuestionnaire && x.SecondaryID == ID.ToString()).ToList();
                foreach (var location in tempTwoLocations)
                {
                    if (existingDataMappings.FirstOrDefault(x => x.PrimaryID == location.ID.ToString()) == null)
                    {
                        model.AllLocations.Remove(location);
                    }
                }

            }
            if (IsSub || IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }

        public List<Location> GetLocationsWhereAreaIsLive(SITSASEntities context, bool LimitByUser)
        {
            List<Location> model = new List<Location>();
            List<Location> tempLocations = new List<Location>();
            if (LimitByUser)
            {
                List<Location> userLoc = context.GetLocationsForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                List<Location> tempLoc = context.Locations.Include("Area").Where(x => x.Deleted == false).ToList();
                foreach (Location tempLocation in tempLoc)
                {
                    if (userLoc.Contains(tempLocation))
                    {
                        tempLocations.Add(tempLocation);
                    }
                }
            }
            else
            {
                tempLocations = context.Locations.Include("Area").Where(x => x.Deleted == false).ToList();
            }



            List<Area> DeletedAreas = new List<Area>();

            DeletedAreas = context.Areas.Where(x => x.Deleted == true).ToList();
            foreach (Location Location in tempLocations)
            {
                if (DeletedAreas.Where(x => x.ID == Location.AreaID).ToList().Count == 0)
                {
                    model.Add(Location);
                }
            }
            return model;
        }

        public ActionResult SubmitAnswers(FormCollection form, string submit)
        {

            //Dont create Questionnaire - just update answers!!!!!!
            Result_Headers headerInfo = GetHeaderInfoFromForm(form);
            using (SITSASEntities context = new SITSASEntities())
            {
                int QuestionsAnswered = 0;
                List<Question> questions = CalculateScoreHelper.GetQuestionsForQuestionnaireFromForm(form, context, headerInfo.QuestionnaireID);
                foreach (Question question in questions)
                {
                    //if (!string.IsNullOrEmpty(form[question.ID.ToString()]))
                    //{
                    string lsAnswer = form[question.ID.ToString()];
                    Result_Answers ranswer = new Result_Answers();
                    ranswer.ID = Guid.NewGuid();
                    ranswer.HeaderID = headerInfo.ID;

                    if (form.AllKeys.Contains("NA_" + question.ID))
                    {
                        ranswer.RawAnswer = "N/A";
                        ranswer.AnswerID = question.Answers.FirstOrDefault().ID; //this is only used to link the N/A back to a question. 
                    }
                    else
                    {
                        ranswer.RawAnswer = lsAnswer;
                        if (string.IsNullOrEmpty(lsAnswer) || (form.AllKeys.Contains("NAns_" + question.ID)))
                        {
                            if (question.Answers.Count > 0)
                            {
                                ranswer.AnswerID = question.Answers.FirstOrDefault().ID; //this is only used to link the Not Answered back to a question. 
                                ranswer.RawAnswer = "Not Answered";
                                ranswer.RawScore = question.DefaultValue;
                                ranswer.WeightedScore = 1;
                                ranswer.StalenessScore = 1;
                            }
                        }
                        else
                        {
                            ranswer.RawScore = CalculateScoreHelper.CalculateScore(question, lsAnswer, context, form, ranswer, headerInfo);
                            string liAnswer = ((float)question.Weighting.Value / 4).ToString();
                            if (!string.IsNullOrEmpty(question.ID.ToString() + "-NOTES"))
                            {
                                ranswer.Comments = form[question.ID.ToString() + "-NOTES"];
                                if (ranswer.Comments == "")
                                {
                                    ranswer.Comments = null; //as we're writing string.empty. 
                                }
                            }
                            decimal ldAnswer = 0;
                            if (decimal.TryParse(liAnswer, out ldAnswer))
                            {
                                ranswer.WeightedScore = ldAnswer;
                            }
                            else
                            {
                                ranswer.WeightedScore = 0;
                            }
                            ranswer.StalenessScore = CalculateScoreHelper.CalculateStalenessScore(ranswer.RawScore.Value, headerInfo.SelectedDate, question.StalenessProfile, DateTime.Now.Date);
                        }


                    }

                    context.Result_Answers.Add(ranswer);
                    QuestionsAnswered += 1;
                    //}
                }
                bool SaveAndContinue = false;
                if (QuestionsAnswered > 0)
                {
                    if (form.AllKeys.Contains("submit"))
                    {
                        headerInfo.Submitted = true;
                    }
                    Guid PartEnteredID = SITMVCFormHelper.GetGuidFromForm(form, "ContinuationID");
                    if (PartEnteredID != null)
                    {
                        if (PartEnteredID != new Guid())
                        {
                            //clear header and lines
                            context.ClearPartEnteredQuestionnaire(PartEnteredID);

                            foreach (Task task in context.Tasks.Where(x => x.HeaderID == PartEnteredID).ToList())
                            {
                                task.HeaderID = headerInfo.ID;
                            }
                        }
                    }


                    context.Result_Headers.Add(headerInfo);
                    context.SaveChanges();

                    if (headerInfo.Submitted == true)
                    {
                        CreateDocument(headerInfo.QuestionnaireID, "PDF", headerInfo.ID);
                    }
                    else
                    {
                        return this.Json(headerInfo.ID, JsonRequestBehavior.AllowGet);

                    }
                }
                else
                {
                    return RedirectToAction("Questionnaires");
                }

            }
            if (headerInfo.IncidentTypeID != null)
            {
                return RedirectToAction("ShowResults", new { QuestionnaireID = headerInfo.QuestionnaireID, IncidentTypeID = headerInfo.IncidentTypeID, IncidentType = true });
            }
            else
            {
                return RedirectToAction("ShowResults", new { QuestionnaireID = headerInfo.QuestionnaireID });
            }

        }

        //public int CalculateScore(Question question, string RawAnswer, SITSASEntities context, FormCollection form, Result_Answers ranswer, Result_Headers header)
        //{
        //    switch (question.CalculationModel.eNumMapping)
        //    {
        //        case (int)eCalculationModels.DropDownLists:
        //            {
        //                if (question.Answers.Count > 0)
        //                {
        //                    Guid lgAnswer = new Guid();
        //                    Guid.TryParse(RawAnswer, out lgAnswer);
        //                    Answer answer = question.Answers.FirstOrDefault(x => x.ID == lgAnswer);
        //                    if (answer != null)
        //                    {
        //                        ranswer.AnswerID = answer.ID;
        //                        ranswer.RawAnswer = answer.Description;
        //                        return answer.Score;
        //                    }
        //                }

        //                break;
        //            }
        //        case (int)eCalculationModels.NumericValue:
        //            {
        //                int liAnswer = 0;
        //                int.TryParse(RawAnswer, out liAnswer);
        //                if (question.Answers.Count > 0)
        //                {
        //                    foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList()) //SCORE
        //                    {
        //                        bool MeetsCritera = false;
        //                        if (ans.Answer_ScoreMappings.Count > 0)
        //                        {
        //                            foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings) //COMPONENT
        //                            {
        //                                MeetsCritera = CheckCritera(liAnswer, scoreMap);
        //                            }
        //                        }
        //                        if (MeetsCritera)
        //                        {
        //                            ranswer.AnswerID = ans.ID;
        //                            return ans.Score;
        //                        }

        //                    }
        //                }
        //                break;
        //            }
        //        case (int)eCalculationModels.QuestionnaireResult:
        //            {
        //                int Score = 0;
        //                if (question.SubQuestionnaireID.HasValue)
        //                {
        //                    Guid SubQuestionnaireID = question.SubQuestionnaireID.Value;
        //                    if (SubQuestionnaireID != null)
        //                    {

        //                        //create sub results header
        //                        Result_Headers subrHeader = new Result_Headers();
        //                        subrHeader.ID = Guid.NewGuid();
        //                        subrHeader.QuestionnaireID = SubQuestionnaireID;
        //                        subrHeader.CompletedBy = header.CompletedBy;
        //                        subrHeader.CreatedDate = header.CreatedDate;
        //                        subrHeader.LocationID = header.LocationID;
        //                        subrHeader.SelectedDate = header.SelectedDate;
        //                        subrHeader.ParentID = header.ID;

        //                        List<Question> subQuestions = GetQuestionsForQuestionnaireFromForm(form, context, SubQuestionnaireID);
        //                        foreach (Question subQuestion in subQuestions)
        //                        {
        //                            Result_Answers subranswer = new Result_Answers();
        //                            subranswer.ID = Guid.NewGuid();
        //                            subranswer.HeaderID = subrHeader.ID;
        //                            if (!string.IsNullOrEmpty(form[subQuestion.ID.ToString()]))
        //                            {
        //                                string lsAnswer = form[subQuestion.ID.ToString()];
        //                                subranswer.RawAnswer = lsAnswer;
        //                                int subItemScore = CalculateScore(subQuestion, lsAnswer, context, form, subranswer, subrHeader);
        //                                subranswer.RawScore = subItemScore;
        //                                Score += subItemScore;
        //                            }

        //                            subranswer.WeightedScore = subranswer.RawScore * (subQuestion.Weighting / 4);
        //                            subranswer.StalenessScore = subranswer.RawScore;
        //                            context.Result_Answers.Add(subranswer);



        //                        }
        //                        context.Result_Headers.Add(subrHeader);
        //                    }


        //                }
        //                ranswer.RawAnswer = Score.ToString();
        //                if (question.Answers.Count > 0)
        //                {
        //                    foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList()) //SCORE
        //                    {
        //                        bool MeetsCritera = false;
        //                        if (ans.Answer_ScoreMappings.Count > 0)
        //                        {
        //                            foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings) //COMPONENT
        //                            {
        //                                MeetsCritera = CheckCritera(Score, scoreMap);
        //                                if (!MeetsCritera)
        //                                {
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                        if (MeetsCritera)
        //                        {

        //                            ranswer.AnswerID = ans.ID;
        //                            return ans.Score;
        //                        }
        //                    }
        //                }

        //                break;

        //            }
        //        case (int)eCalculationModels.TimeSinceARecordedDate:
        //            {
        //                DateTime ldAnswer = DateTime.MinValue;
        //                DateTime.TryParse(RawAnswer, out ldAnswer);
        //                if (question.Answers.Count > 0)
        //                {
        //                    foreach (Answer ans in question.Answers.Where(x => x.Deleted == false).ToList().OrderBy(x => x.Score)) //SCORE
        //                    {
        //                        bool MeetsCritera = false;
        //                        if (ans.Answer_ScoreMappings.Count > 0)
        //                        {
        //                            foreach (Answer_ScoreMappings scoreMap in ans.Answer_ScoreMappings) //COMPONENT
        //                            {
        //                                MeetsCritera = CheckCritera(ldAnswer, scoreMap, header.SelectedDate);
        //                                if (!MeetsCritera)
        //                                {
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                        if (MeetsCritera)
        //                        {
        //                            ranswer.AnswerID = ans.ID;
        //                            return ans.Score;
        //                        }
        //                    }
        //                }
        //                break;
        //            }
        //        case (int)eCalculationModels.ManualEntry:
        //            {
        //                int liAnswer = 0;
        //                int.TryParse(RawAnswer, out liAnswer);
        //                Answer manAns = question.Answers.Where(x => x.Deleted == true && x.Description == "Manual Entry").FirstOrDefault(); //DEFAULT ANSWER
        //                if (manAns != null)
        //                {
        //                    ranswer.AnswerID = manAns.ID;
        //                }
        //                return liAnswer;
        //            }
        //    }
        //    Answer defAns = question.Answers.Where(x => x.Deleted == true && x.Description == "Answer does not meet any calculations.").FirstOrDefault(); //DEFAULT ANSWER
        //    if (defAns != null)
        //    {
        //        ranswer.AnswerID = defAns.ID;
        //    }
        //    return 0;
        //}
        //public bool CheckCritera(DateTime ldAnswer, Answer_ScoreMappings scoreMap, DateTime QuestionnaireDate)
        //{
        //    DateTime myDate = DateTime.MinValue;
        //    switch (scoreMap.DateUnit)
        //    {
        //        case "days":
        //            {
        //                myDate = QuestionnaireDate.AddDays(scoreMap.Value * -1);
        //                break;
        //            }
        //        case "weeks":
        //            {
        //                myDate = QuestionnaireDate.AddDays((scoreMap.Value * 7) * -1);
        //                break;
        //            }
        //        case "months":
        //            {
        //                myDate = QuestionnaireDate.AddMonths((int)scoreMap.Value * -1);
        //                break;
        //            }
        //        case "years":
        //            {
        //                myDate = QuestionnaireDate.AddYears((int)scoreMap.Value * -1);
        //                break;
        //            }

        //    }
        //    bool MeetsCritera = false;
        //    switch (scoreMap.Answer_Operators.eNumMapping)
        //    {
        //        //case (int)eOperator.EqualTo:
        //        //    {
        //        //        if (ldAnswer.Date == myDate.Date)
        //        //        {
        //        //            MeetsCritera = true;
        //        //        }
        //        //        else
        //        //        {
        //        //            MeetsCritera = false;
        //        //        }
        //        //        break;
        //        //    }
        //        //case (int)eOperator.GreaterThan:
        //        //    {
        //        //        if (ldAnswer.Date > myDate.Date)
        //        //        {
        //        //            MeetsCritera = true;
        //        //        }
        //        //        else
        //        //        {
        //        //            MeetsCritera = false;
        //        //        }
        //        //        break;
        //        //    }
        //        case (int)eOperator.GreaterThanorEqualTo:
        //            {
        //                if (ldAnswer.Date >= myDate.Date && ldAnswer.Date <= QuestionnaireDate.Date)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //            //case (int)eOperator.LessThan:
        //            //    {
        //            //        if (ldAnswer.Date < myDate.Date)
        //            //        {
        //            //            MeetsCritera = true;
        //            //        }
        //            //        else
        //            //        {
        //            //            MeetsCritera = false;
        //            //        }
        //            //        break;
        //            //    }
        //            //case (int)eOperator.LessThanOrEqualTo:
        //            //    {
        //            //        if (ldAnswer.Date <= myDate.Date)
        //            //        {
        //            //            MeetsCritera = true;
        //            //        }
        //            //        else
        //            //        {
        //            //            MeetsCritera = false;
        //            //        }
        //            //        break;
        //            //    }
        //    }
        //    return MeetsCritera;
        //}
        //public bool CheckCritera(int Score, Answer_ScoreMappings scoreMap)
        //{
        //    bool MeetsCritera = false;
        //    switch (scoreMap.Answer_Operators.eNumMapping)
        //    {
        //        case (int)eOperator.EqualTo:
        //            {
        //                if (Score == scoreMap.Value)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //        case (int)eOperator.GreaterThan:
        //            {
        //                if (Score > scoreMap.Value)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //        case (int)eOperator.GreaterThanorEqualTo:
        //            {
        //                if (Score >= scoreMap.Value)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //        case (int)eOperator.LessThan:
        //            {
        //                if (Score < scoreMap.Value)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //        case (int)eOperator.LessThanOrEqualTo:
        //            {
        //                if (Score <= scoreMap.Value)
        //                {
        //                    MeetsCritera = true;
        //                }
        //                else
        //                {
        //                    MeetsCritera = false;
        //                }
        //                break;
        //            }
        //    }
        //    return MeetsCritera;
        //}
        public Result_Headers GetHeaderInfoFromForm(FormCollection form)
        {
            Result_Headers model = new Result_Headers();

            //model.CompletedBy = form["username"];
            model.CompletedBy = User.Identity.Name.Substring(User.Identity.Name.LastIndexOf(@"\") + 1, (User.Identity.Name.Length - User.Identity.Name.LastIndexOf(@"\")) - 1);
            DateTime selectedDate = new DateTime(SITMVCFormHelper.GetIntegerFromForm(form, "YearSelection"), SITMVCFormHelper.GetIntegerFromForm(form, "MonthSelection"), 1);
            model.CreatedDate = DateTime.Now;
            model.SelectedDate = selectedDate;
            if (form.AllKeys.Contains("NewID"))
            {
                model.ID = SITMVCFormHelper.GetGuidFromForm(form, "NewID");
            }

            Guid lgLocationID = new Guid();
            Guid.TryParse(form["LocationID"], out lgLocationID);
            model.LocationID = lgLocationID;
            Guid lgQuestionnaireID = new Guid();
            Guid.TryParse(form["QuestionnaireID"], out lgQuestionnaireID);
            model.QuestionnaireID = lgQuestionnaireID;
            if (form.AllKeys.Contains("IncidentTypeID"))
            {
                Guid lgIncidentTypeID = new Guid();
                Guid.TryParse(form["IncidentTypeID"], out lgIncidentTypeID);
                model.IncidentTypeID = lgIncidentTypeID;
            }

            return model;
        }
        //public List<Question> GetQuestionsForQuestionnaireFromForm(FormCollection form, SITSASEntities context, Guid questionnaireID)
        //{
        //    List<Question> model = new List<Question>();
        //    Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == questionnaireID).FirstOrDefault();
        //    List<QuestionnaireGroup> questionnaireGroups = new List<QuestionnaireGroup>();

        //    foreach (QuestionnaireGroup group in questionnaire.QuestionnaireGroups)
        //    {
        //        List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.SecondaryID == group.ID).ToList();
        //        foreach (DataMapping dataMap in dataMaps)
        //        {
        //            Question question = context.Questions.Where(x => x.ID == dataMap.PrimaryID).FirstOrDefault();
        //            if (question != null)
        //            {
        //                if (form.AllKeys.ToList().Contains(question.ID.ToString()))
        //                {
        //                    model.Add(question);
        //                }
        //            }
        //        }
        //    }


        //    return model;
        //}


        public ActionResult FixScores()
        {
            FixScoresModel model = new FixScoresModel();
            model.AllLocations = new List<Location>();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.AllLocations = GetLocationsWhereAreaIsLive(context, true);
                //model = context.Locations.Where(x => x.Deleted == false).ToList();
            }
            return View(model);
        }
        public ActionResult AnswerOneQuestion()
        {
            AnswerOneQuestionModel model = new AnswerOneQuestionModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.AllLocations = GetLocationsWhereAreaIsLive(context, true);
                DateTime ldNow = DateTime.Now;
                List<Questionnaire> userVisibleQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                model.AllQuestions = context.Questions.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();


                //model = context.Locations.Where(x => x.Deleted == false).ToList();
            }
            return View(model);
        }

        public ActionResult AnswerSingleQuestionForLocations(FormCollection form)
        {
            AnswerOneQuestionForLocationsModel model = new AnswerOneQuestionForLocationsModel();
            model.rights = ContextModel.DetermineAccess();
            string lsDate = form["FixDate"];
            DateTime ldDate = DateTime.MinValue;
            DateTime.TryParse(lsDate, out ldDate);
            model.selectedDate = ldDate;
            Guid lgQuestion = new Guid();
            string lsQID = form["QuestionID"];
            Guid.TryParse(lsQID, out lgQuestion);

            using (SITSASEntities context = new SITSASEntities())
            {
                model.question = context.Questions.Include("CalculationModel").Include("Answers").Where(x => x.ID == lgQuestion).FirstOrDefault();


                List<Guid> AllLocations = new List<Guid>();
                List<Location> Locations = context.Locations.ToList();
                model.Locations = new List<Location>();
                foreach (string key in form.AllKeys)
                {
                    Guid res = new Guid();
                    if (Guid.TryParse(key, out res))
                    {
                        Location Location = Locations.FirstOrDefault(x => x.ID == res);
                        model.Locations.Add(Location);

                        Result_Answers ans = GetMostRecentAnswer(model.question, Location);

                        if (model.previousAnswers == null)
                        {
                            model.previousAnswers = new SortedList<Guid, Result_Answers>();
                        }

                        model.previousAnswers.Add(Location.ID, ans);


                        if (model.allComments == null)
                        {
                            model.allComments = new SortedList<Guid, string>();
                        }
                        model.allComments.Add(Location.ID, GetAllComments(model.question, Location));
                        //AllLocations.Add(res);
                    }
                }

            }
            return View(model);
        }
        public Result_Answers GetMostRecentAnswer(Question question, Location Location)
        {
            Result_Answers model = null;
            List<Answer> ansColl = question.Answers.Where(x => x.Result_Answers.Where(a => a.Result_Headers.LocationID == Location.ID).ToList().Count > 0).ToList();
            if (ansColl != null)
            {

                foreach (Answer ans in ansColl.Where(x => x.Result_Answers.ToList().Count > 0))
                {
                    foreach (Result_Answers rAns in ans.Result_Answers.OrderByDescending(x => x.Result_Headers.SelectedDate))
                    {
                        if (model == null)
                        {
                            model = rAns;
                        }
                        else
                        {
                            if (model.Result_Headers.SelectedDate < rAns.Result_Headers.SelectedDate)
                            {
                                model = rAns;
                            }
                        }
                    }
                }

                //Answer ans = ansColl.OrderByDescending(x => x.Result_Answers.OrderByDescending(y => y.Result_Headers.SelectedDate).FirstOrDefault()).FirstOrDefault(); //latest answer;
                //if (ans != null)
                //{
                //    return ans;

                //}
            }
            return model;
        }
        public string GetAllComments(Question question, Location Location)
        {
            string model = string.Empty;
            List<Answer> ansColl = question.Answers.Where(x => x.Result_Answers.Where(a => a.Result_Headers.LocationID == Location.ID).ToList().Count > 0).ToList();
            if (ansColl != null)
            {

                foreach (Answer ans in ansColl.Where(x => x.Result_Answers.ToList().Count > 0))
                {
                    foreach (Result_Answers rAns in ans.Result_Answers.OrderBy(x => x.Result_Headers.SelectedDate))
                    {
                        if (!string.IsNullOrEmpty(rAns.Comments))
                        {
                            model = model + rAns.Comments + "<br/>";
                        }

                    }
                }

                //Answer ans = ansColl.OrderByDescending(x => x.Result_Answers.OrderByDescending(y => y.Result_Headers.SelectedDate).FirstOrDefault()).FirstOrDefault(); //latest answer;
                //if (ans != null)
                //{
                //    return ans;

                //}
            }
            return model;
        }
        public ActionResult SubmitMultiLocationOneQuestion(FormCollection form)
        {
            string gID = form["QuestionID"];
            Guid GID = new Guid();
            Guid.TryParse(gID, out GID);


            DateTime selectedDate = DateTime.MinValue;
            DateTime.TryParse(form["dateCompleted"], out selectedDate);


            DateTime ldNow = DateTime.Now;
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Location> allLocations = context.Locations.ToList();
                Question question = context.Questions.Where(x => x.ID == GID).FirstOrDefault();
                List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionaireGroup && x.PrimaryID == question.ID.ToString()).ToList();
                if (dataMaps != null)
                {

                    Guid groupID = new Guid(dataMaps.FirstOrDefault().SecondaryID);

                    QuestionnaireGroup qGroup = context.QuestionnaireGroups.Where(x => x.ID == groupID).FirstOrDefault();

                    foreach (Location Location in allLocations)
                    {
                        if (!string.IsNullOrEmpty(form[Location.ID.ToString()]))
                        {
                            Result_Headers headerInfo = new Result_Headers();
                            headerInfo.ID = Guid.NewGuid();
                            headerInfo.CompletedBy = form["username"];
                            headerInfo.CreatedDate = ldNow;
                            headerInfo.SelectedDate = selectedDate;
                            headerInfo.LocationID = Location.ID;
                            headerInfo.QuestionnaireID = qGroup.QuestionnaireID.Value;

                            string lsAnswer = form[Location.ID.ToString()];
                            Result_Answers ranswer = new Result_Answers();
                            ranswer.ID = Guid.NewGuid();
                            ranswer.HeaderID = headerInfo.ID;

                            if (form.AllKeys.Contains("NA_" + Location.ID))
                            {
                                ranswer.RawAnswer = "N/A";
                                ranswer.AnswerID = question.Answers.FirstOrDefault().ID; //this is only used to link the N/A back to a question. 
                            }
                            else
                            {
                                ranswer.RawAnswer = lsAnswer;
                                ranswer.RawScore = CalculateScoreHelper.CalculateScore(question, lsAnswer, context, form, ranswer, headerInfo);
                                string liAnswer = ((float)question.Weighting.Value / 4).ToString();
                                if (!string.IsNullOrEmpty(Location.ID.ToString() + "-NOTES"))
                                {
                                    ranswer.Comments = form[Location.ID.ToString() + "-NOTES"];
                                    if (ranswer.Comments == "")
                                    {
                                        ranswer.Comments = null; //as we're writing string.empty. 
                                    }
                                }
                                decimal ldAnswer = 0;
                                if (decimal.TryParse(liAnswer, out ldAnswer))
                                {
                                    ranswer.WeightedScore = ldAnswer;
                                }
                                else
                                {
                                    ranswer.WeightedScore = 0;
                                }
                                ranswer.StalenessScore = CalculateScoreHelper.CalculateStalenessScore(ranswer.RawScore.Value, headerInfo.SelectedDate, question.StalenessProfile, DateTime.Now.Date);
                            }

                            context.Result_Answers.Add(ranswer);
                            context.Result_Headers.Add(headerInfo);
                        }
                    }

                }
                context.SaveChanges();
            }
            return RedirectToAction("Success");
        }


        public ActionResult ViewFixingResults(DateTime date)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                var tempmodel = context.Result_Answers_Fixings.Include("Result_Headers_Fixings").Include("Answer").Include("Answer.Question").Include("Result_Headers_Fixings.Location").Where(x => x.Result_Headers_Fixings.SelectedDate == date).OrderBy(x => x.Result_Headers_Fixings.SelectedDate).ToList();
                return View(tempmodel);
            }

        }
        public ActionResult FixAreaScores(FormCollection form)
        {
            string lsDate = form["FixDate"];
            DateTime ldDate = DateTime.MinValue;
            DateTime.TryParse(lsDate, out ldDate);
            List<Guid> AllLocations = new List<Guid>();
            foreach (string key in form.AllKeys)
            {
                Guid res = new Guid();
                if (Guid.TryParse(key, out res))
                {
                    AllLocations.Add(res);
                }
            }
            foreach (Guid Location in AllLocations)
            {
                ScoreFixingHelper.ScoreFixingHelper.FixScores(Location, ldDate, "Manual Score Fix");
            }

            return RedirectToAction("ViewFixingResults", new { Date = ldDate });
        }
        #endregion
        public void FillQuestionnaireModel(Guid ID, SITSASEntities context, AnswerQuestionnaireModel model, bool IsSub)
        {
            model.questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
            model.questionnaireGroup = new List<Models.QuestionnaireQuestions>();
            model.IsSub = IsSub;
            //foreach (QuestionnaireGroup group in model.questionnaire.QuestionnaireGroups)
            //{
            List<DataMapping> dataMaps = context.DataMappings.Where(x => x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire && x.SecondaryID == ID.ToString()).ToList();
            QuestionnaireQuestions qgModel = new QuestionnaireQuestions();
            qgModel.Categories = new List<QuestionnaireCategory>();

            DateTime ldNow = DateTime.Now;
            foreach (Category arCategory in context.Categories.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList())
            {
                QuestionnaireCategory qgaRiskCategory = new QuestionnaireCategory();
                qgaRiskCategory.Category = arCategory;
                qgaRiskCategory.SubCategories = new List<QuestionnaireSubCategories>();
                List<SubCategory> dashCats = context.SubCategories.Where(x => x.CategoryID == arCategory.ID).ToList();

                foreach (SubCategory dashCat in dashCats)
                {
                    QuestionnaireSubCategories qgdCategory = new QuestionnaireSubCategories();
                    qgdCategory.SubCategory = dashCat;
                    qgdCategory.questions = new List<QuestionWithOrder>();
                    foreach (DataMapping dataMap in dataMaps)
                    {
                        Question question = context.Questions.Include("CalculationModel").Include("Answers").Where(x => x.ID == new Guid(dataMap.PrimaryID) && x.StartDate < ldNow && x.EndDate > ldNow && x.Deleted == false).FirstOrDefault();
                        if (question != null)
                        {
                            if (question.SubCategoryID == dashCat.ID)
                            {
                                QuestionWithOrder questWorder = new QuestionWithOrder();
                                questWorder.DisplayOrder = dataMap.DisplayOrder;
                                //get previous answer here
                                questWorder.Question = question;
                                qgdCategory.questions.Add(questWorder);
                            }
                        }
                    }
                    if (qgdCategory.questions.Count > 0)
                    {
                        qgaRiskCategory.SubCategories.Add(qgdCategory);
                    }

                }
                if (qgaRiskCategory.SubCategories.Count > 0)
                {
                    qgModel.Categories.Add(qgaRiskCategory);
                }

            }
            if (qgModel.Categories.Count > 0)
            {
                model.questionnaireGroup.Add(qgModel);
            }


            //}
        }

        public Result_Answers GetPreviousAnswer(Question question, Location location)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Result_Answers answer = context.Result_Answers.Where(x => x.Answer.QuestionID == question.ID && x.Result_Headers.LocationID == location.ID && x.Result_Headers.Submitted == true).OrderByDescending(x => x.Result_Headers.SelectedDate).FirstOrDefault();
                return answer;
            }
        }

        #region Questions
        public ActionResult Questions()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionsModel model = new QuestionsModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingQuestions = context.Questions.Include("CalculationModel").Include("SubCategory").Include("SubCategory.Category").Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow).ToList();
                return View(model);
            }
        }
        public ActionResult RetiredQuestions()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionsModel model = new QuestionsModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingQuestions = context.Questions.Include("CalculationModel").Where(x => x.Deleted == false && x.EndDate < ldNow).ToList();
                return View(model);
            }
        }
        public ActionResult FutureQuestions()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                QuestionsModel model = new QuestionsModel();
                model.rights = ContextModel.DetermineAccess();
                DateTime ldNow = DateTime.Now;
                model.ExistingQuestions = context.Questions.Include("CalculationModel").Where(x => x.Deleted == false && x.StartDate > ldNow && x.EndDate > ldNow).ToList();
                return View(model);
            }
        }
        public ActionResult CreateUpdateQuestion(Guid? ID, bool IsPart = false)
        {
            CreateUpdateQuestionModel model = new CreateUpdateQuestionModel();
            model.rights = ContextModel.DetermineAccess();
            model.QuestionExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Question rCategory = context.Questions.Include("CalculationModel").Include("Answers").Include("Answers.Result_Answers").Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingQuestion = rCategory;
                        model.QuestionExists = true;
                    }

                }
                model.AllCalculationModels = context.CalculationModels.Where(x => x.Deleted == false).ToList();
                model.AllSubCategories = context.SubCategories.Where(x => x.Deleted == false).ToList();
                model.AllQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
                DateTime ldNow = DateTime.Now.Date;
                model.RetiredQuestions = context.Questions.Include("CalculationModel").Where(x => x.Deleted == false && x.EndDate < ldNow).ToList();
                SystemSetting includeStaleness = context.SystemSettings.FirstOrDefault(x => x.Name == "ShowStaleness");
                bool lbIncludeStaleness = false;
                bool.TryParse(includeStaleness.Value, out lbIncludeStaleness);
                model.ShowStalenessProfile = lbIncludeStaleness;
                if (lbIncludeStaleness)
                {
                    model.AllStalenessProfiles = context.StalenessProfiles.Where(x => x.Deleted == false).ToList();
                }
                else
                {
                    model.AllStalenessProfiles = new List<StalenessProfile>();
                    model.DefaultStalenessID = context.StalenessProfiles.FirstOrDefault().ProfileID;
                }

            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteQuestion(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Question rCategory = context.Questions.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        //check here to make sure its not linked to any questionnaire groups - if it is do not allow delete (probs do this on the view)
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("Questions");
        }



        public ActionResult SaveQuestion(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingQuestionID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Question question = null;
                if (ID != new Guid())
                {
                    question = context.Questions.Where(x => x.ID == ID).FirstOrDefault();
                }
                else
                {
                    question = new Question();
                    question.ID = Guid.NewGuid();
                }

                if (form.AllKeys.Contains("Name"))
                {
                    question.Name = form["Name"];
                    question.ToolTipDescription = form["ToolTipDescription"];

                    if (form.AllKeys.Contains("CanBeNAns"))
                    {

                        question.CanBeNotAnswered = true;
                    }
                    else
                    {

                        question.CanBeNotAnswered = false;
                    }
                }
                if (form.AllKeys.Contains("CalculationModelID"))
                {
                    Guid lgCalModelID = new Guid();
                    Guid.TryParse(form["CalculationModelID"], out lgCalModelID);
                    question.CalculationModelID = lgCalModelID;
                    CalculationModel cM = context.CalculationModels.Where(x => x.ID == lgCalModelID).FirstOrDefault();
                    if (form.AllKeys.Contains("CanBeNA"))
                    {
                        if (!question.CanBeNA) //just changed
                        {
                            if (cM.eNumMapping == (int)eCalculationModels.DropDownLists)
                            {
                                Answer ans = context.Answers.Where(x => x.QuestionID == question.ID && x.Description == "N/A").FirstOrDefault();
                                if (ans != null)
                                {
                                    ans.Deleted = false;
                                }
                                else
                                {
                                    Answer newAns = new Answer();
                                    newAns.ID = Guid.NewGuid();
                                    newAns.QuestionID = question.ID;
                                    newAns.Score = 0;
                                    newAns.Deleted = true;

                                    newAns.Description = "N/A";
                                    context.Answers.Add(newAns);
                                }
                            }
                            question.CanBeNA = true;
                        }

                    }
                    else
                    {
                        if (question.CanBeNA)
                        {
                            if (cM.eNumMapping == (int)eCalculationModels.DropDownLists)
                            {
                                //remove answer
                                Answer ans = context.Answers.Where(x => x.QuestionID == question.ID && x.Description == "N/A").FirstOrDefault();
                                if (ans != null)
                                {
                                    ans.Deleted = true;
                                }
                            }
                            question.CanBeNA = false;
                        }

                    }
                    if (cM.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                    {
                        Guid subQID = new Guid();
                        Guid.TryParse(form["SubQuestionnaireID"], out subQID);
                        question.SubQuestionnaireID = subQID;
                        if (form.AllKeys.Contains("SubQuestionnaireMultipleTimes"))
                        {
                            question.SubQuestionnaireMultipleTimes = true;
                        }
                        else
                        {
                            question.SubQuestionnaireMultipleTimes = false;
                        }
                    }

                    if (ID == new Guid())
                    {
                        context.Questions.Add(question);
                        if (cM.eNumMapping == (int)eCalculationModels.ManualEntry)
                        {
                            Answer ans = new Answer();
                            ans.ID = Guid.NewGuid();
                            ans.QuestionID = question.ID;
                            ans.Score = question.DefaultValue;
                            ans.Deleted = true;

                            ans.Description = "Manual Entry";
                            context.Answers.Add(ans);
                        }
                        if (cM.eNumMapping == (int)eCalculationModels.NumericValue || cM.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                        {
                            Answer ans = new Answer();
                            ans.ID = Guid.NewGuid();
                            ans.QuestionID = question.ID;
                            ans.Score = question.DefaultValue;
                            ans.Deleted = false;
                            ans.Description = "Is Greater Than 0";
                            ans.CatchAll = true;
                            context.Answers.Add(ans);

                            Answer_ScoreMappings scoreMap = new Answer_ScoreMappings();
                            scoreMap.ID = Guid.NewGuid();
                            scoreMap.AnswerID = ans.ID;
                            scoreMap.Value = 0;
                            scoreMap.OperatorID = context.Answer_Operators.Where(x => x.Operator.Trim() == ">").FirstOrDefault().ID;

                            context.Answer_ScoreMappings.Add(scoreMap);
                        }

                        if (cM.eNumMapping == (int)eCalculationModels.TimeSinceARecordedDate)
                        {
                            int liWithin = 0;
                            int.TryParse(form["Within"], out liWithin);

                            string Unit = form["Unit"];

                            Answer_Operators GreaterThanEqualTo = context.Answer_Operators.Where(X => X.ID == new Guid("7CA1A1E1-793A-4171-BEEF-173561F86494")).FirstOrDefault();
                            Answer ans = new Answer();
                            ans.ID = Guid.NewGuid();
                            ans.QuestionID = question.ID;
                            ans.Score = question.DefaultValue;
                            ans.Deleted = false;
                            ans.Description = "All Other Dates";
                            ans.CatchAll = true;
                            context.Answers.Add(ans);

                            Answer_ScoreMappings scoreMaxMap = new Answer_ScoreMappings();
                            scoreMaxMap.ID = Guid.NewGuid();
                            scoreMaxMap.AnswerID = ans.ID;
                            scoreMaxMap.OperatorID = GreaterThanEqualTo.ID;
                            scoreMaxMap.Value = 100;
                            scoreMaxMap.DateUnit = "Years";
                            context.Answer_ScoreMappings.Add(scoreMaxMap);

                        }


                    }
                }
                if (form.AllKeys.Contains("SubCategoryID"))
                {

                    Guid lgDashCatID = new Guid();
                    Guid.TryParse(form["SubCategoryID"], out lgDashCatID);
                    question.SubCategoryID = lgDashCatID;
                }
                if (form.AllKeys.Contains("StalenessProfileID"))
                {

                    Guid lgDashCatID = new Guid();
                    Guid.TryParse(form["StalenessProfileID"], out lgDashCatID);
                    question.StalenessProfileID = lgDashCatID;
                }
                if (form.AllKeys.Contains("StartDate"))
                {
                    question.StartDate = SITMVCFormHelper.GetDateTimeFromForm(form, "StartDate");
                }
                if (form.AllKeys.Contains("EndDate"))
                {
                    question.EndDate = SITMVCFormHelper.GetDateTimeFromForm(form, "EndDate");
                    if (question.EndDate == DateTime.MinValue)
                    {

                        question.EndDate = new DateTime(2099, 1, 1);
                    }
                }
                //if (form.AllKeys.Contains("EndDate"))
                //{
                //    DateTime ldEndDate = DateTime.MinValue;
                //    DateTime.TryParse(form["EndDate"], out ldEndDate);
                //    question.EndDate = ldEndDate;
                //}
                if (form.AllKeys.Contains("Weighting"))
                {
                    int liWeighting = 0;
                    int.TryParse(form["Weighting"], out liWeighting);
                    question.Weighting = liWeighting;
                }

                if (form.AllKeys.Contains("DefaultValue"))
                {
                    int liDefaultValue = 0;
                    int.TryParse(form["DefaultValue"], out liDefaultValue);
                    question.DefaultValue = liDefaultValue;
                }



                if (form.AllKeys.Contains("PreviousQuestionID"))
                {
                    question.PreviousQuestionID = SITMVCFormHelper.GetGuidFromForm(form, "PreviousQuestionID");
                }


                context.SaveChanges();
                if (ID == new Guid())
                {
                    return RedirectToAction("AddUpdateQuestionAnswers", new { QuestionID = question.ID });
                }
            }
            return RedirectToAction("Questions");
        }
        #endregion
        public ActionResult ReviewLocation(Guid LocationID)
        {
            ReviewLocationModel model = new ReviewLocationModel();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.questions = context.Questions.Where(x => x.Deleted == false).ToList();
                model.headers = context.Result_Headers.Where(x => x.LocationID == LocationID && x.Submitted == true).ToList();
                model.location = context.Locations.FirstOrDefault(x => x.ID == LocationID);
            }
            return View(model);
        }
        public ActionResult ReviewQuestionForLocation(FormCollection form)
        {
            Guid QuestionID = SITMVCFormHelper.GetGuidFromForm(form, "QuestionID");
            Guid LocationID = SITMVCFormHelper.GetGuidFromForm(form, "LocationID");
            QuestionReviewLocationModel model = new QuestionReviewLocationModel();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.question = context.Questions.FirstOrDefault(x => x.Deleted == false && x.ID == QuestionID);
                model.answers = context.Result_Answers.Include("Result_Headers").Where(x => x.Result_Headers.LocationID == LocationID && x.Result_Headers.Submitted == true && x.Answer.Question.ID == QuestionID).ToList();
                model.location = context.Locations.FirstOrDefault(x => x.ID == LocationID);
            }
            return View(model);
        }

        private string GetDomain(SITSASEntities context)
        {
            SystemSetting DirectoryDomain = context.SystemSettings.Where(x => x.Name == "DirectoryEntryDomain").FirstOrDefault();

            return DirectoryDomain.Value;
        }
        private String SID1
        {
            get
            {
                return "S-1-5-21-1199947781-3238188910-400929352-4882";
                //return "SNTest";
            }
        }
        private SecurityIdentifier SID
        {
            get
            {
                WindowsIdentity identity = null;

                if (System.Web.HttpContext.Current == null)
                {
                    identity = WindowsIdentity.GetCurrent();
                }
                else
                {
                    identity = System.Web.HttpContext.Current.User.Identity as WindowsIdentity;
                }
                return identity.User;
            }
        }

        //public AccessRights DetermineAccess()
        //{
        //    string ID = string.Empty;
        //    AccessRights Rights = new AccessRights();
        //    string DomainName = string.Empty;
        //    using (SITSASEntities CORIScontext = new SITSASEntities())
        //    {
        //        DomainName = GetDomain(CORIScontext);

        //        if (!string.IsNullOrEmpty(DomainName))
        //        {

        //            if (DomainName == "Test")
        //            {
        //                ID = SID1;
        //            }
        //            else
        //            {
        //                SecurityIdentifier Identity = SID;

        //                if (!(Identity == null))
        //                {
        //                    //var sid = new SecurityIdentifier((byte[])de.Properties["objectSid"][0], 0);                          
        //                    ID = Identity.Value;
        //                    //AddToAudit("Found users in domain: SID = " + ID);
        //                }

        //            }
        //        }

        //        if (!(string.IsNullOrEmpty(ID)))
        //        {
        //            using (SITSASEntities context = new SITSASEntities())
        //            {


        //                List<Role_User_PermissionMapping> ListOfMappings = new List<Role_User_PermissionMapping>();
        //                ListOfMappings = CORIScontext.Role_User_PermissionMapping.Where(x => x.ObjectSID == ID).ToList();
        //                //AddToAudit("SID = " + ID + " permissions found for user: " + ListOfMappings.Count.ToString());
        //                if (!(ListOfMappings == null))
        //                {
        //                    List<Role_Permissions> PermissionList = new List<Role_Permissions>();
        //                    foreach (Role_User_PermissionMapping PM in ListOfMappings)
        //                    {
        //                        Role_Permissions Perm = context.Role_Permissions.Where(x => x.RoleID == PM.RoleID).FirstOrDefault();
        //                        if (Perm != null)
        //                        {
        //                            PermissionList.Add(Perm);
        //                        }
        //                    }
        //                    if (PermissionList.Count > 0)
        //                    {
        //                        foreach (Role_Permissions liPerm in PermissionList)
        //                        {
        //                            if (liPerm.CanAdd == true)
        //                            {
        //                                Rights.CanAdd = true;
        //                            }
        //                            if (liPerm.CanEdit == true)
        //                            {
        //                                Rights.CanEdit = true;
        //                            }
        //                            if (liPerm.CanView == true)
        //                            {
        //                                Rights.CanView = true;
        //                            }
        //                            if (liPerm.CanDelete == true)
        //                            {
        //                                Rights.CanDelete = true;
        //                            }
        //                        }
        //                        Rights.HasBeenSet = true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return Rights;
        //}


        #region Answers
        public ActionResult AddUpdateQuestionAnswers(Guid QuestionID, bool IsPart = false)
        {
            AnswersModel model = new AnswersModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                model.Operators = new List<Answer_Operators>();
                model.question = context.Questions.Include("CalculationModel").Include("SubCategory").Include("Answers").Include("Answers.Result_Answers").Where(x => x.ID == QuestionID).FirstOrDefault();
                if (model.question != null)
                {
                    model.ExistingAnswers = context.Answers.Include("Answer_ScoreMappings").Include("Answer_ScoreMappings.Answer_Operators").Where(x => x.QuestionID == QuestionID && x.Deleted == false).ToList();
                    if (model.question.CalculationModel.eNumMapping == (int)eCalculationModels.NumericValue || model.question.CalculationModel.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                    {
                        if (model.ExistingAnswers.ToList().Count > 1)
                        {
                            if (model.ExistingAnswers.Where(x => x.CatchAll == false).FirstOrDefault().Answer_ScoreMappings.Count > 0)
                            {
                                model.Operators.Add(model.ExistingAnswers.Where(x => x.CatchAll == false).FirstOrDefault().Answer_ScoreMappings.FirstOrDefault().Answer_Operators);
                            }
                        }

                        if (model.Operators.Count == 0)
                        {
                            model.Operators = context.Answer_Operators.ToList();
                        }
                        model.CatchAllScore = 0;
                        if (model.ExistingAnswers.Where(x => x.CatchAll == true).FirstOrDefault() != null)
                        {
                            model.CatchAllScore = model.ExistingAnswers.Where(x => x.CatchAll == true).FirstOrDefault().Score;
                        }


                        //calculate descriptions
                        model.AnswerDescriptions = new SortedList<Guid, string>();
                        int PreviousValue = 0;
                        bool FirstItem = true;
                        int i = 1;
                        if (model.ExistingAnswers.Where(x => x.CatchAll == false).ToList().Count > 0)
                        {
                            if (model.ExistingAnswers.Where(x => x.CatchAll == false).FirstOrDefault().Answer_ScoreMappings.Count > 0)
                            {
                                eOperator mainOperator = (eOperator)model.ExistingAnswers.FirstOrDefault(x => x.CatchAll == false).Answer_ScoreMappings.FirstOrDefault().Answer_Operators.eNumMapping;
                                List<Answer> answers = null;
                                switch (mainOperator)
                                {
                                    case eOperator.LessThan:
                                    case eOperator.LessThanOrEqualTo:
                                        {
                                            answers = model.ExistingAnswers.OrderBy(x => x.Score).ToList();
                                            break;
                                        }
                                    case eOperator.GreaterThan:
                                    case eOperator.GreaterThanorEqualTo:
                                        {
                                            answers = model.ExistingAnswers.OrderByDescending(x => x.Score).ToList();
                                            break;
                                        }
                                }

                                foreach (var ans in answers)
                                {
                                    if (i == model.ExistingAnswers.ToList().Count)
                                    {
                                        model.AnswerDescriptions.Add(ans.ID, ans.Answer_ScoreMappings.FirstOrDefault().Answer_Operators.Operator + " " + ans.Answer_ScoreMappings.FirstOrDefault().Value);
                                    }
                                    else
                                    {
                                        if (FirstItem)
                                        {
                                            PreviousValue = (int)ans.Answer_ScoreMappings.FirstOrDefault().Value;
                                            FirstItem = false;
                                            model.AnswerDescriptions.Add(ans.ID, ans.Answer_ScoreMappings.FirstOrDefault().Answer_Operators.Operator + " " + ans.Answer_ScoreMappings.FirstOrDefault().Value);
                                        }
                                        else
                                        {
                                            switch (mainOperator)
                                            {
                                                case eOperator.LessThan:
                                                    {
                                                        model.AnswerDescriptions.Add(ans.ID, PreviousValue + " - " + ((int)ans.Answer_ScoreMappings.FirstOrDefault().Value - 1));
                                                        PreviousValue = (int)ans.Answer_ScoreMappings.FirstOrDefault().Value;
                                                        break;
                                                    }
                                                case eOperator.GreaterThan:
                                                    {
                                                        model.AnswerDescriptions.Add(ans.ID, ((int)ans.Answer_ScoreMappings.FirstOrDefault().Value) + " - " + PreviousValue);
                                                        PreviousValue = (int)ans.Answer_ScoreMappings.FirstOrDefault().Value;
                                                        break;
                                                    }
                                                case eOperator.LessThanOrEqualTo:
                                                    {
                                                        model.AnswerDescriptions.Add(ans.ID, (PreviousValue + 1) + " - " + ((int)ans.Answer_ScoreMappings.FirstOrDefault().Value));
                                                        PreviousValue = (int)ans.Answer_ScoreMappings.FirstOrDefault().Value;
                                                        break;
                                                    }
                                                case eOperator.GreaterThanorEqualTo:
                                                    {
                                                        model.AnswerDescriptions.Add(ans.ID, (((int)ans.Answer_ScoreMappings.FirstOrDefault().Value)) + " - " + (PreviousValue - 1));
                                                        PreviousValue = (int)ans.Answer_ScoreMappings.FirstOrDefault().Value;
                                                        break;
                                                    }
                                            }

                                        }
                                    }
                                    i++;
                                }
                            }
                        }

                    }
                    else
                    {
                        model.CatchAllScore = 0;
                        if (model.ExistingAnswers.Where(x => x.CatchAll == true).FirstOrDefault() != null)
                        {
                            model.CatchAllScore = model.ExistingAnswers.Where(x => x.CatchAll == true).FirstOrDefault().Score;
                        }
                        model.Operators = context.Answer_Operators.ToList();
                    }


                }
                model.Questions = new List<Question>();
                DateTime ldNow = DateTime.Now;
                model.Questions = context.Questions.Where(x => x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow && x.CalculationModelID == model.question.CalculationModelID && x.ID != model.question.ID).ToList();
            }


            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }
        public ActionResult ImportAnswersFromAnotherQuestion(FormCollection form)
        {
            Guid QuestionID = SITMVCFormHelper.GetGuidFromForm(form, "QuestionID");
            Guid CopyFromID = SITMVCFormHelper.GetGuidFromForm(form, "ImportFromID");
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Answer> answers = context.Answers.Where(x => x.QuestionID == CopyFromID && x.Deleted == false).ToList();

                foreach (Answer answer in answers)
                {

                    Answer newAnswer = new Answer();
                    newAnswer.ID = Guid.NewGuid();
                    newAnswer.CatchAll = answer.CatchAll;
                    newAnswer.Deleted = false;
                    newAnswer.Description = answer.Description;
                    newAnswer.QuestionID = QuestionID;
                    newAnswer.Score = answer.Score;
                    context.Answers.Add(newAnswer);

                    foreach (Answer_ScoreMappings ansScoreMap in newAnswer.Answer_ScoreMappings)
                    {
                        Answer_ScoreMappings newScoreMap = new Answer_ScoreMappings();
                        newScoreMap.ID = Guid.NewGuid();
                        newScoreMap.AnswerID = newAnswer.ID;
                        newScoreMap.DateUnit = ansScoreMap.DateUnit;
                        newScoreMap.OperatorID = ansScoreMap.OperatorID;
                        newScoreMap.Value = ansScoreMap.Value;
                        context.Answer_ScoreMappings.Add(ansScoreMap);
                    }


                }
                context.SaveChanges();
            }
            //AddUpdateQuestionAnswers?QuestionID=b788c185-aae1-4126-977a-2103cfaad19d
            return RedirectToAction("AddUpdateQuestionAnswers", new { QuestionID = QuestionID });
        }
        public ActionResult AddAnswer(FormCollection form)
        {
            string strID = form["QuestionID"];
            Guid qID = new Guid();
            Guid.TryParse(strID, out qID);
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Answer_Operators> operators = context.Answer_Operators.ToList();
                Question question = context.Questions.Where(x => x.ID == qID).FirstOrDefault();
                if (question != null)
                {
                    Answer ans = new Answer();
                    ans.ID = Guid.NewGuid();
                    ans.QuestionID = qID;
                    int liScore = 0;
                    int.TryParse(form["Score"], out liScore);
                    ans.Score = liScore;
                    if (question.CalculationModel.eNumMapping == (int)eCalculationModels.DropDownLists || question.CalculationModel.eNumMapping == (int)eCalculationModels.YesNo)
                    {
                        ans.Description = form["Value"];

                    }
                    if (question.CalculationModel.eNumMapping == (int)eCalculationModels.NumericValue || question.CalculationModel.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                    {
                        Guid gOperatorID = new Guid();
                        Guid.TryParse(form["OperatorID"], out gOperatorID);
                        Answer_Operators op = context.Answer_Operators.Where(X => X.ID == gOperatorID).FirstOrDefault();
                        string Description = "Is " + op.OperatorDescription + " " + form["Value"];


                        Answer_ScoreMappings scoreMap = new Answer_ScoreMappings();
                        scoreMap.ID = Guid.NewGuid();
                        scoreMap.AnswerID = ans.ID;
                        int liValue = 0;
                        int.TryParse(form["Value"], out liValue);
                        scoreMap.Value = liValue;
                        scoreMap.OperatorID = gOperatorID;
                        context.Answer_ScoreMappings.Add(scoreMap);


                        Answer_Operators CatchAllOperator = null;
                        switch (op.Operator.Trim())
                        {
                            case "<":
                                {
                                    CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == ">=").FirstOrDefault();
                                    break;
                                }
                            case ">":
                                {
                                    CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == "<=").FirstOrDefault();
                                    break;
                                }
                            case ">=":
                                {
                                    CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == "<").FirstOrDefault();
                                    break;
                                }
                            case "<=":
                                {
                                    CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == ">").FirstOrDefault();
                                    break;
                                }
                        }
                        Answer catchans = null;
                        catchans = question.Answers.Where(x => x.CatchAll == true && x.Deleted == false).FirstOrDefault();
                        bool IsNew = false;
                        if (catchans == null)
                        {
                            IsNew = true;
                            catchans = new Answer();
                            catchans.ID = Guid.NewGuid();
                            catchans.QuestionID = qID;
                            int liCatchAllScore = 0;
                            int.TryParse(form["catchallscore"], out liCatchAllScore);
                            catchans.Score = liCatchAllScore;

                            Answer_ScoreMappings catchscoreMap = new Answer_ScoreMappings();
                            catchscoreMap.ID = Guid.NewGuid();
                            catchscoreMap.AnswerID = catchans.ID;
                            catchscoreMap.Value = liValue;
                            catchscoreMap.OperatorID = CatchAllOperator.ID;
                            context.Answer_ScoreMappings.Add(catchscoreMap);
                            catchans.Description = "Is " + CatchAllOperator.OperatorDescription + " " + liValue;
                        }
                        else
                        {
                            int liCatchAllScore = 0;
                            int.TryParse(form["catchallscore"], out liCatchAllScore);
                            catchans.Score = liCatchAllScore;
                            Answer lowestValue = question.Answers.Where(x => x.CatchAll == false && x.Deleted == false).OrderBy(x => x.Score).FirstOrDefault();
                            Answer highestValue = question.Answers.Where(x => x.CatchAll == false && x.Deleted == false).OrderByDescending(x => x.Score).FirstOrDefault();

                            long liHighestValue = 0;
                            long lilowestValue = 0;
                            if (highestValue == null)
                            {
                                liHighestValue = liValue;
                            }
                            else
                            {
                                if (liValue > highestValue.Answer_ScoreMappings.FirstOrDefault().Value)
                                {
                                    liHighestValue = liValue;
                                }
                                else
                                {
                                    liHighestValue = highestValue.Answer_ScoreMappings.FirstOrDefault().Value;
                                }
                            }

                            if (lowestValue == null)
                            {
                                lilowestValue = liValue;
                            }
                            else
                            {
                                if (liValue < lowestValue.Answer_ScoreMappings.FirstOrDefault().Value)
                                {
                                    lilowestValue = liValue;
                                }
                                else
                                {
                                    lilowestValue = lowestValue.Answer_ScoreMappings.FirstOrDefault().Value;
                                }
                            }

                            if (catchans.Answer_ScoreMappings.Count > 0)
                            {
                                catchans.Answer_ScoreMappings.FirstOrDefault().OperatorID = CatchAllOperator.ID;
                                switch (CatchAllOperator.Operator.Trim())
                                {
                                    case ">":
                                        {

                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = liHighestValue + 1;


                                            break;
                                        }
                                    case ">=":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = liHighestValue;
                                            break;
                                        }
                                    case "<":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = lilowestValue - 1;
                                            break;
                                        }
                                    case "<=":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = lilowestValue;
                                            break;
                                        }
                                }
                                catchans.Description = "Is " + CatchAllOperator.OperatorDescription + " " + catchans.Answer_ScoreMappings.FirstOrDefault().Value;
                            }
                        }



                        if (IsNew)
                        {
                            context.Answers.Add(catchans);
                        }





                        //if (!string.IsNullOrEmpty(form["MORE_value"]))
                        //{
                        //    Guid gmoreOperatorID = new Guid();
                        //    Guid.TryParse(form["MORE_OperatorID"], out gmoreOperatorID);
                        //    Answer_Operators more_op = context.Answer_Operators.Where(X => X.ID == gmoreOperatorID).FirstOrDefault();
                        //    Description = Description + " AND Is " + more_op.OperatorDescription + " " + form["MORE_Value"];


                        //    Answer_ScoreMappings MOREscoreMap = new Answer_ScoreMappings();
                        //    MOREscoreMap.ID = Guid.NewGuid();
                        //    MOREscoreMap.AnswerID = ans.ID;
                        //    int limoreValue = 0;
                        //    int.TryParse(form["MORE_Value"], out limoreValue);
                        //    MOREscoreMap.Value = limoreValue;
                        //    MOREscoreMap.OperatorID = gmoreOperatorID;
                        //    context.Answer_ScoreMappings.Add(MOREscoreMap);
                        //}



                        ans.Description = Description;
                    }
                    //if (question.CalculationModel.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                    //{
                    //    Guid gOperatorID = new Guid();
                    //    Guid.TryParse(form["OperatorID"], out gOperatorID);
                    //    Answer_Operators op = context.Answer_Operators.Where(X => X.ID == gOperatorID).FirstOrDefault();
                    //    string Description = "Questionnaire Result Is " + op.OperatorDescription + " " + form["Value"];


                    //    Answer_ScoreMappings scoreMap = new Answer_ScoreMappings();
                    //    scoreMap.ID = Guid.NewGuid();
                    //    scoreMap.AnswerID = ans.ID;
                    //    int liValue = 0;
                    //    int.TryParse(form["Value"], out liValue);
                    //    scoreMap.Value = liValue;
                    //    scoreMap.OperatorID = gOperatorID;
                    //    context.Answer_ScoreMappings.Add(scoreMap);
                    //    if (!string.IsNullOrEmpty(form["MORE_value"]))
                    //    {
                    //        Guid gmoreOperatorID = new Guid();
                    //        Guid.TryParse(form["MORE_OperatorID"], out gmoreOperatorID);
                    //        Answer_Operators more_op = context.Answer_Operators.Where(X => X.ID == gmoreOperatorID).FirstOrDefault();
                    //        Description = Description + " AND Is " + more_op.OperatorDescription + " " + form["MORE_Value"];


                    //        Answer_ScoreMappings MOREscoreMap = new Answer_ScoreMappings();
                    //        MOREscoreMap.ID = Guid.NewGuid();
                    //        MOREscoreMap.AnswerID = ans.ID;
                    //        int limoreValue = 0;
                    //        int.TryParse(form["MORE_Value"], out limoreValue);
                    //        MOREscoreMap.Value = limoreValue;
                    //        MOREscoreMap.OperatorID = gmoreOperatorID;
                    //        context.Answer_ScoreMappings.Add(MOREscoreMap);
                    //    }
                    //    ans.Description = Description;
                    //}
                    if (question.CalculationModel.eNumMapping == (int)eCalculationModels.TimeSinceARecordedDate)
                    {
                        string Description = "Is within ";


                        //DateTime lidate = DateTime.MinValue;
                        //DateTime.TryParse(form["date"], out lidate);

                        //Description = Description + "After or Equal To " + lidate.ToString("dd MMM yyyy");

                        int liWithin = 0;
                        int.TryParse(form["Within"], out liWithin);

                        string Unit = form["Unit"];

                        Description = Description + liWithin + " " + Unit + " of date provided for questionnaire";

                        //DateTime maxDate = DateTime.MinValue;
                        //switch (Unit)
                        //{
                        //    case "days":
                        //        {
                        //            maxDate = lidate.AddDays(liWithin);

                        //            break;
                        //        }
                        //    case "weeks":
                        //        {
                        //            maxDate = lidate.AddDays((liWithin * 7));

                        //            break;
                        //        }
                        //    case "months":
                        //        {
                        //            maxDate = lidate.AddMonths(liWithin);

                        //            break;
                        //        }
                        //    case "years":
                        //        {
                        //            maxDate = lidate.AddYears(liWithin);

                        //            break;
                        //        }
                        //}
                        //Description = Description + " AND IS BEFORE OR EQUAL TO " + maxDate.ToString("dd MMM yyyy");
                        ans.Description = Description;
                        //Answer_ScoreMappings scoreMap = new Answer_ScoreMappings();
                        //scoreMap.ID = Guid.NewGuid();
                        //scoreMap.AnswerID = ans.ID;
                        Answer_Operators GreaterThanEqualTo = context.Answer_Operators.Where(X => X.ID == new Guid("7CA1A1E1-793A-4171-BEEF-173561F86494")).FirstOrDefault();
                        //Answer_Operators LessThanEqualTo = context.Answer_Operators.Where(X => X.ID == new Guid("DE5C019F-A6EE-4814-B2F9-798DE363697E")).FirstOrDefault();

                        //scoreMap.OperatorID = GreaterThanEqualTo.ID;
                        //scoreMap.Value = lidate.Ticks;
                        //context.Answer_ScoreMappings.Add(scoreMap);

                        Answer_ScoreMappings scoreMaxMap = new Answer_ScoreMappings();
                        scoreMaxMap.ID = Guid.NewGuid();
                        scoreMaxMap.AnswerID = ans.ID;
                        scoreMaxMap.OperatorID = GreaterThanEqualTo.ID;
                        scoreMaxMap.Value = liWithin;
                        scoreMaxMap.DateUnit = Unit;
                        context.Answer_ScoreMappings.Add(scoreMaxMap);

                        Answer catchans = null;
                        catchans = question.Answers.Where(x => x.CatchAll == true && x.Deleted == false).FirstOrDefault();
                        bool IsNew = false;
                        if (catchans == null)
                        {
                            IsNew = true;
                            //Answer_Operators GreaterThanEqualTo = context.Answer_Operators.Where(X => X.ID == new Guid("7CA1A1E1-793A-4171-BEEF-173561F86494")).FirstOrDefault();
                            catchans = new Answer();
                            catchans.ID = Guid.NewGuid();
                            catchans.QuestionID = question.ID;
                            int liCatchAllScore = 0;
                            int.TryParse(form["catchallscore"], out liCatchAllScore);
                            catchans.Score = liCatchAllScore;
                            catchans.Deleted = false;
                            catchans.Description = "All Other Dates";
                            catchans.CatchAll = true;
                            context.Answers.Add(catchans);

                            Answer_ScoreMappings catchscoreMaxMap = new Answer_ScoreMappings();
                            catchscoreMaxMap.ID = Guid.NewGuid();
                            catchscoreMaxMap.AnswerID = catchans.ID;
                            catchscoreMaxMap.OperatorID = GreaterThanEqualTo.ID;
                            catchscoreMaxMap.Value = 100;
                            catchscoreMaxMap.DateUnit = "Years";
                            context.Answer_ScoreMappings.Add(catchscoreMaxMap);
                        }
                        else
                        {
                            int liCatchAllScore = 0;
                            int.TryParse(form["catchallscore"], out liCatchAllScore);
                            catchans.Score = liCatchAllScore;
                            if (catchans.Answer_ScoreMappings.Count == 0)
                            {
                                Answer_ScoreMappings catchscoreMaxMap = new Answer_ScoreMappings();
                                catchscoreMaxMap.ID = Guid.NewGuid();
                                catchscoreMaxMap.AnswerID = catchans.ID;
                                catchscoreMaxMap.OperatorID = GreaterThanEqualTo.ID;
                                catchscoreMaxMap.Value = 100;
                                catchscoreMaxMap.DateUnit = "Years";
                                context.Answer_ScoreMappings.Add(catchscoreMaxMap);
                            }
                        }

                    }

                    context.Answers.Add(ans);



                    context.SaveChanges();
                }

            }
            return RedirectToAction("AddUpdateQuestionAnswers", new { QuestionID = qID });
        }
        public ActionResult RemoveAnswer(Guid ID, Guid qID)
        {

            using (SITSASEntities context = new SITSASEntities())
            {
                Question question = null;
                Answer ans = context.Answers.Where(x => x.ID == ID).FirstOrDefault();
                ans.Deleted = true;
                question = ans.Question;

                if (question.CalculationModel.eNumMapping == (int)eCalculationModels.NumericValue || question.CalculationModel.eNumMapping == (int)eCalculationModels.QuestionnaireResult)
                {
                    Answer catchans = null;
                    catchans = question.Answers.Where(x => x.CatchAll == true && x.Deleted == false).FirstOrDefault();
                    Answer lowestValue = question.Answers.Where(x => x.CatchAll == false && x.Deleted == false).OrderBy(x => x.Score).FirstOrDefault();
                    Answer highestValue = question.Answers.Where(x => x.CatchAll == false && x.Deleted == false).OrderByDescending(x => x.Score).FirstOrDefault();
                    Answer_Operators CatchAllOperator = null;
                    if (lowestValue == null)
                    {
                        if (catchans != null)
                        {
                            CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == ">").FirstOrDefault();
                            if (catchans.Answer_ScoreMappings.Count > 0)
                            {
                                catchans.Answer_ScoreMappings.FirstOrDefault().Value = 0;
                                catchans.Description = "Is " + CatchAllOperator.OperatorDescription + " " + 0;
                            }
                        }
                    }
                    else
                    {
                        if (catchans != null)
                        {
                            if (highestValue.Answer_ScoreMappings.Count > 0)
                            {
                                long liHighestValue = highestValue.Answer_ScoreMappings.FirstOrDefault().Value;
                                long lilowestValue = lowestValue.Answer_ScoreMappings.FirstOrDefault().Value;


                                switch (ans.Answer_ScoreMappings.FirstOrDefault().Answer_Operators.Operator.Trim())
                                {
                                    case "<":
                                        {
                                            CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == ">=").FirstOrDefault();
                                            break;
                                        }
                                    case ">":
                                        {
                                            CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == "<=").FirstOrDefault();
                                            break;
                                        }
                                    case ">=":
                                        {
                                            CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == "<").FirstOrDefault();
                                            break;
                                        }
                                    case "<=":
                                        {
                                            CatchAllOperator = context.Answer_Operators.Where(x => x.Operator.Trim() == ">").FirstOrDefault();
                                            break;
                                        }
                                }
                                catchans.Answer_ScoreMappings.FirstOrDefault().OperatorID = CatchAllOperator.ID;
                                switch (CatchAllOperator.Operator.Trim())
                                {
                                    case ">":
                                        {

                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = liHighestValue + 1;


                                            break;
                                        }
                                    case ">=":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = liHighestValue;
                                            break;
                                        }
                                    case "<":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = lilowestValue - 1;
                                            break;
                                        }
                                    case "<=":
                                        {
                                            catchans.Answer_ScoreMappings.FirstOrDefault().Value = lilowestValue;
                                            break;
                                        }
                                }
                                catchans.Description = "Is " + CatchAllOperator.OperatorDescription + " " + catchans.Answer_ScoreMappings.FirstOrDefault().Value;
                            }
                        }
                    }

                }

                context.SaveChanges();



            }
            return RedirectToAction("AddUpdateQuestionAnswers", new { QuestionID = qID });
        }

        #endregion


        #region PrintQuestionnaires

        public ActionResult PrintQuestionnaire(Guid ID, string Type, Guid? ResultHeaderID = null)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
                DataSet ds = GenerateDataSetFromQuestionnaire(questionnaire, context, ResultHeaderID);
                string path = string.Empty;
                //Aspose.Words.License license = new Aspose.Words.License();
                //license.SetLicense("Aspose.Words.lic");
                switch (Type.ToUpper())
                {
                    case "PDF":
                        {
                            path = CreateQuestionnaire(ds, SaveFormat.Pdf, false, ID);
                            break;
                        }
                    case "DOCX":
                        {
                            path = CreateQuestionnaire(ds, SaveFormat.Docx, false, ID);
                            break;
                        }
                    case "HTML":
                        {
                            return RedirectToAction("PreviewQuestionnaire", new { ID = ID });
                        }
                }
                string rawHtml = System.IO.File.ReadAllText(path);
                byte[] filedata = System.IO.File.ReadAllBytes(path);
                string contentType = MimeMapping.GetMimeMapping(path);
                if (Type.ToUpper() == "PDF")
                {
                    return File(filedata, "application/pdf");
                }
                else
                {
                    return File(filedata, contentType);
                }

            }
        }
        public ActionResult PrintResults(Guid ResultHeaderID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Result_Headers rHeader = context.Result_Headers.FirstOrDefault(x => x.ID == ResultHeaderID);
                return File(rHeader.PDFDocument, "application/pdf");
            }
        }
        public ActionResult PrintApprovedResults(Guid ResultHeaderID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Result_Headers_Fixings rHeader = context.Result_Headers_Fixings.FirstOrDefault(x => x.ID == ResultHeaderID);
                return File(rHeader.PDFDocument, "application/pdf");
            }
        }
        public ActionResult CreateDocument(Guid ID, string Type, Guid ResultHeaderID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
                DataSet ds = GenerateDataSetFromQuestionnaire(questionnaire, context, ResultHeaderID);
                string path = string.Empty;
                //Aspose.Words.License license = new Aspose.Words.License();
                //license.SetLicense("Aspose.Words.lic");
                switch (Type.ToUpper())
                {
                    case "PDF":
                        {
                            path = CreateQuestionnaire(ds, SaveFormat.Pdf, false, ID);
                            break;
                        }
                    case "DOCX":
                        {
                            path = CreateQuestionnaire(ds, SaveFormat.Docx, false, ID);
                            break;
                        }
                    case "HTML":
                        {
                            return RedirectToAction("PreviewQuestionnaire", new { ID = ID });
                        }
                }
                string rawHtml = System.IO.File.ReadAllText(path);
                byte[] filedata = System.IO.File.ReadAllBytes(path);

                Result_Headers rHeader = context.Result_Headers.FirstOrDefault(x => x.ID == ResultHeaderID);

                rHeader.PDFDocument = filedata;
                context.SaveChanges();
                string contentType = MimeMapping.GetMimeMapping(path);
                if (Type.ToUpper() == "PDF")
                {
                    return File(filedata, "application/pdf");
                }
                else
                {
                    return File(filedata, contentType);
                }

            }
        }
        public ActionResult PreviewQuestionnaire(Guid ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                Questionnaire questionnaire = context.Questionnaires.Where(x => x.ID == ID).FirstOrDefault();
                DataSet ds = GenerateDataSetFromQuestionnaire(questionnaire, context);
                string path = string.Empty;
                path = CreateQuestionnaire(ds, SaveFormat.Html, false, ID);
                string rawHtml = System.IO.File.ReadAllText(path);
                PreviewQuestionnaireModel model = new PreviewQuestionnaireModel();
                model.Data = rawHtml;
                return PartialView(model);
            }
        }
        public DataSet GenerateDataSetFromQuestionnaire(Questionnaire questionnaire, SITSASEntities context, Guid? ResultHeaderID = null)
        {

            DataSet ds = new DataSet();

            DataTable dtFields = CreateFieldsDT(ds);
            DataTable dtCategory = CreateCategoryDT(ds);
            DataTable dtSubCategory = CreateSubCategoryDT(ds);
            DataTable dtQuestion = CreateQuestionDT(ds);
            DataTable dtAnswers = CreateAnswersDT(ds);
            //DataTable dtQuestionnaireGroup = CreateQuestionnaireGroupDT(ds);

            DataRow HeaderInfo = dtFields.NewRow();
            HeaderInfo["Name"] = questionnaire.Name;
            Result_Headers header = null;
            if (ResultHeaderID != null)
            {
                header = context.Result_Headers.Where(x => x.ID == ResultHeaderID.Value).FirstOrDefault();
                if (header != null)
                {
                    if (header.Location != null)
                    {
                        HeaderInfo["LocationName"] = header.Location.Name;
                    }

                }

            }

            HeaderInfo["qDate"] = DateTime.Now.ToString("MMMM yyyy");
            dtFields.Rows.Add(HeaderInfo);

            CreateRelationships(ds, dtCategory, dtSubCategory, dtQuestion, dtAnswers, null);

            //List<QuestionnaireGroup> QuestionnaireGroups = context.QuestionnaireGroups.Where(x => x.QuestionnaireID == questionnaire.ID).ToList();

            //foreach (QuestionnaireGroup qGroup in QuestionnaireGroups)
            //{
            List<DataMapping> dataMaps = context.DataMappings.Where(x => x.SecondaryID == questionnaire.ID.ToString() && x.DataMappingType.eNumMapping == (int)eDataMappingType.QuestionToQuestionnaire).ToList();
            if (dataMaps.Count > 0)
            {
                //DataRow nr = dtQuestionnaireGroup.NewRow();
                //nr["QuestionnaireGroupID"] = questionnaire.ID;
                //nr["Name"] = qGroup.Name;
                //dtQuestionnaireGroup.Rows.Add(nr);
                FillDataTables(dtQuestion, dtSubCategory, dtCategory, null, context, dtAnswers, dataMaps, ResultHeaderID);
            }


            //}


            return ds;

        }

        public void FillDataTables(DataTable dtQuestion, DataTable dtSubCategory, DataTable dtCategory, QuestionnaireGroup qGroup, SITSASEntities context, DataTable dtAnswers, List<DataMapping> dataMaps, Guid? ResultHeaderID = null)
        {
            Result_Headers header = null;
            if (ResultHeaderID != null)
            {
                header = context.Result_Headers.Where(x => x.ID == ResultHeaderID.Value).FirstOrDefault();
            }
            List<Guid> dashCatIDs = new List<Guid>();
            List<Guid> assRiskIDs = new List<Guid>();
            foreach (DataMapping dataMap in dataMaps)
            {
                DateTime ldNow = DateTime.Now;
                Question q = context.Questions.Where(x => x.ID == new Guid(dataMap.PrimaryID) && x.Deleted == false && x.StartDate < ldNow && x.EndDate > ldNow && x.SubCategory.Category.StartDate < ldNow && x.SubCategory.Category.EndDate > ldNow).FirstOrDefault();
                if (q != null)
                {
                    string find = "QuestionID = '" + q.ID + "'";
                    DataRow qRow = null;
                   DataRow[] foundRows = dtQuestion.Select(find);
                    if (foundRows.Count() == 0)
                    {
                        qRow = dtQuestion.NewRow();
                        qRow["QuestionID"] = q.ID;
                        qRow["Name"] = q.Name;
                        if (header != null)
                        {
                            Result_Answers answer = header.Result_Answers.FirstOrDefault(x => x.Answer.QuestionID == q.ID);
                            if (answer != null)
                            {
                                if (!string.IsNullOrEmpty(answer.RawAnswer))
                                {
                                    qRow["RawAnswer"] = answer.RawAnswer;
                                }
                                if (!string.IsNullOrEmpty(answer.Comments))
                                {
                                    qRow["Comments"] = answer.Comments;
                                }

                            }

                        }
                    }
                    else
                    {
                        qRow = foundRows[0];
                    }

                    if (!dashCatIDs.Contains(q.SubCategory.ID))
                    {
                        DataRow dashboardCat = dtSubCategory.NewRow();
                        dashboardCat["SubCategoryID"] = q.SubCategory.ID;
                        dashboardCat["Name"] = q.SubCategory.DisplayName;
                        dashboardCat["CategoryID"] = q.SubCategory.CategoryID;
                        if (q.SubCategory.CategoryID.HasValue)
                        {
                            if (!assRiskIDs.Contains(q.SubCategory.CategoryID.Value))
                            {
                                DataRow assCat = dtCategory.NewRow();
                                assCat["CategoryID"] = q.SubCategory.Category.ID;
                                assCat["Name"] = q.SubCategory.Category.DisplayName;
                                //assCat["QuestionnaireGroupID"] = qGroup.ID;
                                assRiskIDs.Add(q.SubCategory.Category.ID);
                                dtCategory.Rows.Add(assCat);
                            }
                        }
                        dtSubCategory.Rows.Add(dashboardCat);
                        dashCatIDs.Add(q.SubCategory.ID);
                    }

                    qRow["SubCategoryID"] = q.SubCategoryID;
                    if (foundRows.Count() == 0)
                    {
                        dtQuestion.Rows.Add(qRow);
                    }
                        List<SystemSetting> settings = context.SystemSettings.ToList();
                    switch (q.CalculationModel.eNumMapping)
                    {
                        case (int)eCalculationModels.DropDownLists:
                            {
                                foreach (Answer ans in q.Answers.Where(x => x.Deleted == false))
                                {
                                    DataRow nar = dtAnswers.NewRow();
                                    nar["QuestionID"] = q.ID;
                                    //nar["ImagePath"] = settings.FirstOrDefault(x => x.Name == "Image_DropDownList").Value;
                                    nar["Description"] = ans.Description;
                                    dtAnswers.Rows.Add(nar);
                                }
                                break;
                            }
                        case (int)eCalculationModels.NumericValue:
                            {
                                DataRow nar = dtAnswers.NewRow();
                                nar["QuestionID"] = q.ID;
                                //nar["ImagePath"] = settings.FirstOrDefault(x => x.Name == "Image_Numeric").Value;
                                dtAnswers.Rows.Add(nar);
                                break;
                            }

                        case (int)eCalculationModels.QuestionnaireResult:
                            {
                                if (q.SubQuestionnaireID != null)
                                {
                                    DataSet ds = GenerateDataSetFromQuestionnaire(q.Questionnaire, context);
                                    string path = CreateQuestionnaire(ds, SaveFormat.Docx, true, q.SubQuestionnaireID.Value);
                                    if (path != string.Empty)
                                    {
                                        DataRow nar = dtAnswers.NewRow();
                                        nar["QuestionID"] = q.ID;
                                        nar["QuestionaiirePath"] = path;
                                        dtAnswers.Rows.Add(nar);
                                    }
                                }
                                //Create Questionnaire.
                                break;
                            }

                        case (int)eCalculationModels.TimeSinceARecordedDate:
                            {
                                DataRow nar = dtAnswers.NewRow();
                                nar["QuestionID"] = q.ID;
                                // nar["ImagePath"] = settings.FirstOrDefault(x => x.Name == "Image_DateTime").Value;
                                dtAnswers.Rows.Add(nar);
                                break;
                            }

                        case (int)eCalculationModels.ManualEntry:
                            {
                                DataRow nar = dtAnswers.NewRow();
                                nar["QuestionID"] = q.ID;
                                //nar["ImagePath"] = settings.FirstOrDefault(x => x.Name == "Image_Manual").Value;
                                dtAnswers.Rows.Add(nar);
                                break;
                            }
                        case (int)eCalculationModels.YesNo:
                            {
                                foreach (Answer ans in q.Answers.Where(x => x.Deleted == false))
                                {
                                    DataRow nar = dtAnswers.NewRow();
                                    nar["QuestionID"] = q.ID;
                                    //nar["ImagePath"] = settings.FirstOrDefault(x => x.Name == "Image_DropDownList").Value;
                                    nar["Description"] = ans.Description;
                                    dtAnswers.Rows.Add(nar);
                                }
                                break;
                            }
                    }
                }
            }
        }

        private static void CreateRelationships(DataSet ds, DataTable dtCategory, DataTable dtSubCategory, DataTable dtQuestion, DataTable dtAnswers, DataTable dtQuestionnaireGroup)
        {
            DataRelation Question_Answer = new DataRelation("Question_Answer", dtQuestion.Columns["QuestionID"], dtAnswers.Columns["QuestionID"]);
            ds.Relations.Add(Question_Answer);

            DataRelation Question_SubCategory = new DataRelation("Question_SubCategory", dtSubCategory.Columns["SubCategoryID"], dtQuestion.Columns["SubCategoryID"]);
            ds.Relations.Add(Question_SubCategory);

            DataRelation SubCategory_AssessedRisk = new DataRelation("SubCategory_AssessedRisk", dtCategory.Columns["CategoryID"], dtSubCategory.Columns["CategoryID"]);
            ds.Relations.Add(SubCategory_AssessedRisk);

            //DataRelation QuestionnaireGroup_AssessedRisk = new DataRelation("QuestionnaireGroup_AssessedRisk", dtQuestionnaireGroup.Columns["QuestionnaireGroupID"], dtAssessedRisk.Columns["QuestionnaireGroupID"]);
            //ds.Relations.Add(QuestionnaireGroup_AssessedRisk);
        }
        private static DataTable CreateAnswersDT(DataSet ds)
        {
            DataTable dtAnswers = new DataTable();
            dtAnswers.Columns.Add("QuestionID");
            dtAnswers.Columns.Add("ImagePath");
            dtAnswers.Columns.Add("Description");
            dtAnswers.Columns.Add("QuestionaiirePath");
            dtAnswers.TableName = "Answers";
            ds.Tables.Add(dtAnswers);
            return dtAnswers;
        }

        private static DataTable CreateFieldsDT(DataSet ds)
        {
            DataTable dtFields = new DataTable();
            dtFields.Columns.Add("Name");
            dtFields.Columns.Add("LocationName");
            dtFields.Columns.Add("qDate");
            dtFields.TableName = "Fields";
            ds.Tables.Add(dtFields);
            return dtFields;
        }

        //private static DataTable CreateQuestionnaireGroupDT(DataSet ds)
        //{
        //    DataTable dtFields = new DataTable();
        //    dtFields.Columns.Add("QuestionnaireGroupID");
        //    dtFields.Columns.Add("Name");
        //    dtFields.TableName = "QuestionnaireGroup";
        //    ds.Tables.Add(dtFields);
        //    return dtFields;
        //}


        private static DataTable CreateQuestionDT(DataSet ds)
        {
            DataTable dtQuestion = new DataTable();
            dtQuestion.Columns.Add("QuestionID");
            dtQuestion.Columns.Add("SubCategoryID");
            dtQuestion.Columns.Add("Name");
            dtQuestion.Columns.Add("RawAnswer");
            dtQuestion.Columns.Add("Comments");
            dtQuestion.TableName = "Questions";
            ds.Tables.Add(dtQuestion);
            return dtQuestion;
        }

        private static DataTable CreateSubCategoryDT(DataSet ds)
        {
            DataTable dtSubCategory = new DataTable();
            dtSubCategory.Columns.Add("SubCategoryID");
            dtSubCategory.Columns.Add("CategoryID");
            dtSubCategory.Columns.Add("Name");
            dtSubCategory.TableName = "SubCategory";
            ds.Tables.Add(dtSubCategory);
            return dtSubCategory;
        }

        private static DataTable CreateCategoryDT(DataSet ds)
        {
            DataTable dtAssessedRisk = new DataTable();
            dtAssessedRisk.Columns.Add("CategoryID");
            dtAssessedRisk.Columns.Add("Name");
            dtAssessedRisk.TableName = "Category";
            ds.Tables.Add(dtAssessedRisk);
            return dtAssessedRisk;
        }

        private string CreateQuestionnaire(DataSet ds, SaveFormat sv, bool SubQuestionnaire, Guid QuestionnaireID)
        {
            Aspose.Words.License license = new Aspose.Words.License();
            license.SetLicense("Aspose.Words.lic");
            string path = string.Empty;
            Document doc = null;
            using (SITSASEntities context = new SITSASEntities())
            {
                List<SystemSetting> settings = context.SystemSettings.ToList();
                if (SubQuestionnaire)
                {
                    path = settings.FirstOrDefault(x => x.Name == "Directory_Questionnaire").Value;
                }
                else
                {
                    path = settings.FirstOrDefault(x => x.Name == "Directory_Questionnaire").Value;
                }
                switch (sv)
                {
                    case SaveFormat.Docx:
                        {
                            path = path + @"\" + QuestionnaireID + DateTime.Now.ToString("yyyyMMddhhmmss") + ".docx";
                            break;
                        }
                    case SaveFormat.Pdf:
                        {
                            path = path + @"\" + QuestionnaireID + DateTime.Now.ToString("yyyyMMddhhmmss") + ".docx";
                            break;
                        }
                    case SaveFormat.Html:
                        {
                            path = path + @"\" + QuestionnaireID + DateTime.Now.ToString("yyyyMMddhhmmss") + ".docx";
                            break;
                        }
                }


                if (SubQuestionnaire)
                {
                    doc = new Document(settings.FirstOrDefault(x => x.Name == "Template_Questionnaire").Value);
                }
                else
                {
                    doc = new Document(settings.FirstOrDefault(x => x.Name == "Template_Questionnaire").Value);
                }
            }
            doc.MailMerge.TrimWhitespaces = false;
            doc.MailMerge.FieldMergingCallback = new myHandler();
            doc.MailMerge.ExecuteWithRegions(ds);
            doc.Save(path, sv);
            return path;
        }

        private class myHandler : IFieldMergingCallback
        {
            public void FieldMerging(FieldMergingArgs e)
            {
                if (e.FieldName == "QuestionaiirePath")
                {
                    if (!string.IsNullOrEmpty(e.FieldValue.ToString()))
                    {
                        DocumentBuilder builder = new DocumentBuilder(e.Document);
                        builder.MoveToMergeField(e.FieldName);

                        // The name of the document to load and insert is stored in the field value.
                        Document subDoc = new Document((String)e.FieldValue);

                        // Insert the document.
                        builder.InsertDocument(subDoc, ImportFormatMode.KeepSourceFormatting);

                        // Indicate to the mail merge engine that we have inserted what we wanted.
                        e.Text = null;
                    }
                }
                if (e.FieldName == "ImagePath")
                {
                    if (!string.IsNullOrEmpty(e.FieldValue.ToString()))
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(e.FieldValue.ToString());
                        DocumentBuilder builder = new DocumentBuilder(e.Document);
                        builder.MoveToMergeField(e.FieldName);
                        builder.InsertImage(image);
                        e.Text = null;
                    }
                }
            }

            public void ImageFieldMerging(ImageFieldMergingArgs args)
            {
                //throw new NotImplementedException();
            }
        }

        #endregion

        #region IncidentTypes
        public ActionResult IncidentTypes()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();
                IncidentTypesModel model = new IncidentTypesModel();
                model.rights = ContextModel.DetermineAccess();
                List<IncidentType> itColl = context.IncidentTypes.Include("Questionnaire").Where(x => x.Deleted == false).ToList();
                List<Questionnaire> userVisibleQuestionnaires = context.GetQuestionnairesForUser(ContextModel.GetCurrentUserSID(), false).ToList();
                model.ExistingIncidentTypes = new List<IncidentType>();
                foreach (IncidentType it in itColl)
                {
                    if (userVisibleQuestionnaires.Contains(it.Questionnaire))
                    {

                        model.ExistingIncidentTypes.Add(it);
                    }
                }
                return View(model);
            }
        }

        public ActionResult CreateUpdateIncidentType(Guid? ID, bool IsPart = false)
        {
            CreateUpdateIncidentTypeModel model = new CreateUpdateIncidentTypeModel();
            model.IncidentTypeExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    IncidentType rCategory = context.IncidentTypes.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingIncidentType = rCategory;
                        model.IncidentTypeExists = true;
                    }

                }
                model.AllQuestionnaires = context.Questionnaires.Where(x => x.Deleted == false).ToList();
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteIncidentType(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    IncidentType rCategory = context.IncidentTypes.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("IncidentTypes");
        }
        public ActionResult SaveIncidentType(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingIncidentTypeID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                IncidentType IncidentType = null;
                if (ID != new Guid())
                {
                    IncidentType = context.IncidentTypes.Where(x => x.ID == ID).FirstOrDefault();

                }
                else
                {
                    IncidentType = new IncidentType();
                    IncidentType.ID = Guid.NewGuid();
                }
                IncidentType.Name = form["Name"];
                Guid qID = new Guid();
                Guid.TryParse(form["QuestionnaireID"], out qID);
                IncidentType.QuestionnaireID = qID;
                if (ID == new Guid())
                {
                    context.IncidentTypes.Add(IncidentType);
                }

                context.SaveChanges();
            }
            return RedirectToAction("IncidentTypes");
        }





        #endregion

        #region StalenessProfiles
        public ActionResult StalenessProfiles()
        {
            StalenessProfilesModel model = new StalenessProfilesModel();
            model.rights = ContextModel.DetermineAccess();
            using (SITSASEntities context = new SITSASEntities())
            {
                //AccessRights Rights = ContextModel.DetermineAccess();

                model.ExistingStalenessProfiles = context.StalenessProfiles.Where(x => x.Deleted == false).ToList();

            }

            return View(model);
        }

        public ActionResult CreateUpdateStalenessProfile(Guid? ID, bool IsPart = false)
        {
            CreateUpdateStalenessProfileModel model = new CreateUpdateStalenessProfileModel();
            model.rights = ContextModel.DetermineAccess();
            model.StalenessProfileExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    StalenessProfile rCategory = context.StalenessProfiles.Where(x => x.ProfileID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        model.ExistingStalenessProfile = rCategory;
                        model.StalenessProfileExists = true;
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }
        }
        public ActionResult DeleteStalenessProfile(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    StalenessProfile rCategory = context.StalenessProfiles.Where(x => x.ProfileID == ID.Value).FirstOrDefault();
                    if (rCategory != null)
                    {
                        rCategory.Deleted = true;
                        context.SaveChanges();
                    }

                }
            }
            return Redirect("StalenessProfiles");
        }
        public ActionResult SaveStalenessProfile(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingStalenessProfileID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                StalenessProfile StalenessProfile = null;
                if (ID != new Guid())
                {
                    StalenessProfile = context.StalenessProfiles.Where(x => x.ProfileID == ID).FirstOrDefault();

                }
                else
                {
                    StalenessProfile = new StalenessProfile();
                    StalenessProfile.ProfileID = Guid.NewGuid();

                }
                StalenessProfile.Name = form["Name"];

                int liFinalScore = 0;
                int.TryParse(form["FinalScore"], out liFinalScore);
                StalenessProfile.FinalScore = liFinalScore;
                int liDaysUntilFinalScore = 0;
                int.TryParse(form["DaysUntilFinalScore"], out liDaysUntilFinalScore);
                StalenessProfile.DaysUntilFinalScore = liDaysUntilFinalScore;
                int liStaticDays = 0;
                int.TryParse(form["StaticDays"], out liStaticDays);
                StalenessProfile.StaticDays = liStaticDays;

                if (ID == new Guid())
                {
                    context.StalenessProfiles.Add(StalenessProfile);
                }

                context.SaveChanges();
            }
            return RedirectToAction("StalenessProfiles");
        }
        #endregion



        public ActionResult FrequencyProfiles()
        {
            using (SITSASEntities context = new SITSASEntities())
            {

                FrequencyProfileModel model = new FrequencyProfileModel();
                model.rights = ContextModel.DetermineAccess();
                List<FrequencyProfile> tempFrequencyProfileColl = context.FrequencyProfiles.Where(x => x.Deleted == false).ToList();

                model.ExistingFrequencyProfiles = new List<FrequencyProfile>();
                foreach (FrequencyProfile count in tempFrequencyProfileColl)
                {

                    model.ExistingFrequencyProfiles.Add(count);

                }
                return View(model);
            }
        }

        public ActionResult CreateUpdateFrequencyProfile(Guid? ID, bool IsPart = false)
        {
            CreateUpdateFrequencyProfileModel model = new CreateUpdateFrequencyProfileModel();
            model.rights = ContextModel.DetermineAccess();
            model.FrequencyProfileExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    FrequencyProfile FrequencyProfile = context.FrequencyProfiles.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (FrequencyProfile != null)
                    {
                        model.ExistingFrequencyProfile = FrequencyProfile;
                        model.FrequencyProfileExists = true;
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }

        public ActionResult DeleteFrequencyProfile(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    FrequencyProfile FrequencyProfile = context.FrequencyProfiles.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (FrequencyProfile != null)
                    {
                        FrequencyProfile.Deleted = true;
                        context.SaveChanges();
                    }
                }
            }
            return RedirectToAction("FrequencyProfiles");
        }
        public ActionResult SaveFrequencyProfile(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingFrequencyProfileID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                FrequencyProfile cty = null;
                if (ID != new Guid())
                {
                    cty = context.FrequencyProfiles.Where(x => x.ID == ID).FirstOrDefault();
                    //AddToAudit("FrequencyProfile Updated " + CTY.full_name);
                }
                else
                {
                    cty = new FrequencyProfile();
                    cty.ID = Guid.NewGuid();
                }
                cty.Name = form["Name"];
                cty.eDateUnit = SITMVCFormHelper.GetIntegerFromForm(form, "Unit");
                cty.StartDate = SITMVCFormHelper.GetDateTimeFromForm(form, "StartDate");
                int Frequency = 0;
                int.TryParse(form["Frequency"], out Frequency);
                cty.Frequency = Frequency;

                if (ID == new Guid())
                {
                    if (cty.StartDate < DateTime.Now)
                    {
                        switch ((eDateUnit)cty.eDateUnit)
                        {
                            case eDateUnit.Days:
                                {
                                    cty.NextDateToComplete = cty.StartDate.AddDays(Frequency);
                                    break;
                                }
                            case eDateUnit.Weeks:
                                {
                                    cty.NextDateToComplete = cty.StartDate.AddDays((Frequency * 7));
                                    break;
                                }
                            case eDateUnit.Months:
                                {
                                    cty.NextDateToComplete = cty.StartDate.AddMonths(Frequency);
                                    break;
                                }
                            case eDateUnit.Years:
                                {
                                    cty.NextDateToComplete = cty.StartDate.AddYears(Frequency);
                                    break;


                                }
                        }
                    }
                    else
                    {
                        cty.NextDateToComplete = cty.StartDate;
                    }

                    context.FrequencyProfiles.Add(cty);

                }

                context.SaveChanges();
            }
            return RedirectToAction("FrequencyProfiles");
        }
        public ActionResult Tasks()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                TaskModel model = new TaskModel();
                model.rights = ContextModel.DetermineAccess();
                //List<Task> tempLocations = context.GetTasksForUser(ContextModel.GetCurrentUserSID(), false).ToList();

                List<Task> tempTaskColl = context.Tasks.Where(x => x.Deleted == false).ToList();
                model.ExistingTasks = new SortedList<Guid, string>();
                foreach (Task count in tempTaskColl)
                {
                    model.ExistingTasks.Add(count.ID, count.Title);
                }
                return View(model);
            }
        }

        public ActionResult CreateUpdateTask(Guid? ID, bool IsPart = false, bool IsFromQuestionnaire = false, Guid? HeaderID = null)
        {
            CreateUpdateTaskModel model = new CreateUpdateTaskModel();
            model.rights = ContextModel.DetermineAccess();
            model.TaskExists = false;
            model.IsPart = IsPart;
            model.IsFromQuestionnaire = IsFromQuestionnaire;
            if (HeaderID.HasValue)
            {
                model.HeaderID = HeaderID.Value;
            }

            using (SITSASEntities context = new SITSASEntities())
            {
                if (HttpContext.Session["DirectoryUsers"] != null)
                {
                    model.AllUsers = (List<DirectoryUser>)HttpContext.Session["DirectoryUsers"];
                }
                else
                {
                    model.AllUsers = ContextModel.GetUsersFromActiveDirectory(context);
                    HttpContext.Session["DirectoryUsers"] = model.AllUsers;
                }
                model.AllTaskStatuses = context.TaskStatus.ToList();
                model.AssignedUsers = new List<string>();
                if (ID.HasValue)
                {
                    Task Task = context.Tasks.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (Task != null)
                    {
                        List<DataMapping> dMaps = context.DataMappings.Where(x => x.PrimaryID == ID.Value.ToString() && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();

                        foreach (DataMapping dMap in dMaps)
                        {
                            model.AssignedUsers.Add(dMap.SecondaryID);
                        }

                        model.ExistingTask = Task;
                        model.TaskExists = true;

                        if (Task.HeaderID != null)
                        {
                            Result_Headers header = context.Result_Headers.FirstOrDefault(x => x.ID == Task.HeaderID);
                            if (header != null)
                            {
                                model.CreatedFrom = header.Location.Name + " | " + header.Questionnaire.Name + " | " + header.SelectedDate.ToString("dd/MM/yy");
                            }
                            else
                            {
                                Result_Headers_Fixings fixheader = context.Result_Headers_Fixings.FirstOrDefault(x => x.ID == Task.HeaderID);
                                if (fixheader != null)
                                {
                                    model.CreatedFrom = fixheader.Location.Name + " | " + fixheader.Questionnaire.Name + " | " + fixheader.SelectedDate.ToString("dd/MM/yy");
                                }
                                else
                                {
                                    model.CreatedFrom = "No Assessment";
                                }
                            }
                        }
                        else
                        {
                            model.CreatedFrom = "No Assessment";
                        }
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }

        public ActionResult DeleteTask(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Task Task = context.Tasks.Where(x => x.ID == ID.Value).FirstOrDefault();
                    if (Task != null)
                    {
                        Task.Deleted = true;
                        context.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Tasks");
        }
        public ActionResult GetTasks(string Status)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                GetTasksModel model = new GetTasksModel();
                string myID = ContextModel.GetCurrentUserSID();
                List<DataMapping> dMaps = context.DataMappings.Where(x => x.SecondaryID == myID && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();

                List<Task> tasks = new List<Task>();
                foreach (DataMapping dMap in dMaps)
                {
                    Guid taskID = new Guid(dMap.PrimaryID);
                    Task t = context.Tasks.FirstOrDefault(x => x.ID == taskID && x.Deleted == false);
                    if (t != null)
                    {
                        tasks.Add(t);
                    }

                }
                DateTime now = DateTime.Now;
                switch (Status)
                {
                    case "Overdue":
                        {


                            model.Tasks = tasks.Where(x => x.DueDate < now && x.CompletedDate == null).ToList();
                            break;
                        }
                    case "DueSoon":
                        {
                            model.Tasks = tasks.Where(x => x.DueDate > now && x.CompletedDate == null).ToList();
                            break;
                        }
                    case "Completed":
                        {
                            model.Tasks = tasks.Where(x => x.CompletedDate < now).ToList();
                            break;
                        }
                    case "Created":
                        {
                            model.Tasks = tasks.Where(x => x.CreatedBy == ContextModel.GetCurrentUserSID()).ToList();
                            break;
                        }
                }
                model.Headers = new List<Result_Headers>();
                model.FixedHeaders = new List<Result_Headers_Fixings>();
                foreach (Task task in model.Tasks)
                {
                    if (task.HeaderID != null)
                    {
                        Result_Headers header = context.Result_Headers.Include("Questionnaire").Include("Location").FirstOrDefault(x => x.ID == task.HeaderID);
                        if (header != null)
                        {
                            model.Headers.Add(header);
                        }
                        else
                        {
                            Result_Headers_Fixings fixheader = context.Result_Headers_Fixings.Include("Questionnaire").Include("Location").FirstOrDefault(x => x.ID == task.HeaderID);
                            if (fixheader != null)
                            {
                                model.FixedHeaders.Add(fixheader);
                            }
                        }
                    }
                }
                return PartialView(model);
            }
        }

        public ActionResult TaskStatuses()
        {
            List<MyTasksModel> statuses = new List<MyTasksModel>();
            using (SITSASEntities context = new SITSASEntities())
            {
                string MyID = ContextModel.GetCurrentUserSID();
                List<DataMapping> dMaps = context.DataMappings.Where(x => x.SecondaryID == MyID && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();

                List<Task> tasks = new List<Task>();
                List<Task> recentlyCompletedTasks = new List<Task>();
                foreach (DataMapping dMap in dMaps)
                {
                    Guid taskID = new Guid(dMap.PrimaryID);
                    Task t = context.Tasks.FirstOrDefault(x => x.ID == taskID && x.Deleted == false);
                    if (t != null)
                    {
                        if (t.CompletedDate == null)
                        {
                            tasks.Add(t);
                        }
                        else
                        {
                            DateTime TwoDaysAgo = DateTime.Now.Date.AddDays(-2);
                            DateTime now = DateTime.Now;
                            if (t.CompletedDate < now && t.CompletedDate > TwoDaysAgo)
                            {
                                recentlyCompletedTasks.Add(t);
                            }
                        }

                    }

                }

                MyTasksModel green = new MyTasksModel();
                MyTasksModel orange = new MyTasksModel();
                MyTasksModel red = new MyTasksModel();
                DateTime today = DateTime.Now;

                green.Colour = "#19841a";

                orange.Colour = "#f87626";
                red.Colour = "#e00039";
                red.NumberOfTasks = tasks.Where(x => x.DueDate < today).ToList().Count;
                orange.NumberOfTasks = tasks.Where(x => x.DueDate > today).ToList().Count;

                green.NumberOfTasks = recentlyCompletedTasks.Count;


                statuses.Add(green);
                statuses.Add(orange);
                statuses.Add(red);
            }

            return PartialView(statuses);
        }

        public ActionResult MyTasks()
        {
            //green: 19841a
            //orange: f87626
            //red: e00039
            MyTasksModel model = new MyTasksModel();
            using (SITSASEntities context = new SITSASEntities())
            {

                string MyID = ContextModel.GetCurrentUserSID();
                List<DataMapping> dMaps = context.DataMappings.Where(x => x.SecondaryID == MyID && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();

                List<Task> tasks = new List<Task>();
                foreach (DataMapping dMap in dMaps)
                {
                    Guid taskID = new Guid(dMap.PrimaryID);
                    Task t = context.Tasks.FirstOrDefault(x => x.ID == taskID && x.Deleted == false);
                    if (t != null)
                    {
                        if (t.CompletedDate == null)
                        {
                            tasks.Add(t);
                        }
                    }

                }
                model.NumberOfTasks = tasks.Count;
                DateTime today = DateTime.Now;
                if (tasks.Where(x => x.DueDate < today).ToList().Count > 0)
                {
                    model.Colour = "#e00039";
                }
                else
                {
                    if (tasks.Where(x => x.DueDate > today).ToList().Count > 0)
                    {
                        model.Colour = "#f87626";
                    }
                    else
                    {
                        model.Colour = "#19841a";
                    }
                }
            }



            return PartialView("partMyTasks", model);

        }
        public ActionResult SaveTask(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingTaskID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Task cty = null;
                if (ID != new Guid())
                {
                    cty = context.Tasks.Where(x => x.ID == ID).FirstOrDefault();
                    //AddToAudit("Task Updated " + CTY.full_name);
                }
                else
                {
                    cty = new Task();
                    cty.ID = Guid.NewGuid();
                    cty.CreatedDate = DateTime.Now;
                }
                cty.Title = form["Title"];
                cty.Description = form["Description"];
                cty.Comments = form["Comments"];
                cty.DueDate = SITMVCFormHelper.GetDateTimeFromForm(form, "DueDate");
                //cty.AssignedTo = form["AssignedTo"];


                List<string> lgEmployeeIDs = SITMVCFormHelper.GetStringCollectionFromForm(form, "AssignedTo");

                List<DataMapping> dMaps = context.DataMappings.Where(x => x.PrimaryID == cty.ID.ToString() && x.DataMappingType.eNumMapping == (int)eDataMappingType.TaskToUser).ToList();
                context.DataMappings.RemoveRange(dMaps);

                foreach (string lgEmployeeID in lgEmployeeIDs)
                {
                    DataMapping userToTask = new DataMapping();
                    userToTask.ID = Guid.NewGuid();
                    userToTask.SecondaryID = lgEmployeeID;
                    userToTask.PrimaryID = cty.ID.ToString();
                    userToTask.DataMappingTypeID = context.DataMappingTypes.Where(x => x.eNumMapping == (int)eDataMappingType.TaskToUser).FirstOrDefault().ID;
                    context.DataMappings.Add(userToTask);
                }

                cty.CreatedBy = ContextModel.GetCurrentUserSID();
                cty.Status = SITMVCFormHelper.GetGuidFromForm(form, "Status");
                if (form.AllKeys.Contains("HeaderID"))
                {
                    cty.HeaderID = SITMVCFormHelper.GetGuidFromForm(form, "HeaderID");
                }
                if (cty.Status == new Guid("15C81B29-E3D6-4F41-994E-C6E11AB1BE20"))
                {
                    cty.CompletedDate = DateTime.Now;
                }
                cty.Deleted = false;

                if (ID == new Guid())
                {
                    context.Tasks.Add(cty);

                }

                context.SaveChanges();
            }
            return RedirectToAction("Tasks");
        }

        public ActionResult CreateUpdateRole(Guid? ID, bool IsPart = false)
        {
            CreateUpdateRoleModel model = new CreateUpdateRoleModel();
            model.rights = ContextModel.DetermineAccess();
            model.RoleExists = false;

            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Role Role = context.Roles.Include("Role_Permissions").Where(x => x.RoleID == ID.Value).FirstOrDefault();
                    if (Role != null)
                    {
                        model.ExistingRole = Role;
                        model.RoleExists = true;
                    }

                }
            }
            if (IsPart)
            {
                return PartialView(model);
            }
            else
            {
                return View(model);
            }

        }

        public ActionResult DeleteRole(Guid? ID)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                if (ID.HasValue)
                {
                    Role Role = context.Roles.Where(x => x.RoleID == ID.Value).FirstOrDefault();
                    if (Role != null)
                    {
                        Role.Deleted = true;
                        context.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Index", "Roles");
        }
        public ActionResult Permissions()
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Page> model = context.Pages.ToList();
                return View(model);
            }

        }
        public ActionResult GetRolePermissions(Guid PageID)
        {
            RolePermissionsModel model = new RolePermissionsModel();
            model.PageID = PageID;
            model.rights = ContextModel.DetermineAccess();
            model.Roles = new List<RolePermission>();
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Role> roles = context.Roles.ToList();

                foreach (Role role in roles)
                {
                    RolePermission rPerm = new RolePermission();
                    rPerm.Role = role;
                    rPerm.Mapping = context.Page_Role_Mappings.Where(x => x.RoleID == role.RoleID.ToString() && x.PageID == PageID).FirstOrDefault();
                    if (rPerm.Mapping == null)
                    {
                        rPerm.Mapping = new Page_Role_Mappings();
                        rPerm.Mapping.RoleID = rPerm.Role.RoleID.ToString();
                        rPerm.Mapping.PageID = PageID;
                        rPerm.Mapping.CanCreate = false;
                        rPerm.Mapping.CanView = false;
                        rPerm.Mapping.CanUpdate = false;
                        rPerm.Mapping.CanDelete = false;
                    }
                    model.Roles.Add(rPerm);
                }


            }

            //get role permissions
            //get role
            return PartialView("partRolePermissions", model);


        }

        public ActionResult SaveRolePermission(FormCollection form)
        {
            Guid PageID = SITMVCFormHelper.GetGuidFromForm(form, "PageID");
            using (SITSASEntities context = new SITSASEntities())
            {
                List<Role> roles = context.Roles.ToList();

                foreach (Role role in roles)
                {
                    bool IsNew = false;
                    Page_Role_Mappings existingmap = context.Page_Role_Mappings.Where(x => x.RoleID == role.RoleID.ToString() && x.PageID == PageID).FirstOrDefault();
                    if (existingmap == null)
                    {
                        IsNew = true;
                        existingmap = new Page_Role_Mappings();
                        existingmap.ID = Guid.NewGuid();
                        existingmap.PageID = PageID;
                        existingmap.RoleID = role.RoleID.ToString();
                    }
                    if (SITMVCFormHelper.GetBooleanFromForm(form, role.RoleID + "-CanCreate"))
                    {
                        existingmap.CanCreate = true;
                    }
                    else
                    {
                        existingmap.CanCreate = false;
                    }
                    if (SITMVCFormHelper.GetBooleanFromForm(form, role.RoleID + "-CanView"))
                    {
                        existingmap.CanView = true;
                    }
                    else
                    {
                        existingmap.CanView = false;
                    }
                    if (SITMVCFormHelper.GetBooleanFromForm(form, role.RoleID + "-CanEdit"))
                    {
                        existingmap.CanUpdate = true;
                    }
                    else
                    {
                        existingmap.CanUpdate = false;
                    }
                    if (SITMVCFormHelper.GetBooleanFromForm(form, role.RoleID + "-CanDelete"))
                    {
                        existingmap.CanDelete = true;
                    }
                    else
                    {
                        existingmap.CanDelete = false;
                    }
                    if (IsNew)
                    {
                        context.Page_Role_Mappings.Add(existingmap);
                    }


                    context.SaveChanges();

                }
            }
            return RedirectToAction("Permissions");
        }

        public ActionResult partSideMenu()
        {
            List<Page> model = new List<Page>();
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
            using (SITSASEntities context = new SITSASEntities())
            {

                List<Page> pages = context.Pages.Where(x => x.Link != "").ToList();
                List<Role> AllRoles = context.Roles.ToList();
                List<Role_User_PermissionMapping> ListOfMappings = new List<Role_User_PermissionMapping>();
                ListOfMappings = context.Role_User_PermissionMapping.Where(x => x.ObjectSID == ID).ToList();
                foreach (Role_User_PermissionMapping role in ListOfMappings)
                {
                    Role vgRole = AllRoles.FirstOrDefault(x => x.RoleID == role.RoleID);
                    if (vgRole != null)
                    {
                        List<Page_Role_Mappings> mappings = context.Page_Role_Mappings.Where(x => x.RoleID == vgRole.RoleID.ToString() && x.CanView == true).ToList();
                        foreach (Page_Role_Mappings mapping in mappings)
                        {
                            if (model.Count == pages.Count)
                            {
                                break;
                            }
                            else
                            {
                                if (model.Where(x => x.ID == mapping.PageID).ToList().Count == 0)
                                {
                                    Page page = pages.FirstOrDefault(x => x.ID == mapping.PageID);
                                    if (page != null)
                                    {
                                        model.Add(page);
                                    }
                                }
                            }
                        }
                    }
                }
                return PartialView(model);
            }
        }

        public ActionResult SaveRole(FormCollection form)
        {
            using (SITSASEntities context = new SITSASEntities())
            {
                string strID = form["ExistingRoleID"];
                Guid ID = new Guid();
                Guid.TryParse(strID, out ID);
                Role cty = null;
                if (ID != new Guid())
                {
                    cty = context.Roles.Where(x => x.RoleID == ID).FirstOrDefault();
                    //AddToAudit("Role Updated " + CTY.full_name);
                }
                else
                {
                    cty = new Role();
                    cty.RoleID = Guid.NewGuid();
                }
                cty.RoleName = form["Name"];

                if (ID == new Guid())
                {
                    context.Roles.Add(cty);

                }
                //string cEdit = form["PermEdit"];
                //string cAdd = form["PermAdd"];
                //string cView = form["PermView"];
                //string cDelete = form["PermDelete"];
                //bool boolEdit = false;
                //bool booladd = false;
                //bool boolView = false;
                //bool boolDelete = false;
                //if (cEdit == null)
                //{
                //    boolEdit = false;
                //}
                //else
                //{
                //    if (cEdit.ToUpper() == "ON")
                //    {
                //        boolEdit = true;
                //    }
                //}
                //if (cAdd == null)
                //{
                //    booladd = false;
                //}
                //else
                //{
                //    if (cAdd.ToUpper() == "ON")
                //    {
                //        booladd = true;
                //    }
                //}
                //if (cView == null)
                //{
                //    boolView = false;
                //}
                //else
                //{
                //    if (cView.ToUpper() == "ON")
                //    {
                //        boolView = true;
                //    }
                //}
                //if (cDelete == null)
                //{
                //    boolDelete = false;
                //}
                //else
                //{
                //    if (cDelete.ToUpper() == "ON")
                //    {
                //        boolDelete = true;
                //    }
                //}

                //if (ID != new Guid())
                //{

                //    Role_Permissions existingPermission = context.Role_Permissions.Where(x => x.RoleID == cty.RoleID).FirstOrDefault();
                //    bool NewPermissionRequired = false;
                //    if (existingPermission == null)
                //    {
                //        existingPermission = new Role_Permissions();
                //        NewPermissionRequired = true;
                //    }
                //    existingPermission.RoleID = cty.RoleID;
                //    existingPermission.CanEdit = boolEdit;
                //    existingPermission.CanDelete = boolDelete;
                //    existingPermission.CanAdd = booladd;
                //    existingPermission.CanView = boolView;
                //    if (NewPermissionRequired == true)
                //    {
                //        existingPermission.PermissionsID = Guid.NewGuid();
                //        context.Role_Permissions.Add(existingPermission);
                //    }
                //}
                //else
                //{
                //    Role_Permissions newPermission = new Role_Permissions();
                //    newPermission.PermissionsID = Guid.NewGuid();
                //    newPermission.RoleID = cty.RoleID;
                //    newPermission.CanEdit = boolEdit;
                //    newPermission.CanAdd = booladd;
                //    newPermission.CanDelete = boolDelete;
                //    newPermission.CanView = boolView;

                //    context.Role_Permissions.Add(newPermission);
                //}

                context.SaveChanges();
            }
            return RedirectToAction("Index", "Roles");
        }
    }
}