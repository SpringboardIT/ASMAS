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
    
    public partial class PermissionGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PermissionGroup()
        {
            this.PermissionGroup_User_Mapping = new HashSet<PermissionGroup_User_Mapping>();
        }
    
        public System.Guid ID { get; set; }
        public string GroupName { get; set; }
        public bool Deleted { get; set; }
        public Nullable<bool> Hidden { get; set; }
        public Nullable<System.Guid> ParentGroupID { get; set; }
        public Nullable<System.Guid> TemplateID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PermissionGroup_User_Mapping> PermissionGroup_User_Mapping { get; set; }
        public virtual Location_PermissionGroupTemplate Location_PermissionGroupTemplate { get; set; }
    }
}
