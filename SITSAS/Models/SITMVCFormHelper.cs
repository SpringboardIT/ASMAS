using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SITSAS.Models
{
    public static class SITMVCFormHelper
    {
        public static Guid GetGuidFromForm(FormCollection form, string Key)
        {
            string ls = form[Key];
            Guid lg = new Guid();
            Guid.TryParse(ls, out lg);
            return lg;
        }
        public static DateTime GetDateTimeFromForm(FormCollection form, string Key)
        {
            string ls = form[Key];
            DateTime lg = new DateTime();
            DateTime.TryParse(ls, out lg);
            return lg;
        }
        public static int GetIntegerFromForm(FormCollection form, string Key)
        {
            string ls = form[Key];
            int lg = 0;
            int.TryParse(ls, out lg);
            return lg;
        }
        public static decimal GetDecimalFromForm(FormCollection form, string Key)
        {
            string ls = form[Key];
            decimal lg = 0;
            decimal.TryParse(ls, out lg);
            return lg;
        }
        public static bool GetBooleanFromForm(FormCollection form, string Key)
        {
            if (form.AllKeys.Contains(Key))
            {
                return true;
            }
            return false;
        }
        public static string GetStringFromForm(FormCollection form, string Key)
        {
            return form[Key];
        }
        public static List<Guid> GetGuidCollectionFromForm(FormCollection form, string Key)
        {
            List<Guid> model = new List<Guid>();
            string ls = form[Key];
            if (!string.IsNullOrEmpty(ls))
            {
                List<string> lsArr = ls.Split(',').ToList();
                foreach (string lsID in lsArr)
                {
                    Guid lg = new Guid();
                    Guid.TryParse(lsID, out lg);
                    model.Add(lg);
                }
            }
           
            return model;
        }
    }
}