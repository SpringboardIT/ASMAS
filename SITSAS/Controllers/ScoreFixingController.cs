using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SITSAS.Controllers
{
    public class ScoreFixingController : ApiController
    {
        // GET: api/ScoreFixing
        public string Get(Guid LocationID, DateTime Date, string CompletedBy)
        {
            try
            {
                SITSAS.ScoreFixingHelper.ScoreFixingHelper.FixScores(LocationID, Date, CompletedBy);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return "Error fixing scores: " + ex.Message + " " + ex.InnerException;
                }
                else
                {
                    return "Error fixing scores: " + ex.Message;
                }
            }

            return "Completed Successfully. ";
        }
        public List<Location> GetLocationsWhereAreaIsLive(SITSASEntities context)
        {
            List<Location> model = new List<Location>();
            List<Location> tempLocations = context.Locations.Where(x => x.Deleted == false).ToList();
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
        public string Get(DateTime Date, string CompletedBy)
        {
            try
            {
                using (SITSASEntities context = new SITSASEntities())
                {
                    List<Location> allLocations = GetLocationsWhereAreaIsLive(context);
                    //List<Location> allLocations = context.Locations.Where(x => x.Deleted == false).ToList();

                    SITSAS.ScoreFixingHelper.ScoreFixingHelper.FixScores(allLocations, Date, CompletedBy);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return "Error fixing scores: " + ex.Message + " " + ex.InnerException;
                }
                else
                {
                    return "Error fixing scores: " + ex.Message;
                }

            }

            return "Completed Successfully. \n";
        }
    }
}
