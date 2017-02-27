using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Tomato.Data.Validation
{
    /// <summary>
    /// 外部验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ExternalValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var provider = (IValidationProvider)validationContext.GetService(typeof(IValidationProvider));
            if (provider == null)
                throw new ArgumentException("未提供 IValidationProvider 服务。", "validationContext");
            return provider.IsValid(value, validationContext);
        }
    }
}
