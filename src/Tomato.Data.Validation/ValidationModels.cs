//
// Sharewin Data Validation
// 验证模型
//
// 作者             郭晖
// 创建日期         2015-07-20
// 修改记录
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Tomato.Reflection;

namespace Tomato.Data.Validation
{
    /// <summary>
    /// 验证模型接口
    /// </summary>
    public abstract class ValidationModel
    {
        /// <summary>
        /// 导出特性定义
        /// </summary>
        /// <returns>特性定义</returns>
        public abstract AttributeDefineInfo ExportAttribute();
    }

    /// <summary>
    /// 指定需要数据字段值
    /// </summary>
    public class RequiredValidationModel : ValidationModel
    {
        private static readonly ConstructorInfo ctorInfo = typeof(RequiredAttribute).GetConstructor(new Type[] { });

        public override AttributeDefineInfo ExportAttribute()
        {
            return new AttributeDefineInfo
            {
                Constructor = ctorInfo,
                Arguments = new object[] { }
            };
        }
    }

    /// <summary>
    /// 外部验证
    /// </summary>
    public abstract class ExternalValidationModel : ValidationModel
    {
        private static readonly ConstructorInfo ctorInfo = typeof(ExternalValidationAttribute).GetConstructor(new Type[] { });

        public override AttributeDefineInfo ExportAttribute()
        {
            return new AttributeDefineInfo
            {
                Constructor = ctorInfo,
                Arguments = new object[] { }
            };
        }
    }
}
