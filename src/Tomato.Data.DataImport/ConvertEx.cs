//
// PEDMAS
// 扩展转换辅助类
//
// 作者             SunnyCase
// 创建日期         2015-08-14
// 修改记录
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Tomato.Data.DataImport
{
    public static class ConvertEx
    {
        public static object SafeCastValue(object value, Type desiredType)
        {
            Exception ex;
            return SafeCastValue(value, desiredType, out ex);
        }

        private static readonly string[] _userDateTimeFormat = new[]
        {
            "yyyyMMdd",
            @"yyyy/MM/dd",
            @"yyyy\.M\.d",
            @"yyyy\.M",
            @"yyyy\.MM",
            @"yyyyMM",
            @"yyyy-M-d-H点",
            @"yyyy年M月d日",
            @"yyyy年M月"
        };

        /// <summary>
        /// 安全转换类型（不抛出异常，转换失败时返回 default(T)）
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <param name="desiredType"></param>
        /// <returns></returns>
        public static object SafeCastValue(object value, Type desiredType, out Exception error)
        {
            Contract.Assert(desiredType != null);

            object result;
            try
            {
                var nullUnderlying = Nullable.GetUnderlyingType(desiredType);
                if (nullUnderlying != null)
                {
                    Exception ignoreErr;
                    var innerValue = ConvertEx.SafeCastValue(value, nullUnderlying, out ignoreErr);
                    if (ignoreErr != null)
                        result = Activator.CreateInstance(desiredType, null);
                    else
                        result = Activator.CreateInstance(desiredType, innerValue);
                    if (string.IsNullOrWhiteSpace(value as string))
                        error = null;
                    else
                        error = ignoreErr;
                }
                else
                {
                    // double 到 DateTime 的特殊转换
                    if (value != null && value.GetType() == typeof(double) && desiredType == typeof(DateTime))
                        result = Convert.ChangeType(value.ToString(), desiredType);
                    else
                    {
                        if (desiredType == typeof(DateTime) && value != null)
                        {
                            var dateTime = DateTime.MinValue;
                            if (!(DateTime.TryParse(value.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime) ||
                                _userDateTimeFormat.Any(f => DateTime.TryParseExact(value.ToString(), f, CultureInfo.CurrentCulture,
                                 DateTimeStyles.None, out dateTime))))
                                throw new InvalidCastException("无法转换为日期。");
                            result = dateTime;
                        }
                        else
                            result = Convert.ChangeType(value, desiredType);
                    }
                    error = null;
                }
            }
            catch (Exception ex)
            {
                error = ex;
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
                result = GetDefaultValue(desiredType);
            }
            return result;
        }

        public static object GetDefaultValue(Type type)
        {
            Contract.Assert(type != null);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
