using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Models
{
    public class Note
    {
        public int ID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        [Display(Name = "Note")]
        public string Text { get; set; }

        public virtual Booking Booking { get; set; }

        [NotMapped]
        public int Index { get; set; }
    }
}
