using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class SubCategoriesModel
    {
        public List<SubCategory> ExistingSubCategories { get; set; }
        public AccessRights rights { get; set; }
    }
      public class CreateUpdateSubCategoryModel
    {
        public bool SubCategoryExists { get; set; }
        public SubCategory ExistingSubCategory { get; set; }
        public List<Category> AllCategories { get; set; }
        public AccessRights rights { get; set; }
    }
}