using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using System.Reflection;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Tomato.Plugin
{
    /// <summary>
    /// 脚本调用器工厂
    /// </summary>
    public class ScriptCallerFactory
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        public ICollection<Type> ParamTypes { get; set; }

        /// <summary>
        /// 工厂函数名称
        /// </summary>
        public string Name { get; set; }

        public ScriptCallerFactory()
        {
            ParamTypes = new List<Type>();
        }

        /// <summary>
        /// 创建调用器
        /// </summary>
        public dynamic Create(dynamic scriptContext, Func<Type, object> serviceProvider)
        {
            // 参数数量：CallSite, object, args, retval
            int paramsCount = ParamTypes.Count + 3;
            // 工厂函数类型
            // 示例：System.Func`4[[System.Runtime.CompilerServices.CallSite, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
            var funcType = Type.GetType("System.Func`" + paramsCount);
            funcType = funcType.MakeGenericType(new[] { typeof(CallSite), typeof(object) }.Concat(ParamTypes).Concat(new[] { typeof(object) }).ToArray());
            // 创建 CallSite
            var binder = Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, Name, null, this.GetType(), Enumerable.Repeat(
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), paramsCount));
            var callSiteType = typeof(CallSite<>).MakeGenericType(funcType);
            dynamic callSite = callSiteType.GetMethod("Create", new[] { typeof(CallSiteBinder) }).Invoke(null, new object[] { binder });
            // 获取 Target
            var target = (Delegate)(callSite.Target);
            var parameters = ParamTypes.Select(t => serviceProvider(t));
            // 执行构造函数
            return target.DynamicInvoke(new[] { callSite, scriptContext }.Concat(parameters).ToArray());
        }
    }
}
