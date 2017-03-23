using System.Web.Mvc;
using HRS.Models;
using HRS.ViewModels;
using HRS.DAL;
using System.Linq;
using System;
using System.Data.Entity;

namespace HRS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private HRSDbContext db = new HRSDbContext();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Index()
        {
            var model = new FrontdeskModel();

            model.From = DateTime.Now.Date.AddDays(-3);
            model.To = model.From.AddDays(33);

            model.RoomTypes = db.RoomTypes
                .Join(db.Rooms, rt => rt.ID, r => r.RoomTypeID, (x, y) => new { RoomType = x, Room = y })
                .Where(join => join.RoomType.IsActive && join.Room.IsActive)
                .OrderBy(join => join.RoomType.Name)
                .ThenBy(join => join.Room.Name)
                .GroupBy(join => join.RoomType.ID)
                .Select(group => new FrontdeskRoomType { RoomType = group.Select(row => row.RoomType).FirstOrDefault(),
                    Rooms = group.Select(row => row.Room).OrderBy(r => r.Name).ToList() })
                .OrderBy(frt => frt.RoomType.Name)
                .ToList();

            model.RoomItems = db.RoomItems
                .Where(ri => ri.RoomID != null)
                .Where(ri => ri.Status != Status.Cancelled)
                .Where(ri => ri.To > model.From && ri.From <= model.To)
                .GroupBy(group => group.RoomID.Value)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.OrderBy(row => row.From).ToList());

            model.Blackouts = db.Blackouts
                .Where(b => b.To >= model.From && b.From <= model.To)
                .GroupBy(group => group.RoomID)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.OrderBy(row => row.From).ToList());

            return View(model);
        }
    }
}