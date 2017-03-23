using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRS.Models
{
    public class RoomType
    {
        public int ID { get; set; }

        [Required]
        [Display(Name="Room Type")]
        public string Name { get; set; }

        [Display(Name="Is Active")]
        public bool IsActive { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }
    }
}