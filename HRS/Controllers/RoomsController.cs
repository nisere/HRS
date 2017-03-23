using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRS.Models;
using HRS.DAL;
using HRS.ViewModels;
using System.Data.Entity;
using System.Net;

namespace HRS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private HRSDbContext db = new HRSDbContext();

        private ICollection<Room> getRooms()
        {
            var list = db.Rooms
                .Include(r => r.RoomType)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.RoomType.Name)
                .ThenBy(r => r.Name)
                .ToList();

            return list;
        }

        private IEnumerable<RoomType> getRoomTypes()
        {
            var list = db.RoomTypes
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.Name)
                .ToList();

            return list;
        }

        //
        // GET: /Rooms/
        public ActionResult Index()
        {
            var model = new RoomsModel();
            model.Rooms = getRooms();
            model.Room = new Room();
            model.RoomTypes = getRoomTypes();

            return View(model);
        }

        //
        // GET: /Rooms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id != null)
            {
                var room = db.Rooms.Find(id);
                if (room != null)
                {
                    room.IsActive = !room.IsActive;
                    db.Entry(room).State = EntityState.Modified;
                    db.SaveChanges();

                }
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /Rooms/Create
        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        //
        // POST: /Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,RoomTypeID,IsActive")] Room room)
        {

            if (ModelState.IsValid)
            {
                db.Rooms.Add(room);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            var model = new RoomsModel();
            model.Rooms = getRooms();
            model.Room = room;
            model.RoomTypes = getRoomTypes();

            return View("Index", model);
        }

        //
        // GET: Rooms/Blackouts/5
        public ActionResult Blackouts(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
            Room room = db.Rooms.Find(id);
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var model = new RoomBlackoutsModel();
            model.Room = room;
            model.Blackout = new RoomBlackout { RoomID = id.Value };
            ViewBag.Action = "Create";

            return View(model);
        }

        //
        // GET: /Rooms/SaveBlackout/5
        public ActionResult SaveBlackout(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            RoomBlackout blackout = db.Blackouts.Find(id);
            if (blackout == null)
            {
                return RedirectToAction("Index");
            }

            var model = new RoomBlackoutsModel();
            model.Room = blackout.Room;
            model.Blackout = blackout;
            ViewBag.Action = "Edit";

            return View("Blackouts", model);
        }

        //
        // POST: /Rooms/SaveBlackout/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveBlackout(RoomBlackout blackout, string action)
        {

            if (ModelState.IsValid)
            {
                if (action == "Create")
                {
                    // Create
                    db.Blackouts.Add(blackout);
                    db.SaveChanges();
                }
                else
                {
                    // Edit
                    db.Entry(blackout).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return RedirectToAction("Blackouts", new { id = blackout.RoomID });
            }

            var model = new RoomBlackoutsModel();
            model.Room = db.Rooms.Find(blackout.RoomID);
            model.Blackout = blackout;
            ViewBag.Action = (action == "Create") ? "Create" : "Edit";

            return View("Blackouts", model);
        }

        //
        // GET: /Rooms/DeleteBlackout/5
        public ActionResult DeleteBlackout(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            RoomBlackout blackout = db.Blackouts.Find(id);
            if (blackout == null)
            {
                return RedirectToAction("Index");
            }

            var model = new RoomBlackoutsModel();
            model.Room = blackout.Room;
            model.Blackout = new RoomBlackout { RoomID = id.Value };
            model.DelBlackout = blackout;
            ViewBag.Action = "Create";

            return View("Blackouts", model);
        }

        //
        // POST: /Rooms/DeleteBlackout/5
        [HttpPost, ActionName("DeleteBlackout")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBlackoutConfirm(int id)
        {
            RoomBlackout blackout = db.Blackouts.Find(id);
            db.Blackouts.Remove(blackout);
            db.SaveChanges();
            return RedirectToAction("Blackouts", new { id = blackout.RoomID });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}