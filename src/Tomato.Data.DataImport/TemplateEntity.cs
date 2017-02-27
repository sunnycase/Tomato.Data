//
// DataImport
// 导入模板实体
//
// 作者             SunnyCase
// 创建日期         2015-09-22
// 修改记录
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Tomato.Data.DataImport.Models;
using Tomato.Data.Validation;
using Tomato.Reflection;

namespace Tomato.Data.DataImport
{
    public delegate object ValueConverter(object value, Type desiredType, out Exception error);

    /// <summary>
    /// 导入模板实体
    /// </summary>
    public class TemplateEntity : IDisposable
    {
        private static TypeCreator _typeCreator = new TypeCreator();

        private Template _template;
        private Type _entityType;
        /// <summary>
        /// 模板化数据模型实例化器
        /// </summary>
        private Func<IEnumerable<object>, Tuple<object, IEnumerable<ValidationResult>>> _activator;

        public TemplateEntity(Template template, Func<Type, object> serviceProvider, ValueConverter valueConverter, IDictionary<string, object> hostObjects = null, dynamic environment = null)
        {
            if (!template.IsLoaded)
                template.Load(environment, hostObjects, serviceProvider);
            _template = template;
            _entityType = MakeTemplateType(template);

            valueConverter = valueConverter ?? ConvertEx.SafeCastValue;

            var props = _entityType.GetProperties();
            _activator = o =>
            {
                var obj = Activator.CreateInstance(_entityType);
                var validations = from e in props.Zip(o, (p, m) =>
                {
                    Exception error;
                    p.SetValue(obj, valueConverter(m, p.PropertyType, out error), null);
                    return new
                    {
                        MemeberName = p.Name,
                        Error = error
                    };
                })
                                  where e.Error != null
                                  select new ValidationResult(e.Error.Message, new[] { e.MemeberName });
                return Tuple.Create(obj, validations);
            };
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="@object">数据源</param>
        /// <returns>实例</returns>
        public Tuple<object, IEnumerable<ValidationResult>> ActivateInstance(IEnumerable<object> @object)
        {
            return _activator(@object);
        }

        public void Import(Array instances)
        {
            _template.Import(instances);
        }

        public void BeginSession()
        {
            _template.BeginSession();
        }

        public ValidationContext CreateValidationContext(object instance, IServiceProvider serviceProvider, IDictionary<object, object> items)
        {
            var context = new ValidationContext(instance, null, items);
#if NET40
            context.ServiceContainer.AddService(typeof(IValidationProvider), new TemplateEntityCustomValidationService(_template));
#else
            context.InitializeServiceProvider(t => t == typeof(IValidationProvider) ? new TemplateEntityCustomValidationService(_template) : null);
#endif
            return context;
        }

        /// <summary>
        /// 构造模板类型
        /// </summary>
        /// <param name="model">模板视图模型</param>
        /// <returns>模板类型</returns>
        private static Type MakeTemplateType(Template template)
        {
            var displayNameCtorAttr = typeof(DisplayAttribute).GetConstructor(new Type[] { });
            var props = from f in template.Fields
                        let dispAttr = new AttributeDefineInfo
                        {
                            Constructor = displayNameCtorAttr,
                            Arguments = new object[] { },
                            Properties = new
                            {
                                Name = f.DisplayName
                            }
                        }
                        select new PropertyDefineInfo
                        {
                            Name = f.Name,
                            Type = f.Type,
                            AutoImplement = true,
                            Attributes = new[] { dispAttr }.Concat(f.Validation.GetValidationModels().Select(o => o.ExportAttribute())).ToArray()
                        };
            return _typeCreator.CreateClass(new ClassDefineInfo
            {
                Name = Guid.NewGuid().ToString("N"),
                Properties = props,
                TypeAttributes = System.Reflection.TypeAttributes.Public
            });
        }
        private class TemplateEntityCustomValidationService : IValidationProvider
        {
            private readonly Template _template;
            public TemplateEntityCustomValidationService(Template template)
            {
                _template = template;
            }

            public ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                // 验证成员
                if (validationContext.MemberName != null)
                {
                    var field = _template.Fields.FirstOrDefault(o => o.Name == validationContext.MemberName);
                    if (field == null)
                        throw new ArgumentException(string.Format("未找到名为 {0} 的成员。", validationContext.MemberName), "validationContext");
                    if (field.Validation != null && field.Validation.CustomValidation != null)
                        return field.Validation.CustomValidation.IsValid(value, validationContext);
                }
                // 验证对象
                else
                {
                    if (_template.Validation != null && _template.Validation.CustomValidation != null)
                        return _template.Validation.CustomValidation.IsValid(value, validationContext);
                }
                return new ValidationResult("Invalid");
            }
        }

#region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _template?.Dispose();
                    _template = null;
                }

                disposedValue = true;
            }
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            Dispose(true);
        }
#endregion
    }
}
