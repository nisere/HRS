using System;
using System.ComponentModel.DataAnnotations;

namespace HRS.Models
{
    public class RoomBlackout
    {
        public int ID { get; set; }

        [Required]
        [Display(Name="Room")]
        public int RoomID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? From { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DateInterval("From", ErrorMessage="The date To must be the same with or come after the date From.")]
        public DateTime? To { get; set; }

        public virtual Room Room { get; set; }
    }
}