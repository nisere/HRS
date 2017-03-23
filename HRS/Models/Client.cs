using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Models
{
    public class Client
    {
        public int ID { get; set; }

        [Display(Name = "Is Company")]
        public bool IsCompany { get; set; }

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [DataType("Title")]
        public string Title { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        [NotMapped]
        public string FullName
        {
            get 
            {
                return (IsCompany)?(CompanyName):(HRS.Models.Title.List[Title] + " " + FirstName + " " + LastName);  
            }
        }

        [NotMapped]
        public int BookingID { get; set; }
    }
}