using System.Collections.Generic;
using HRS.Models;

namespace HRS.ViewModels
{
    public class RoomsModel
    {
        public ICollection<Room> Rooms { get; set; }
        public Room Room { get; set; }
        public IEnumerable<RoomType> RoomTypes { get; set; }
    }
}