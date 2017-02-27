//
// DataImport
// 导入模板
//
// 作者             SunnyCase
// 创建日期         2015-09-22
// 修改记录
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript.Windows;
using Microsoft.CSharp.RuntimeBinder;
using Tomato.Data.Validation;
using System.Dynamic;
using Newtonsoft.Json;
using Tomato.Plugin;

namespace Tomato.Data.DataImport.Models
{
    /// <summary>
    /// 导入模板信息
    /// </summary>
    public class TemplateInfo
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 类别
        /// </summary>
        public string Category { get; set; }
    }

    /// <summary>
    /// 导入模板引用
    /// </summary>
    public class TemplateRefer : TemplateInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Content { get; set; }
        public string FilePath { get; set; }

        public static TemplateRefer LoadFromJson(string filePath, string content)
        {
            var template = JsonConvert.DeserializeObject<TemplateRefer>(content);
            template.FilePath = filePath;
            template.Content = content;
            return template;
        }
    }

    public class Command
    {
        public string DisplayName { get; set; }
        public ScriptCallerFactory ExecuteFactory { get; set; }

        private dynamic _executor;

        public void Execute()
        {
            if (_executor == null)
                throw new InvalidOperationException("没有定义执行器。");
            _executor.execute();
        }

        internal protected virtual void OnDeserialized(ScriptEngine engine, Func<Type, object> serviceProvider)
        {
            if (ExecuteFactory != null)
                _executor = ExecuteFactory.Create(engine.Script, serviceProvider);
        }
    }

    /// <summary>
    /// 导入模板
    /// </summary>
    public class Template : TemplateInfo, IDisposable
    {
        /// <summary>
        /// 脚本包含文件路径
        /// </summary>
        public List<string> ScriptIncludes { get; set; }

        /// <summary>
        /// 包含的程序集
        /// </summary>
        public List<string> AssemblyIncludes { get; set; }

        /// <summary>
        /// 验证
        /// </summary>
        public TemplateValidation Validation { get; set; }

        /// <summary>
        /// 字段集合
        /// </summary>
        public List<TemplateField> Fields { get; set; }

        /// <summary>
        /// 命令
        /// </summary>
        public List<Command> Commands { get; set; } = new List<Command>();

        /// <summary>
        /// 导入器工厂
        /// </summary>
        public ScriptCallerFactory ImporterFactory { get; set; }

        private dynamic _importer;
        /// <summary>
        /// 是否已经加载
        /// </summary>
        internal bool IsLoaded { get; private set; }

        private string _filePath;
        private V8ScriptEngine _engine;

        public List<string> EnvironmentDefinations { get; } = new List<string>();

        public dynamic Environment { get; private set; }

        public Template()
        {
            Fields = new List<TemplateField>();
            ScriptIncludes = new List<string>();
            AssemblyIncludes = new List<string>();
            Validation = new TemplateValidation();
        }

        internal void Load(dynamic environment, IDictionary<string, object> hostObjects, Func<Type, object> serviceProvider)
        {
            if (!IsLoaded)
            {
                Environment = environment ?? new ExpandoObject();
                var dir = Path.GetDirectoryName(_filePath);
                _engine = new V8ScriptEngine();
                {
                    var typeCollection = new HostTypeCollection(new[] { "mscorlib", "System", "System.Core", "System.ComponentModel.DataAnnotations" }
                        .Concat(AssemblyIncludes).ToArray());
                    _engine.AddHostObject("clr", typeCollection);
                    _engine.AddHostObject("host", new HostFunctions());
                    _engine.AddHostObject("Debug", new DebugProvider());
                    _engine.AddHostObject("env", Environment);
                    if (hostObjects != null)
                    {
                        foreach (var item in hostObjects)
                            _engine.AddHostObject(item.Key, HostItemFlags.PrivateAccess, item.Value);
                    }
                    EnvironmentDefinations.ForEach(_engine.Execute);
                    ScriptIncludes.ForEach(s =>
                    {
                        var fileName = Path.IsPathRooted(s) ? s : Path.Combine(dir, s);
                        _engine.Execute(File.ReadAllText(fileName));
                    });
                    OnDeserialized(_engine, serviceProvider);
                    IsLoaded = true;
                }
            }
        }

        internal protected virtual void OnDeserialized(ScriptEngine engine, Func<Type, object> serviceProvider)
        {
            if (Validation != null)
                Validation.OnDeserialized(engine, serviceProvider);
            if (ImporterFactory != null)
                _importer = ImporterFactory.Create(engine.Script, serviceProvider);
            Fields.ForEach(f => f.OnDeserialized(engine, serviceProvider));
            Commands.ForEach(f => f.OnDeserialized(engine, serviceProvider));
        }

        public static Template LoadFromJson(string filePath, string content)
        {
            var template = JsonConvert.DeserializeObject<Template>(content);
            template._filePath = filePath;
            return template;
        }

        internal void Import(Array instances)
        {
            if (_importer == null)
                throw new InvalidOperationException("没有定义导入器。");
            _importer.import(instances);
        }

        internal void BeginSession()
        {
            if (_importer == null)
                throw new InvalidOperationException("没有定义导入器。");
            try
            {
                _importer.beginSession();
            }
            catch (Exception) { }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _engine?.Dispose();
                    _engine = null;
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

    /// <summary>
    /// 字段
    /// </summary>
    [Serializable]
    public class TemplateField
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        [XmlIgnore]
        [NonSerialized]
        private Type _type;
        /// <summary>
        /// 类型
        /// </summary>
        [XmlIgnore]
        public Type Type
        {
            get
            {
                if (_type == null)
                    _type = Type.GetType(TypeName);
                return _type;
            }
            set { _type = value; TypeName = value.AssemblyQualifiedName; }
        }

        [JsonIgnore]
        [XmlAttribute("Type")]
        public string TypeName { get; set; }

        /// <summary>
        /// 验证
        /// </summary>
        public TemplateValidation Validation { get; set; }

        /// <summary>
        /// 获取是否要求有数据
        /// </summary>
        public bool IsRequired
        {
            get { return Validation != null && Validation.Required != null; }
        }

        public TemplateField()
        {
            Validation = new TemplateValidation();
        }

        internal protected virtual void OnDeserialized(ScriptEngine engine, Func<Type, object> serviceProvider)
        {
            if (Validation != null)
                Validation.OnDeserialized(engine, serviceProvider);
        }
    }

    /// <summary>
    /// 验证
    /// </summary>
    [Serializable]
    public class TemplateValidation
    {
        /// <summary>
        /// 需要指定数据字段
        /// </summary>
        public RequiredValidationModel Required { get; set; }

        /// <summary>
        /// 自定义验证
        /// </summary>
        public ExternalValidationModel CustomValidation { get; set; }

        internal protected virtual void OnDeserialized(ScriptEngine engine, Func<Type, object> serviceProvider)
        {
            if (CustomValidation != null)
                CustomValidation.OnDeserialized(engine, serviceProvider);
        }

        public virtual IEnumerable<ValidationModel> GetValidationModels()
        {
            if (Required != null)
                yield return Required;
            if (CustomValidation != null)
                yield return CustomValidation;
        }
    }

    [Serializable]
    public class ExternalValidationModel : Tomato.Data.Validation.ExternalValidationModel
    {
        public ScriptCallerFactory Factory { get; set; }

        [NonSerialized]
        private dynamic _validator;

        internal protected virtual void OnDeserialized(ScriptEngine engine, Func<Type, object> serviceProvider)
        {
            if (Factory != null)
                _validator = Factory.Create(engine.Script, serviceProvider);
        }

        public ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (_validator != null)
                return _validator.isValid(value, validationContext);
            return ValidationResult.Success;
        }
    }
}
