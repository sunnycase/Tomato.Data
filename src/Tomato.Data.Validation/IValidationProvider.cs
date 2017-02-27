//
// Sharewin Data Validation
// 验证提供程序接口
//
// 作者             郭晖
// 创建日期         2015-09-22
// 修改记录
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Tomato.Data.Validation
{
    /// <summary>
    /// 验证提供程序接口
    /// </summary>
    public interface IValidationProvider
    {
        ValidationResult IsValid(object value, ValidationContext validationContext);
    }
}
