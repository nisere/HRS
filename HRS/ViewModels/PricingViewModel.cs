using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HRS.Models;

namespace HRS.ViewModels
{
    public class PricingViewModel
    {
        public PricingRuleSet RuleSet { get; set; }
        public List<PricingRule> Rules { get; set; }
        public ICollection<PricingRuleSet> RuleSets { get; set; }
        public PricingRuleSet DelRuleSet { get; set; }
    }
}