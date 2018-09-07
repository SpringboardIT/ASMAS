using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SITSAS.Models
{
    public class CategoriesModel
    {
        public List<Category> ExistingCategories { get; set; }
        public AccessRights rights { get; set; }
    }
      public class CreateUpdateCategoryModel
    {
        public bool CategoryExists { get; set; }
        public Category ExistingCategory { get; set; }

        public bool AllowEdit { get; set; }

        public AccessRights rights { get; set; }
    }
}