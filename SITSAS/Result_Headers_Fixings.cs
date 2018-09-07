//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class Result_Headers_Fixings
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Result_Headers_Fixings()
        {
            this.Result_Answers_Fixings = new HashSet<Result_Answers_Fixings>();
            this.Result_Headers_Fixings1 = new HashSet<Result_Headers_Fixings>();
        }
    
        public System.Guid ID { get; set; }
        public string CompletedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public System.DateTime SelectedDate { get; set; }
        public System.Guid LocationID { get; set; }
        public System.Guid QuestionnaireID { get; set; }
        public Nullable<System.Guid> ParentID { get; set; }
        public Nullable<System.Guid> IncidentTypeID { get; set; }
    
        public virtual IncidentType IncidentType { get; set; }
        public virtual Location Location { get; set; }
        public virtual Questionnaire Questionnaire { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Result_Answers_Fixings> Result_Answers_Fixings { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Result_Headers_Fixings> Result_Headers_Fixings1 { get; set; }
        public virtual Result_Headers_Fixings Result_Headers_Fixings2 { get; set; }
    }
}
