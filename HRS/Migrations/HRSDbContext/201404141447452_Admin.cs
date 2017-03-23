namespace HRS.Migrations.HRSDbContext
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Admin : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RoomBlackout",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        RoomID = c.Int(nullable: false),
                        From = c.DateTime(nullable: false),
                        To = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Room", t => t.RoomID, cascadeDelete: true)
                .Index(t => t.RoomID);
            
            CreateTable(
                "dbo.Room",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        RoomTypeID = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.RoomType", t => t.RoomTypeID, cascadeDelete: true)
                .Index(t => t.RoomTypeID);
            
            CreateTable(
                "dbo.RoomType",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.PricingRule",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        RuleSetID = c.Int(nullable: false),
                        RoomTypeID = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.PricingRuleSet", t => t.RuleSetID, cascadeDelete: true)
                .ForeignKey("dbo.RoomType", t => t.RoomTypeID, cascadeDelete: true)
                .Index(t => t.RuleSetID)
                .Index(t => t.RoomTypeID);
            
            CreateTable(
                "dbo.PricingRuleSet",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        From = c.DateTime(nullable: false),
                        To = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PricingRule", "RoomTypeID", "dbo.RoomType");
            DropForeignKey("dbo.PricingRule", "RuleSetID", "dbo.PricingRuleSet");
            DropForeignKey("dbo.Room", "RoomTypeID", "dbo.RoomType");
            DropForeignKey("dbo.RoomBlackout", "RoomID", "dbo.Room");
            DropIndex("dbo.PricingRule", new[] { "RoomTypeID" });
            DropIndex("dbo.PricingRule", new[] { "RuleSetID" });
            DropIndex("dbo.Room", new[] { "RoomTypeID" });
            DropIndex("dbo.RoomBlackout", new[] { "RoomID" });
            DropTable("dbo.PricingRuleSet");
            DropTable("dbo.PricingRule");
            DropTable("dbo.RoomType");
            DropTable("dbo.Room");
            DropTable("dbo.RoomBlackout");
        }
    }
}
