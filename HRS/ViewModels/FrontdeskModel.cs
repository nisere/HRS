using HRS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRS.ViewModels
{
    public class FrontdeskRoomType
    {
        public RoomType RoomType { get; set; }
        public List<Room> Rooms { get; set; }
    }

    public class FrontdeskModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<FrontdeskRoomType> RoomTypes { get; set; }
        public Dictionary<int, List<RoomItem>> RoomItems { get; set; }
        public Dictionary<int, List<RoomBlackout>> Blackouts { get; set; }
    }
}