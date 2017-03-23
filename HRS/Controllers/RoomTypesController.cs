using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRS.Models;
using HRS.DAL;
using HRS.ViewModels;
using System.Data.Entity;

namespace HRS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomTypesController : Controller
    {
        private HRSDbContext db = new HRSDbContext();

        private ICollection<RoomType> getList()
        {
            var list = db.RoomTypes
                .OrderByDescending(rt => rt.IsActive)
                .ThenBy(rt => rt.Name)
                .ToList();

            return list;
        }

        //
        // GET: /RoomTypes/
        public ActionResult Index()
        {
            var model = new RoomTypesModel();
            model.RoomTypes = getList();
            model.RoomType = new RoomType();

            return View(model);
        }

        //
        // GET: /RoomTypes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id != null)
            {
                var roomType = db.RoomTypes.Find(id);
                if (roomType != null)
                {
                    roomType.IsActive = !roomType.IsActive;
                    db.Entry(roomType).State = EntityState.Modified;
                    foreach (var room in roomType.Rooms)
                    {
                        room.IsActive = roomType.IsActive;
                        db.Entry(room).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        //
        // GET: /RoomTypes/Create
        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }

        //
        // POST: /RoomTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,IsActive")] RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                db.RoomTypes.Add(roomType);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            var model = new RoomTypesModel();
            model.RoomTypes = getList();
            model.RoomType = roomType;

            return View("Index", model);
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