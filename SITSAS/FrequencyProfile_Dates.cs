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
    
    public partial class FrequencyProfile_Dates
    {
        public System.Guid ID { get; set; }
        public System.Guid FrequencyID { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public System.Guid LocationID { get; set; }
    
        public virtual FrequencyProfile FrequencyProfile { get; set; }
    }
}
