using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Models
{
    public class RoomItem
    {
        public int ID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        [DataType("Status")]
        public string Status { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in")]
        public DateTime? From { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DateIntervalStrict("From", ErrorMessage = "The date Check-out must come after the date Check-in.")]
        [Display(Name = "Check-out")]
        public DateTime? To { get; set; }

        [Required]
        public int RoomTypeID { get; set; }

        public int? RoomID { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Label { get; set; }

        public virtual Booking Booking { get; set; }
        public virtual RoomType RoomType { get; set; }
        public virtual Room Room { get; set; }

        [NotMapped]
        public int Index { get; set; }

        [NotMapped]
        public string RoomTypeName { get; set; }

        [NotMapped]
        public string RoomName { get; set; }
    }
}