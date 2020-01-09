﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SITSAS
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class SITSASEntities : DbContext
    {
        public SITSASEntities()
            : base("name=SITSASEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Answer_Operators> Answer_Operators { get; set; }
        public virtual DbSet<Answer_ScoreMappings> Answer_ScoreMappings { get; set; }
        public virtual DbSet<Answer> Answers { get; set; }
        public virtual DbSet<CalculationModel> CalculationModels { get; set; }
        public virtual DbSet<DataMapping> DataMappings { get; set; }
        public virtual DbSet<DataMappingType> DataMappingTypes { get; set; }
        public virtual DbSet<IncidentType> IncidentTypes { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<PermissionGroup> PermissionGroups { get; set; }
        public virtual DbSet<PermissionGroup_User_Mapping> PermissionGroup_User_Mapping { get; set; }
        public virtual DbSet<QuestionnaireGroup> QuestionnaireGroups { get; set; }
        public virtual DbSet<Questionnaire> Questionnaires { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<Result_Answers> Result_Answers { get; set; }
        public virtual DbSet<Result_Answers_Fixings> Result_Answers_Fixings { get; set; }
        public virtual DbSet<Result_Headers_Fixings> Result_Headers_Fixings { get; set; }
        public virtual DbSet<Role_Permissions> Role_Permissions { get; set; }
        public virtual DbSet<Role_User_PermissionMapping> Role_User_PermissionMapping { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<StalenessProfile> StalenessProfiles { get; set; }
        public virtual DbSet<SubCategory> SubCategories { get; set; }
        public virtual DbSet<SystemSetting> SystemSettings { get; set; }
        public virtual DbSet<Area> Areas { get; set; }
        public virtual DbSet<FrequencyProfile> FrequencyProfiles { get; set; }
        public virtual DbSet<FrequencyProfile_Dates> FrequencyProfile_Dates { get; set; }
        public virtual DbSet<TaskStatu> TaskStatus { get; set; }
        public virtual DbSet<Page> Pages { get; set; }
        public virtual DbSet<Page_Role_Mappings> Page_Role_Mappings { get; set; }
        public virtual DbSet<Result_Headers> Result_Headers { get; set; }
        public virtual DbSet<Task> Tasks { get; set; }
        public virtual DbSet<Location_PermissionGroupTemplate> Location_PermissionGroupTemplate { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
    
        public virtual ObjectResult<Questionnaire> GetQuestionnairesForUser(string userID, Nullable<bool> includeDeleted)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Questionnaire>("GetQuestionnairesForUser", userIDParameter, includeDeletedParameter);
        }
    
        public virtual ObjectResult<Questionnaire> GetQuestionnairesForUser(string userID, Nullable<bool> includeDeleted, MergeOption mergeOption)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Questionnaire>("GetQuestionnairesForUser", mergeOption, userIDParameter, includeDeletedParameter);
        }
    
        public virtual ObjectResult<Area> GetAreasForUser(string userID, Nullable<bool> includeDeleted)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Area>("GetAreasForUser", userIDParameter, includeDeletedParameter);
        }
    
        public virtual ObjectResult<Area> GetAreasForUser(string userID, Nullable<bool> includeDeleted, MergeOption mergeOption)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Area>("GetAreasForUser", mergeOption, userIDParameter, includeDeletedParameter);
        }
    
        public virtual ObjectResult<Location> GetLocationsForUser(string userID, Nullable<bool> includeDeleted)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 240;
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Location>("GetLocationsForUser", userIDParameter, includeDeletedParameter);
        }
    
        public virtual ObjectResult<Location> GetLocationsForUser(string userID, Nullable<bool> includeDeleted, MergeOption mergeOption)
        {
            var userIDParameter = userID != null ?
                new ObjectParameter("UserID", userID) :
                new ObjectParameter("UserID", typeof(string));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Location>("GetLocationsForUser", mergeOption, userIDParameter, includeDeletedParameter);
        }
    
        public virtual int ClearPartEnteredQuestionnaire(Nullable<System.Guid> iD)
        {
            var iDParameter = iD.HasValue ?
                new ObjectParameter("ID", iD) :
                new ObjectParameter("ID", typeof(System.Guid));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ClearPartEnteredQuestionnaire", iDParameter);
        }
    
        public virtual int ApproveQuestionnaire(Nullable<System.Guid> iD)
        {
            var iDParameter = iD.HasValue ?
                new ObjectParameter("ID", iD) :
                new ObjectParameter("ID", typeof(System.Guid));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ApproveQuestionnaire", iDParameter);
        }
    }
}
