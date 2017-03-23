using HRS.Models;

namespace HRS.ViewModels
{
    public class RoomBlackoutsModel
    {
        public Room Room { get; set; }
        public RoomBlackout Blackout { get; set; }
        public RoomBlackout DelBlackout { get; set; }
    }
}