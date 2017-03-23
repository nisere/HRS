using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRS.Models
{
    public class Room
    {
        public int ID { get; set; }
        
        [Required]
        [Display(Name="Room Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Room Type")]
        public int RoomTypeID { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public virtual RoomType RoomType { get; set; }

        public virtual ICollection<RoomBlackout> Blackouts { get; set; }
    }
}