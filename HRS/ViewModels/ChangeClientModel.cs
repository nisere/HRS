using System.ComponentModel.DataAnnotations;

namespace HRS.ViewModels
{
    public class ChangeClientModel
    {
        [Required]
        public int ID { get; set; }

        [Required]
        public int ClientID { get; set; }

        public string ClientName { get; set; }
    }
}