namespace HRS.Migrations.HRSDbContext
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using HRS.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<HRS.DAL.HRSDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\HRSDbContext";
            ContextKey = "HRSDbContext";
        }

        protected override void Seed(HRS.DAL.HRSDbContext context)
        {
        }
    }
}
