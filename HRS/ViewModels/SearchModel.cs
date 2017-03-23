using HRS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HRS.ViewModels
{
    public class SearchRoomGroup
    {
        public int Count { get; set; }
        public string RoomTypeName { get; set; }

        public override string ToString() 
        {
            return Count.ToString() + "x" + RoomTypeName;
        }
    }

    public class SearchResult
    {
        [Display(Name = "Code")]
        public int ID { get; set; }

        [Display(Name = "Create Date")]
        public DateTime CreateDate { get; set; }

        [Display(Name = "Client")]
        public string ClientName { get; set; }

        public int ClientID { get; set; }

        [Display(Name = "Check-in")]
        public DateTime CheckIn { get; set; }

        [Display(Name = "Check-out")]
        public DateTime CheckOut { get; set; }

        public string Rooms { get; set; }

        [DataType("Status")]
        public string Status { get; set; }

        public decimal Price { get; set; }

        public decimal Balance { get; set; }
    }

    public class SearchModel
    {
        [Display(Name = "Create Date")]
        [DataType(DataType.Date)]
        public DateTime? CreateDateFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CreateDateTo { get; set; }

        [Display(Name = "Check-in")]
        [DataType(DataType.Date)]
        public DateTime? CheckInFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckInTo { get; set; }

        [Display(Name = "Check-out")]
        [DataType(DataType.Date)]
        public DateTime? CheckOutFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckOutTo { get; set; }

        [Display(Name = "Client")]
        public int? ClientID { get; set; }

        public string ClientName { get; set; }

        [Display(Name = "Room Type")]
        public int? RoomTypeID { get; set; }

        [Display(Name = "Status")]
        public List<string> Statuses { get; set; }

        public List<SearchResult> Results { get; set; }

    }
}