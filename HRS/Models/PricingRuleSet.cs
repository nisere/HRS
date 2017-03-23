using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRS.Models
{
    public class PricingRuleSet
    {
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? From { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DateInterval("From", ErrorMessage = "The date To must be the same with or come after the date From.")]
        public DateTime? To { get; set; }

        public virtual ICollection<PricingRule> PricingRules { get; set; }
    }
}