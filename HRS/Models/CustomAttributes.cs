using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HRS.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false)]
    sealed public class DateIntervalAttribute : ValidationAttribute, IClientValidatable
    {
        private readonly string _from;

        public DateIntervalAttribute(string from)
        {
            _from = from;
        }
        
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var fromValue = validationContext.ObjectInstance.GetType().GetProperty(_from)
                    .GetValue(validationContext.ObjectInstance);
            DateTime fromDate = Convert.ToDateTime(fromValue);
            DateTime toDate = Convert.ToDateTime(value);

            if (fromDate > toDate)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }


        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule();
            rule.ErrorMessage = ErrorMessage;
            rule.ValidationType = "dateinterval";
            rule.ValidationParameters.Add("from", _from);

            yield return rule;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    sealed public class DateIntervalStrictAttribute : ValidationAttribute, IClientValidatable
    {
        private readonly string _from;

        public DateIntervalStrictAttribute(string from)
        {
            _from = from;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var fromValue = validationContext.ObjectInstance.GetType().GetProperty(_from)
                    .GetValue(validationContext.ObjectInstance);
            DateTime fromDate = Convert.ToDateTime(fromValue);
            DateTime toDate = Convert.ToDateTime(value);

            if (fromDate >= toDate)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }


        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule();
            rule.ErrorMessage = ErrorMessage;
            rule.ValidationType = "dateintervalstrict";
            rule.ValidationParameters.Add("from", _from);

            yield return rule;
        }
    }
}