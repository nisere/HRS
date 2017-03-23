using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Models
{
    public class Pax
    {
        public int ID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [Required]
        [DataType("Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        public int? Age { get; set; }

        public string Phone { get; set; }

        public int? RoomItemID { get; set; }

        public virtual Booking Booking { get; set; }
        public virtual RoomItem RoomItem { get; set; }

        [NotMapped]
        public string FullName
        {
            get { return HRS.Models.Title.List[Title] + " " + FirstName + " " + LastName; }
        }

        [NotMapped]
        public int Index { get; set; }

        [NotMapped]
        public string RoomLabel { get; set; }
    }
}