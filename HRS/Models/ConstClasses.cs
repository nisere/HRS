
using System.Collections.Generic;
namespace HRS.Models
{
    public static class Title
    {
        public const string Mr = "Mr";
        public const string Ms = "Ms";
        public const string Miss = "Miss";
        public const string Mrs = "Mrs";

        public static Dictionary<string, string> List = new Dictionary<string, string> 
        {
            {"Mr", "Mr."},
            {"Ms", "Ms."}, 
            {"Miss", "Miss"},
            {"Mrs", "Mrs."},
        };
    }

    public static class Status
    {
        public const string Booked = "Booked";
        public const string CheckedIn = "CheckedIn";
        public const string CheckedOut = "CheckedOut";
        public const string Cancelled = "Cancelled";

        public static Dictionary<string, string> List = new Dictionary<string, string> 
        {
            {"Booked", "Booked"},
            {"CheckedIn", "Checked-in"},
            {"CheckedOut", "Checked-out"},
            {"Cancelled", "Cancelled"}
        };
    }
}