using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRS.DAL;
using HRS.Models;
using HRS.ViewModels;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Transactions;

namespace HRS.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        static object EditLock = new Object();
        static object AddLock = new Object();

        private class _RoomType
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        private class _Room
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        private class _RoomLabel
        {
            public int ID { get; set; }
            public string Label { get; set; }
        }

        private HRSDbContext db = new HRSDbContext();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private IEnumerable<_RoomType> getRoomTypes()
        {
            var list = db.RoomTypes
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.Name)
                .Select(rt => new _RoomType { ID = rt.ID, Name = rt.Name })
                .ToList();

            return list;
        }

        private IEnumerable<_Room> getRooms(int roomType, DateTime from, DateTime to, int? roomID = null)
        {
            // remove check-out day
            to = to.AddDays(-1);

            var sql = @"-- first get all active rooms for the given room type
                        WITH rooms (ID, Name)
                        AS
                        (
                            SELECT ID, Name
                            FROM Room
                            WHERE RoomTypeID = @roomType AND IsActive = 'True'
                        )

                        SELECT * FROM rooms

                        -- then remove ocuppied rooms in given date interval
                        EXCEPT

                        SELECT DISTINCT RoomID AS 'ID', Name
                        FROM RoomItem
                        INNER JOIN rooms ON rooms.ID = RoomID
                        WHERE (RoomItem.Status = @booked OR RoomItem.Status = @checkedIn)
                            AND [From] <= @to AND [To] > @from
                        
                        -- then remove blocked rooms in given date interval                        
                        EXCEPT

                        SELECT DISTINCT RoomID AS 'ID', Name
                        FROM RoomBlackout
                        INNER JOIN rooms ON rooms.ID = RoomID
                        WHERE [From] <= @to AND [To] >= @from
                       ";

            var list = db.Database.SqlQuery<_Room>(sql,
                    new SqlParameter("roomType", roomType),
                    new SqlParameter("from", from),
                    new SqlParameter("to", to),
                    new SqlParameter("booked", Status.Booked),
                    new SqlParameter("checkedIn", Status.CheckedIn)
                    ).ToList();

            // if the item already has a room assigned add it to the list (it was removed from the list)
            if (roomID != null)
            {
                var room = db.Rooms.Find(roomID.Value);
                if (room != null)
                {
                    list.Add(new _Room { ID = room.ID, Name = room.Name });
                }
            }

            list.Sort((x, y) => string.Compare(x.Name, y.Name));
            
            return list;
        }

        private IEnumerable<_RoomLabel> getRoomLabels(int bookingID)
        {
            var list = db.RoomItems
                .Where(ri => ri.BookingID == bookingID && (ri.Status == Status.Booked || ri.Status == Status.CheckedIn))
                .OrderBy(ri => ri.Label)
                .Select(ri => new _RoomLabel { ID = ri.ID, Label = ri.Label })
                .ToList();

            return list;
        }

        private int getAvailability(int roomType, DateTime from, DateTime to)
        {
            // remove check-out day
            to = to.AddDays(-1);

            // get the number of rooms of the given room type not blocked in the date interval
            var sql = @"SELECT COUNT(*) 
                        FROM Room 
                        WHERE RoomTypeID = @roomType AND IsActive = 'True'
                            AND ID NOT IN (
                                            SELECT RoomID
                                            FROM RoomBlackout
                                            WHERE [From] <= @to AND [To] >= @from
                                          )
                       ";
            var noRooms = db.Database.SqlQuery<int>(sql,
                    new SqlParameter("roomType", roomType),
                    new SqlParameter("from", from),
                    new SqlParameter("to", to),
                    new SqlParameter("booked", Status.Booked),
                    new SqlParameter("checkedIn", Status.CheckedIn)
                    ).First();

            // get the number of rooms of the given room type booked in the date interval
            sql = @"SELECT COUNT(*) 
                    FROM RoomItem 
                    WHERE RoomTypeID = @roomType AND (Status = @booked OR Status = @checkedIn)
                        AND [From] <= @to AND [To] > @from
                    ";
            var noBooked = db.Database.SqlQuery<int>(sql,
                    new SqlParameter("roomType", roomType),
                    new SqlParameter("from", from),
                    new SqlParameter("to", to),
                    new SqlParameter("booked", Status.Booked),
                    new SqlParameter("checkedIn", Status.CheckedIn)
                    ).First();

            return noRooms - noBooked;
        }

        private decimal getPrice(int roomType, DateTime fromDate, DateTime toDate)
        {
            decimal price = 0;
            do
            {
                price += db.Rules
                    .Include(r => r.PricingRuleSet)
                    .Where(r => r.RoomTypeID == roomType && fromDate >= r.PricingRuleSet.From.Value
                        && fromDate <= r.PricingRuleSet.To.Value).Select(r => r.Price).FirstOrDefault();
                fromDate = fromDate.AddDays(1);
            }
            while (fromDate < toDate);

            return price;
        }

        //
        // GET: /Booking/
        public ActionResult Index()
        {
            ViewBag.RoomTypes = getRoomTypes();
            ViewBag.Statuses = Status.List;
            return View(new SearchModel());
        }

        //
        // GET: /Booking/Search
        public ActionResult Search()
        {
            return RedirectToAction("Index");
        }

        //
        // POST: /Booking/Search
        [HttpPost, ActionName("Search")]
        public ActionResult SearchPost(SearchModel model)
        {
            var results = db.Bookings
                .Include(b => b.Client)
                .GroupJoin(db.Payments, b => b.ID, p => p.BookingID, (b, group) => new SearchResult
                {
                    ID = b.ID,
                    CreateDate = b.CreateTime,
                    ClientName = b.Client.FirstName + " " + b.Client.LastName,
                    ClientID = b.ClientID.Value,
                    CheckIn = b.From.Value,
                    CheckOut = b.To.Value,
                    Status = b.Status,
                    Price = b.Price,
                    Balance = b.Price - group.Select(p => p.Value).DefaultIfEmpty(0).Sum()
                });
            var empty = true;

            if (model.CreateDateFrom != null)
            {
                results = results.Where(b => b.CreateDate >= model.CreateDateFrom);
                empty = false;
            }
            if (model.CreateDateTo != null)
            {
                var createDateTo = model.CreateDateTo.Value.AddHours(23).AddMinutes(59).AddSeconds(59);
                results = results.Where(b => b.CreateDate <= createDateTo);
                empty = false;
            }
            if (model.CheckInFrom != null)
            {
                results = results.Where(b => b.CheckIn >= model.CheckInFrom);
                empty = false;
            }
            if (model.CheckOutTo != null)
            {
                results = results.Where(b => b.CheckOut <= model.CheckOutTo);
                empty = false;
            }
            if (model.CheckOutFrom != null)
            {
                results = results.Where(b => b.CheckOut >= model.CheckOutFrom);
                empty = false;
            }
            if (model.CheckOutTo != null)
            {
                results = results.Where(b => b.CheckOut <= model.CheckOutTo);
                empty = false;
            }
            if (model.ClientID != null)
            {
                results = results.Where(b => b.ClientID == model.ClientID);
                empty = false;
            }
            if (model.Statuses != null)
            {
                results = results.Where(b => model.Statuses.Contains(b.Status));
                empty = false;
            }
            if (model.RoomTypeID != null)
            {
                results = results.Where(b => db.RoomItems
                    .Where(ri => ri.BookingID == b.ID)
                    .Where(ri => ri.RoomTypeID == model.RoomTypeID)
                    .Any());
                empty = false;
            }

            if (!empty)
            {
                model.Results = results.ToList();

                foreach (var result in model.Results)
                {
                    var rooms = db.RoomItems
                        .Include(ri => ri.RoomType)
                        .Where(ri => ri.BookingID == result.ID)
                        .GroupBy(ri => new { RoomTypeID = ri.RoomTypeID, RoomTypeName = ri.RoomType.Name })
                        .Select(group => new SearchRoomGroup { Count = group.Count(), RoomTypeName = group.Key.RoomTypeName })
                        .ToList();

                    result.Rooms = string.Join(", ", rooms);
                }
            }

            ViewBag.RoomTypes = getRoomTypes();
            ViewBag.Statuses = Status.List;
            return View("Index", model);
        }

        //
        // GET: /Booking/Details/5
        // AJAX: /Booking/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return RedirectToAction("Index");
            }
            booking.RoomItems.OrderBy(ri => ri.Label);

            ViewBag.Statuses = Status.List;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Details", booking);
            }

            return View(booking);
        }

        //
        // POST: /Booking/Details/5
        [HttpPost, ActionName("Details")]
        public ActionResult DetailsPost(int? id)
        {
            return RedirectToAction("Details", new { id = id });
        }


        // GET: /Booking/Create
        public ActionResult Create()
        {
            ViewBag.RoomTypes = getRoomTypes();
            ViewBag.Availability = new int[1] { 0 };

            var booking = new Booking();
            booking.RoomItemList = new List<RoomItem> { new RoomItem { Status = Status.Booked } };
            booking.ClientID = null;
            booking.Status = Status.Booked;

            return View(booking);
        }

        private void setCreateViewBag(Booking booking)
        {
            var roomTypes = getRoomTypes();
            var avail = new int[booking.RoomItemList.Count()];

            for (var i = 0; i < booking.RoomItemList.Count(); i++)
            {
                var item = booking.RoomItemList[i];
                if (item.From == null || item.To == null || item.RoomTypeID == 0) 
                {
                    avail[i] = 0;
                }
                else
                {
                    avail[i] = getAvailability(item.RoomTypeID, item.From.Value, item.To.Value);
                }
            }

            ViewBag.RoomTypes = getRoomTypes();
            ViewBag.Availability = avail;
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Booking booking, string index)
        {
            if (Request.Form.AllKeys.Contains("Delete"))
            {
                var count = booking.RoomItemList.Count;
                int i;
                var ok = Int32.TryParse(index, out i);

                if (count > 1 && ok && i >= 0 && i < count)
                {
                    booking.RoomItemList.RemoveAt(i);
                    ModelState.Clear();
                }

                for (i = 0; i < booking.RoomItemList.Count; i++)
                {
                    booking.Price += booking.RoomItemList[i].Price;
                }

                setCreateViewBag(booking);

                return View(booking);
            }

            booking.Price = 0;
            var from = booking.RoomItemList[0].From;
            var to = booking.RoomItemList[0].To;
            for (int i = 0; i < booking.RoomItemList.Count; i++)
            {
                var room = booking.RoomItemList[i];
                if (room.From < from)
                {
                    from = room.From;
                }
                if (room.To > to)
                {
                    to = room.To;
                }
                booking.Price += room.Price;
            }

            if (Request.Form.AllKeys.Contains("AddItem")) 
            {
                booking.RoomItemList.Add(new RoomItem { Status = Status.Booked, From = from, To = to });

                setCreateViewBag(booking);

                return View(booking);
            }

            booking.From = from;
            booking.To = to;
            booking.CreateTime = DateTime.Now;

            ModelState.Remove("From");
            ModelState.Remove("To");

            if (ModelState.IsValid)
            {
                try
                {
                    lock (AddLock)
                    {
                        using (var transaction = new TransactionScope())
                        {
                            db.Bookings.Add(booking);
                            db.SaveChanges();

                            decimal price = 0;
                            foreach (var room in booking.RoomItemList)
                            {
                                room.BookingID = booking.ID;
                                db.RoomItems.Add(room);
                                if (getAvailability(room.RoomTypeID, room.From.Value, room.To.Value) > 0)
                                {
                                    db.SaveChanges();
                                    price += room.Price;
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }

                            booking.Price = price;
                            db.Entry(booking).State = EntityState.Modified;
                            db.SaveChanges();

                            transaction.Complete();

                            return RedirectToAction("Details", new { id = booking.ID });
                        }
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            setCreateViewBag(booking);

            return View(booking);
        }

        //
        // AJAX: /Booking/GetClient/chera
        public ActionResult GetClient(string term)
        {
            if (Request.IsAjaxRequest())
            {
                var list = db.Clients
                    .Where(c => c.LastName.ToLower().StartsWith(term.ToLower()) || c.CompanyName.ToLower().StartsWith(term.ToLower()))
                    .ToList();

                var clients = from Client c in list
                              select new { id = c.ID, label = c.FullName };

                return Json(clients, JsonRequestBehavior.AllowGet);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/CreateClient
        public ActionResult CreateClient()
        {
            if (Request.IsAjaxRequest())
            {
                ViewBag.Titles = Title.List;

                return PartialView("_CreateClient", new Client());
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/CreateClient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClient(Client client)
        {
            if (!Request.IsAjaxRequest())
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Clients.Add(client);
                    db.SaveChanges();

                    ViewBag.Name = client.FullName;

                    return PartialView("_CreateClientOK");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving.");
                }
            }

            ViewBag.Titles = Title.List;

            return PartialView("_CreateClient", client);
        }

        //
        // AJAX: /Booking/Edit/5
        public ActionResult Edit(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Statuses = Status.List;

                return PartialView("_EditDetails", booking);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Booking booking)
        {
            ModelState.Remove("ClientID");

            if (ModelState.IsValid)
            {
                try
                {
                    var dbBooking = db.Bookings.Find(booking.ID);
                    dbBooking.Status = booking.Status;
                    db.Entry(dbBooking).State = EntityState.Modified;

                    foreach (var item in dbBooking.RoomItems)
                    {
                        if (item.Status != Status.Cancelled)
                        { 
                            item.Status = booking.Status; 
                        }
                    }

                    db.SaveChanges();

                    return PartialView("_Details", dbBooking);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            ViewBag.Statuses = Status.List;

            return PartialView("_EditDetails", booking);
        }

        //
        // AJAX: /Booking/EditClient/5
        public ActionResult EditClient(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                booking.Client.BookingID = booking.ID;

                ViewBag.Titles = Title.List;

                return PartialView("_EditClient", booking.Client);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/EditClient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditClient(Client client)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(client).State = EntityState.Modified;

                    db.SaveChanges();

                    return PartialView("_Client", client);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            ViewBag.Titles = Title.List;

            return PartialView("_EditClient", client);
        }

        //
        // AJAX: /Booking/Client/5
        public ActionResult Client(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                booking.Client.BookingID = booking.ID;

                ViewBag.Titles = Title.List;

                return PartialView("_Client", booking.Client);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/ChangeClient/5
        public ActionResult ChangeClient(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                var model = new ChangeClientModel
                {
                    ID = booking.ID
                };

                return PartialView("_ChangeClient", model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/ChangeClient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeClient(ChangeClientModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var booking = db.Bookings.Find(model.ID);
                    booking.ClientID = model.ClientID;

                    db.Entry(booking).State = EntityState.Modified;
                    db.SaveChanges();

                    booking.Client.BookingID = booking.ID;

                    return PartialView("_Client", booking.Client);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            return PartialView("_EditClient", model);
        }

        //
        // AJAX: /Booking/EditRoom/5?index=3
        public ActionResult EditRoom(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var item = db.RoomItems.Find(id);
                if (item == null)
                {
                    return HttpNotFound();
                }

                item.Index = index.Value;
                item.RoomTypeName = item.RoomType.Name;

                ViewBag.Rooms = getRooms(item.RoomTypeID, item.From.Value, item.To.Value, item.RoomID);
                ViewBag.Statuses = Status.List;

                return PartialView("_EditRoom", item);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/EditRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoom(RoomItem item)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    lock (EditLock)
                    { 
                        var dbItem = db.RoomItems.Find(item.ID);
                        if ((dbItem.Status == Status.Cancelled || dbItem.Status == Status.CheckedOut) 
                            && (item.Status == Status.Booked || item.Status == Status.CheckedIn))
                        {
                            dbItem.RoomID = null;
                        }
                        dbItem.Label = item.Label;
                        dbItem.Status = item.Status;

                        if (item.RoomID != null)
                        { 
                            var rooms = getRooms(dbItem.RoomTypeID, dbItem.From.Value, dbItem.To.Value, dbItem.RoomID);
                            if (!rooms.Select(r => r.ID).Contains(item.RoomID.Value))
                            {
                                throw new Exception();
                            }
                        }
                        dbItem.RoomID = item.RoomID;

                        var booking = db.Bookings.Find(dbItem.BookingID);
                        booking.Price = booking.Price - dbItem.Price + item.Price;
                        db.Entry(booking).State = EntityState.Modified;

                        dbItem.Price = item.Price;
                        db.Entry(dbItem).State = EntityState.Modified;

                        db.SaveChanges();

                        dbItem.Index = item.Index;
                        dbItem.RoomTypeName = item.RoomTypeName;
                        dbItem.RoomName = (dbItem.RoomID != null) ? db.Rooms.Find(dbItem.RoomID).Name : "";

                        return PartialView("_Room", dbItem);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            var dbItem2 = db.RoomItems.Find(item.ID);
            db.Entry(dbItem2).Reload(); // reload because it was modified in try-catch block
            if (dbItem2.Status == Status.CheckedOut || dbItem2.Status == Status.Cancelled)
            {
                var count = db.RoomItems.Where(ri => (ri.Status == Status.Booked || ri.Status == Status.CheckedIn)
                    && ri.RoomID == dbItem2.RoomID).Count();
                if (count > 0)
                {
                    dbItem2.RoomID = null;
                }
            }
            var rooms2 = getRooms(item.RoomTypeID, item.From.Value, item.To.Value, dbItem2.RoomID);
            ViewBag.Rooms = rooms2;
            ViewBag.Statuses = Status.List;

            return PartialView("_EditRoom", item);
        }
        
        //
        // AJAX: /Booking/Room/5?index=3
        public ActionResult Room(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var item = db.RoomItems.Find(id);
                if (item == null)
                {
                    return HttpNotFound();
                }

                item.Index = index.Value;
                item.RoomTypeName = item.RoomType.Name;
                item.RoomName = (item.Room != null) ? item.Room.Name : "";

                return PartialView("_Room", item);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/AddRoom/5?index=3
        public ActionResult AddRoom(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                var item = new RoomItem
                {
                    BookingID = booking.ID,
                    From = booking.From,
                    To = booking.To,
                    Status = booking.Status,
                    Index = index.Value,
                    Label = "Room " + index
                };

                ViewBag.RoomTypes = getRoomTypes();
                ViewBag.Availability = 0;
                ViewBag.Statuses = Status.List;

                return PartialView("_AddRoom", item);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/AddRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoom(RoomItem item)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    lock (AddLock)
                    {
                        db.RoomItems.Add(item);

                        var booking = db.Bookings.Find(item.BookingID);
                        if (item.From < booking.From)
                        {
                            booking.From = item.From;
                        }
                        if (booking.To < item.To)
                        {
                            booking.To = item.To;
                        }

                        if (getAvailability(item.RoomTypeID, item.From.Value, item.To.Value) > 0)
                        {
                            booking.Price += item.Price;
                            db.Entry(booking).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        else
                        {
                            throw new Exception();
                        }

                        item.RoomTypeName = db.RoomTypes.Find(item.RoomTypeID).Name;
                        item.RoomName = (item.RoomID != null) ? db.Rooms.Find(item.RoomID).Name : "";

                        return PartialView("_Room", item);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            ViewBag.RoomTypes = getRoomTypes();
            if (item.From == null || item.To == null || item.RoomTypeID == 0)
            {
                ViewBag.Availability = 0;
            }
            else
            {
                ViewBag.Availability = getAvailability(item.RoomTypeID, item.From.Value, item.To.Value);
            }
            ViewBag.Statuses = Status.List;

            return PartialView("_AddRoom", item);
        }

        //
        // AJAX: /Booking/GetRooms/5
        [HttpPost]
        public ActionResult GetRooms(int? id, int? index, string from, string to)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null || from == null || to == null)
                {
                    return HttpNotFound();
                }

                var rooms = getRooms(id.Value, Convert.ToDateTime(from), Convert.ToDateTime(to));

                return Json(new { Index = index, Rooms = rooms }, JsonRequestBehavior.AllowGet);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/GetBookingPrice/5
        [HttpPost]
        public ActionResult GetBookingPrice(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var price = db.Bookings.Find(id).Price;

                return Json(new { Price = price }, JsonRequestBehavior.AllowGet);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/Balance/5
        [HttpPost]
        public ActionResult GetBalance(int? id)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                decimal value = 0;
                if (booking.Payments != null)
                {
                    value = booking.Payments.Sum(p => p.Value);
                }
                decimal price = booking.Price - value;

                return Json(new { Price = price }, JsonRequestBehavior.AllowGet);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/GetAvailability/5
        [HttpPost]
        public ActionResult GetAvailability(int? id, int? index, string from, string to)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null || from == null || to == null)
                {
                    return HttpNotFound();
                }

                var avail = getAvailability(id.Value, Convert.ToDateTime(from), Convert.ToDateTime(to));
                var price = getPrice(id.Value, Convert.ToDateTime(from), Convert.ToDateTime(to));

                return Json(new { Index = index, Availability = avail, Price = price }, JsonRequestBehavior.AllowGet);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/Pax/5?index=3
        public ActionResult Pax(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var pax = db.Pax.Find(id);
                if (pax == null)
                {
                    return HttpNotFound();
                }

                pax.Index = index.Value;
                pax.RoomLabel = (pax.RoomItem != null) ? pax.RoomItem.Label : "";

                return PartialView("_Pax", pax);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/EditPax/5
        public ActionResult EditPax(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var pax = db.Pax.Find(id);
                if (pax == null)
                {
                    return HttpNotFound();
                }

                pax.Index = index.Value;

                ViewBag.RoomLabels = getRoomLabels(pax.BookingID);
                ViewBag.Titles = Title.List;

                return PartialView("_EditPax", pax);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/EditPax/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPax(Pax pax)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(pax).State = EntityState.Modified;

                    db.SaveChanges();

                    pax.RoomLabel = (pax.RoomItemID != null) ? db.RoomItems.Find(pax.RoomItemID).Label : "";

                    return PartialView("_Pax", pax);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            ViewBag.RoomLabels = getRoomLabels(pax.BookingID);
            ViewBag.Titles = Title.List;

            return PartialView("_EditPax", pax);
        }

        //
        // AJAX: /Booking/AddPax
        public ActionResult AddPax(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                var pax = new Pax { BookingID = booking.ID, Index = index.Value };

                ViewBag.RoomLabels = getRoomLabels(pax.BookingID);
                ViewBag.Titles = Title.List;

                return PartialView("_AddPax", pax);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/AddPax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPax(Pax pax)
        {
            if (!Request.IsAjaxRequest())
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Pax.Add(pax);
                    db.SaveChanges();

                    pax.RoomLabel = (pax.RoomItemID != null) ? db.RoomItems.Find(pax.RoomItemID).Label : "";

                    return PartialView("_Pax", pax);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving.");
                }
            }

            ViewBag.RoomLabels = getRoomLabels(pax.BookingID);
            ViewBag.Titles = Title.List;

            return PartialView("_AddPax", pax);
        }

        //
        // AJAX: /Booking/DeletePax/id
        public ActionResult DeletePax(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var pax = db.Pax.Find(id);
                if (pax == null)
                {
                    return HttpNotFound();
                }

                var model = new DeleteModel { ID = id.Value, Index = index.Value, Subject = "pax", Text = pax.FullName };

                return PartialView("_Delete", model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/DeletePax/5
        [HttpPost, ActionName("DeletePax")]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePaxConfirm(DeleteModel model)
        {
            if (Request.IsAjaxRequest())
            {
                try
                {
                    var pax = db.Pax.Find(model.ID);
                    db.Pax.Remove(pax);
                    db.SaveChanges();

                    ViewBag.Subject = model.Subject;
                    ViewBag.Index = model.Index;
                    return PartialView("_DeleteSuccess");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error.");
                }
                return PartialView("_Delete", model);
            }
            return HttpNotFound();
        }

        //
        // AJAX: /Booking/Payment/5?index=3
        public ActionResult Payment(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var payment = db.Payments.Find(id);
                if (payment == null)
                {
                    return HttpNotFound();
                }

                payment.Index = index.Value;

                return PartialView("_Payment", payment);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/EditPayment/5
        public ActionResult EditPayment(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var payment = db.Payments.Find(id);
                if (payment == null)
                {
                    return HttpNotFound();
                }

                payment.Index = index.Value;

                return PartialView("_EditPayment", payment);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/EditPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment(Payment payment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(payment).State = EntityState.Modified;

                    db.SaveChanges();

                    return PartialView("_Payment", payment);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            return PartialView("_EditPayment", payment);
        }

        //
        // AJAX: /Booking/AddPayment
        public ActionResult AddPayment(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                var payment = new Payment { BookingID = booking.ID, Index = index.Value };

                return PartialView("_AddPayment", payment);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/AddPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment(Payment payment)
        {
            if (!Request.IsAjaxRequest())
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Payments.Add(payment);
                    db.SaveChanges();

                    return PartialView("_Payment", payment);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving.");
                }
            }

            return PartialView("_AddPayment", payment);
        }

        //
        // AJAX: /Booking/DeletePayment/id
        public ActionResult DeletePayment(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var payment = db.Payments.Find(id);
                if (payment == null)
                {
                    return HttpNotFound();
                }

                var model = new DeleteModel { ID = id.Value, Index = index.Value, Subject = "payment",
                    Text = payment.Date.Value.ToShortDateString() + " - " + payment.Value };

                return PartialView("_Delete", model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/DeletePayment/5
        [HttpPost, ActionName("DeletePayment")]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePaymentConfirm(DeleteModel model)
        {
            if (Request.IsAjaxRequest())
            {
                try
                {
                    var payment = db.Payments.Find(model.ID);
                    db.Payments.Remove(payment);
                    db.SaveChanges();

                    ViewBag.Subject = model.Subject;
                    ViewBag.Index = model.Index;
                    return PartialView("_DeleteSuccess");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error.");
                }
                return PartialView("_Delete", model);
            }
            return HttpNotFound();
        }

        //
        // AJAX: /Booking/Note/5?index=3
        public ActionResult Note(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var note = db.Notes.Find(id);
                if (note == null)
                {
                    return HttpNotFound();
                }

                note.Index = index.Value;

                return PartialView("_Note", note);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/EditNote/5
        public ActionResult EditNote(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var note = db.Notes.Find(id);
                if (note == null)
                {
                    return HttpNotFound();
                }

                note.Index = index.Value;

                return PartialView("_EditNote", note);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/EditNote/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditNote(Note note)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(note).State = EntityState.Modified;

                    db.SaveChanges();

                    return PartialView("_Note", note);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            return PartialView("_EditNote", note);
        }

        //
        // AJAX: /Booking/AddNote
        public ActionResult AddNote(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var booking = db.Bookings.Find(id);
                if (booking == null)
                {
                    return HttpNotFound();
                }

                var note = new Note { BookingID = booking.ID, Index = index.Value };

                return PartialView("_AddNote", note);
            }

            return HttpNotFound();
        }

        //
        // AJAX: /Booking/AddNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNote(Note note)
        {
            if (!Request.IsAjaxRequest())
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Notes.Add(note);
                    db.SaveChanges();

                    return PartialView("_Note", note);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving.");
                }
            }

            return PartialView("_AddNote", note);
        }

        //
        // AJAX: /Booking/DeleteNote/id
        public ActionResult DeleteNote(int? id, int? index)
        {
            if (Request.IsAjaxRequest())
            {
                if (id == null || index == null)
                {
                    return HttpNotFound();
                }

                var note = db.Notes.Find(id);
                if (note == null)
                {
                    return HttpNotFound();
                }

                var model = new DeleteModel { ID = id.Value, Index = index.Value, Subject = "note", Text = note.Text };

                return PartialView("_Delete", model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Booking/DeleteNote/5
        [HttpPost, ActionName("DeleteNote")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteNoteConfirm(DeleteModel model)
        {
            if (Request.IsAjaxRequest())
            {
                try
                {
                    var note = db.Notes.Find(model.ID);
                    db.Notes.Remove(note);
                    db.SaveChanges();

                    ViewBag.Subject = model.Subject;
                    ViewBag.Index = model.Index;
                    return PartialView("_DeleteSuccess");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error.");
                }
                return PartialView("_Delete", model);
            }
            return HttpNotFound();
        }

	}
}