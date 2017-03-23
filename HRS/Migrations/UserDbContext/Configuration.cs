namespace HRS.Migrations.UserDbContext
{
    using System.Data.Entity.Migrations;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using HRS.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<HRS.DAL.UserDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\UserDbContext";
            ContextKey = "UserDbContext";
        }

        protected override void Seed(HRS.DAL.UserDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            roleManager.Create(new IdentityRole("Admin"));
            var idManger = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            var user = new ApplicationUser { UserName = "admin", IsActive = true };
            idManger.Create(user, "admin123");
            idManger.AddToRole(user.Id, "Admin");
        }
    }
}
