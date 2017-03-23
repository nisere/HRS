using System.Collections.Generic;
using HRS.Models;

namespace HRS.ViewModels
{
    public class RoomTypesModel
    {
        public ICollection<RoomType> RoomTypes { get; set; }
        public RoomType RoomType { get; set; }
    }
}