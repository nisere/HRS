using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Models
{
    public class Booking
    {
        [Display(Name = "Code")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Create Time")]
        public DateTime CreateTime { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in")]
        public DateTime? From { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out")]
        public DateTime? To { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int? ClientID { get; set; }

        [Required]
        [DataType("Status")]
        public string Status { get; set; }

        [Required]
        public decimal Price { get; set; }

        public virtual Client Client { get; set; }
        public virtual ICollection<RoomItem> RoomItems { get; set; }
        public virtual ICollection<Note> Notes { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Pax> Pax { get; set; }

        [NotMapped]
        public List<RoomItem> RoomItemList { get; set; }

        [NotMapped]
        public string ClientName { get; set; }

    }
}