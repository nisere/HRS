using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HRS.Models
{
    public class PricingRule
    {
        public int ID { get; set; }

        [Required]
        [Column("RuleSetID")]
        public int PricingRuleSetID { get; set; }

        [Required]
        public int RoomTypeID { get; set; }

        [Required]
        public Decimal Price { get; set; }

        public virtual PricingRuleSet PricingRuleSet { get; set; }
        public virtual RoomType RoomType { get; set; }
        
        [NotMapped]
        public string RoomTypeName { get; set; }
    }
}