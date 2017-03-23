using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HRS.Models;
using HRS.ViewModels;
using HRS.DAL;
using System.Transactions;
using System.Data.Entity;

namespace HRS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PricingController : Controller
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

        private ICollection<PricingRuleSet> getRuleSets()
        {
            var list = db.RuleSets
                .OrderBy(rs => rs.Name)
                .ToList();

            return list;
        }

        private List<PricingRule> initRules()
        {
            var rules = new List<PricingRule>();
 
            var roomTypes = db.RoomTypes
                .Where(rt => rt.IsActive)
                .OrderBy(rt => rt.Name)
                .Select(rt => new { ID = rt.ID, Name = rt.Name })
                .ToList();

            foreach (var roomType in roomTypes)
            {
                var rule = new PricingRule { RoomTypeID = roomType.ID, RoomTypeName = roomType.Name };
                rules.Add(rule);
            }

            return rules;
        }

        //
        // GET: /Pricing/
        public ActionResult Index()
        {
            var model = new PricingViewModel();
            model.RuleSet = new PricingRuleSet();
            model.Rules = initRules();
            model.RuleSets = getRuleSets();
            ViewBag.Action = "Create";

            return View(model);
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
        public ActionResult Create(PricingViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var transaction = new TransactionScope())
                    {
                        db.RuleSets.Add(model.RuleSet);
                        db.SaveChanges();

                        foreach (var rule in model.Rules)
                        {
                            rule.PricingRuleSetID = model.RuleSet.ID;
                            db.Rules.Add(rule);
                        }
                        db.SaveChanges();

                        transaction.Complete();
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            model.RuleSets = getRuleSets();
            ViewBag.Action = "Create";

            return View("Index", model);
        }

        //
        // GET: /Rooms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var ruleSet = db.RuleSets.Find(id);
            if (ruleSet == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PricingViewModel();
            model.RuleSet = ruleSet;
            model.Rules = ruleSet.PricingRules.ToList();
            foreach (var rule in model.Rules)
            {
                rule.RoomTypeName = rule.RoomType.Name;
            }
            model.RuleSets = getRuleSets();
            ViewBag.Action = "Edit";

            return View("Index", model);
        }

       //
        // POST: /Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PricingViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(model.RuleSet).State = EntityState.Modified;

                    foreach (var rule in model.Rules)
                    {
                        db.Entry(rule).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            model.RuleSets = getRuleSets();
            ViewBag.Action = "Edit";

            return View("Index", model);
        }

        //
        // GET: /Rooms/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var ruleSet = db.RuleSets.Find(id);
            if (ruleSet == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PricingViewModel();
            model.DelRuleSet = ruleSet;
            model.RuleSets = getRuleSets();
            model.RuleSet = new PricingRuleSet();
            model.Rules = initRules();
            ViewBag.Action = "Create";

            return View("Index", model);
        }

        //
        // POST: /Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirm(int id)
        {
            var ruleSet = db.RuleSets.Find(id);
            db.RuleSets.Remove(ruleSet);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
	}
}