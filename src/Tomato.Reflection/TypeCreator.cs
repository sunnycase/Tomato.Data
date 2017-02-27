using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Tomato.Reflection
{
    public class AttributeDefineInfo
    {
        public ConstructorInfo Constructor { get; set; }
        public object[] Arguments { get; set; }
        public object Properties { get; set; }
    }

    public class PropertyDefineInfo
    {
        public string Name { get; set; }
        public bool AutoImplement { get; set; }
        public Type Type { get; set; }
        public IEnumerable<AttributeDefineInfo> Attributes { get; set; }
    }

    public class ClassDefineInfo
    {
        public string Name { get; set; }
        public TypeAttributes TypeAttributes { get; set; }
        public IEnumerable<PropertyDefineInfo> Properties { get; set; }
        public Type Parent { get; set; }

        public ClassDefineInfo()
        {
            Properties = Enumerable.Empty<PropertyDefineInfo>();
            Parent = typeof(object);
        }
    }

    public class TypeCreator
    {
        private ModuleBuilder moduleBuilder;

        public TypeCreator()
        {
            var asmName = new AssemblyName("Sharewin.DynamicAssembly." + Guid.NewGuid().ToString("N"));
            var assembly =
#if NET40
            AppDomain.CurrentDomain
#else
            AssemblyBuilder
#endif
            .DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            moduleBuilder = assembly.DefineDynamicModule("DynamicModule." + Guid.NewGuid().ToString("N"));
        }

        public Type CreateClass(ClassDefineInfo info)
        {
            var typeBuilder = moduleBuilder.DefineType(info.Name, TypeAttributes.Class | info.TypeAttributes, info.Parent);

            foreach (var property in info.Properties)
            {
                var propBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.Type, null);
                // 设置特性
                foreach (var attr in property.Attributes)
                {
                    CustomAttributeBuilder attrBuilder;
                    if (attr.Properties != null)
                    {
                        var propProvides = attr.Properties.GetType().GetProperties();
                        var propsHas = propProvides.Select(o => attr.Constructor.DeclaringType.GetProperty(o.Name)).ToArray();
                        var values = propProvides.Select(o => o.GetValue(attr.Properties, null)).ToArray();

                        attrBuilder = new CustomAttributeBuilder(attr.Constructor, attr.Arguments, propsHas, values);
                    }
                    else
                        attrBuilder = new CustomAttributeBuilder(attr.Constructor, attr.Arguments);

                    propBuilder.SetCustomAttribute(attrBuilder);
                }

                if (property.AutoImplement)
                {
                    var fieldBuilder = typeBuilder.DefineField("_" + property.Name + "Field", property.Type, FieldAttributes.Private);
                    var methodAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
                    // get方法
                    var getMethod = typeBuilder.DefineMethod("get_" + property.Name, methodAttr, property.Type, null);
                    var ilGen = getMethod.GetILGenerator();
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, fieldBuilder);
                    ilGen.Emit(OpCodes.Ret);
                    propBuilder.SetGetMethod(getMethod);
                    // set方法
                    var setMethod = typeBuilder.DefineMethod("set_" + property.Name, methodAttr, typeof(void), new[] { property.Type });
                    ilGen = setMethod.GetILGenerator();
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Stfld, fieldBuilder);
                    ilGen.Emit(OpCodes.Ret);
                    propBuilder.SetSetMethod(setMethod);
                }
            }
            return typeBuilder.
#if NET40
            CreateType
#else
            CreateTypeInfo().AsType
#endif
            ();
        }
    }
}
