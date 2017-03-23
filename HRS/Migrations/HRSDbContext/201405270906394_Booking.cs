namespace HRS.Migrations.HRSDbContext
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Booking : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Booking",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CreateTime = c.DateTime(nullable: false),
                        From = c.DateTime(nullable: false),
                        To = c.DateTime(nullable: false),
                        ClientID = c.Int(nullable: false),
                        Status = c.String(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Client", t => t.ClientID, cascadeDelete: true)
                .Index(t => t.ClientID);
            
            CreateTable(
                "dbo.Client",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        IsCompany = c.Boolean(nullable: false),
                        CompanyName = c.String(),
                        Title = c.String(),
                        FirstName = c.String(),
                        LastName = c.String(),
                        Address = c.String(),
                        Phone = c.String(),
                        Email = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Note",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        BookingID = c.Int(nullable: false),
                        Text = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Booking", t => t.BookingID, cascadeDelete: true)
                .Index(t => t.BookingID);
            
            CreateTable(
                "dbo.Pax",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        BookingID = c.Int(nullable: false),
                        Title = c.String(nullable: false),
                        FirstName = c.String(nullable: false),
                        LastName = c.String(nullable: false),
                        Age = c.Int(),
                        Phone = c.String(),
                        RoomItemID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Booking", t => t.BookingID, cascadeDelete: true)
                .ForeignKey("dbo.RoomItem", t => t.RoomItemID)
                .Index(t => t.BookingID)
                .Index(t => t.RoomItemID);
            
            CreateTable(
                "dbo.RoomItem",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        BookingID = c.Int(nullable: false),
                        Status = c.String(nullable: false),
                        From = c.DateTime(nullable: false),
                        To = c.DateTime(nullable: false),
                        RoomTypeID = c.Int(nullable: false),
                        RoomID = c.Int(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Label = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Booking", t => t.BookingID, cascadeDelete: true)
                .ForeignKey("dbo.Room", t => t.RoomID)
                .ForeignKey("dbo.RoomType", t => t.RoomTypeID, cascadeDelete: true)
                .Index(t => t.BookingID)
                .Index(t => t.RoomID)
                .Index(t => t.RoomTypeID);
            
            CreateTable(
                "dbo.Payment",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        BookingID = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Text = c.String(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Booking", t => t.BookingID, cascadeDelete: true)
                .Index(t => t.BookingID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Payment", "BookingID", "dbo.Booking");
            DropForeignKey("dbo.Pax", "RoomItemID", "dbo.RoomItem");
            DropForeignKey("dbo.RoomItem", "RoomTypeID", "dbo.RoomType");
            DropForeignKey("dbo.RoomItem", "RoomID", "dbo.Room");
            DropForeignKey("dbo.RoomItem", "BookingID", "dbo.Booking");
            DropForeignKey("dbo.Pax", "BookingID", "dbo.Booking");
            DropForeignKey("dbo.Note", "BookingID", "dbo.Booking");
            DropForeignKey("dbo.Booking", "ClientID", "dbo.Client");
            DropIndex("dbo.Payment", new[] { "BookingID" });
            DropIndex("dbo.Pax", new[] { "RoomItemID" });
            DropIndex("dbo.RoomItem", new[] { "RoomTypeID" });
            DropIndex("dbo.RoomItem", new[] { "RoomID" });
            DropIndex("dbo.RoomItem", new[] { "BookingID" });
            DropIndex("dbo.Pax", new[] { "BookingID" });
            DropIndex("dbo.Note", new[] { "BookingID" });
            DropIndex("dbo.Booking", new[] { "ClientID" });
            DropTable("dbo.Payment");
            DropTable("dbo.RoomItem");
            DropTable("dbo.Pax");
            DropTable("dbo.Note");
            DropTable("dbo.Client");
            DropTable("dbo.Booking");
        }
    }
}
