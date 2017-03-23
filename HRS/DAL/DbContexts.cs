using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using HRS.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HRS.DAL
{
    public class HRSDbContext : DbContext
    {
        public HRSDbContext() : base("DefaultConnection") { }

        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomBlackout> Blackouts { get; set; }
        public DbSet<PricingRuleSet> RuleSets { get; set; }
        public DbSet<PricingRule> Rules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<RoomItem> RoomItems { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Pax> Pax { get; set; }
        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }


    public class UserDbContext : IdentityDbContext<ApplicationUser>
    {
        public UserDbContext() : base("DefaultConnection") { }
    }
}