using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using DynamicDataStore.Core.Model;
using Lokad.ILPack;

namespace DynamicDataStore.Core.Runtime
{
    public class DynamicTypeBuilder
    {
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;

        public TypeBuilder GetTypeBuilder(string typeName, Type baseType, Type genericType = null)
        {
            AssemblyName an = new AssemblyName("DynamicDataStoreAssembly." + typeName);

            if (_assemblyBuilder == null)
            {
                _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            }

            if (_moduleBuilder == null)
            {
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("DynamicDataStoreModule." + typeName);
            }

            TypeBuilder tb = _moduleBuilder.DefineType("DynamicDataStore.Entities." + typeName, TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                genericType != null ? baseType.MakeGenericType(genericType) : baseType);

            return tb;
        }

        public void SaveTypeBuilder(TypeBuilder parentType, string fileName)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), $"generated");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            fileName = $"{fileName}_{DateTime.Now:MMddyyyyhhmmss}";

            var generator = new AssemblyGenerator();
            generator.GenerateAssembly(_assemblyBuilder, Path.Combine($"{basePath}\\{fileName}.dll"));
        }

        public PropertyBuilder CreateProperty(TypeBuilder builder, string propertyName, Type propertyType,
            bool notifyChanged)
        {
            FieldBuilder fieldBuilder = builder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder =
                builder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            MethodBuilder getPropertyBuilder = CreatePropertyGetter(builder, fieldBuilder);
            MethodBuilder setPropertyBuilder = null;

            setPropertyBuilder = notifyChanged
                ? CreatePropertySetterWithNotifyChanged(builder, fieldBuilder, propertyName)
                : CreatePropertySetter(builder, fieldBuilder);

            propertyBuilder.SetGetMethod(getPropertyBuilder);
            propertyBuilder.SetSetMethod(setPropertyBuilder);

            return propertyBuilder;
        }

        private MethodBuilder CreateRaisePropertyChanged(TypeBuilder typeBuilder, FieldBuilder eventField)
        {
            MethodBuilder raisePropertyChangedBuilder =
                typeBuilder.DefineMethod("RaisePropertyChanged",
                    MethodAttributes.Family | MethodAttributes.Virtual,
                    null, new Type[] {typeof(string)});

            ILGenerator raisePropertyChangedIl =
                raisePropertyChangedBuilder.GetILGenerator();
            Label labelExit = raisePropertyChangedIl.DefineLabel();

            raisePropertyChangedIl.Emit(OpCodes.Ldarg_0);
            raisePropertyChangedIl.Emit(OpCodes.Ldfld, eventField);
            raisePropertyChangedIl.Emit(OpCodes.Ldnull);
            raisePropertyChangedIl.Emit(OpCodes.Ceq);
            raisePropertyChangedIl.Emit(OpCodes.Brtrue, labelExit);

            raisePropertyChangedIl.Emit(OpCodes.Ldarg_0);
            raisePropertyChangedIl.Emit(OpCodes.Ldfld, eventField);
            raisePropertyChangedIl.Emit(OpCodes.Ldarg_0);
            raisePropertyChangedIl.Emit(OpCodes.Ldarg_1);
            raisePropertyChangedIl.Emit(OpCodes.Newobj,
                typeof(PropertyChangedEventArgs).GetConstructor(new[] {typeof(string)}));
            raisePropertyChangedIl.EmitCall(OpCodes.Callvirt,
                typeof(PropertyChangedEventHandler).GetMethod("Invoke"), null);

            raisePropertyChangedIl.MarkLabel(labelExit);
            raisePropertyChangedIl.Emit(OpCodes.Ret);

            return raisePropertyChangedBuilder;
        }

        private MethodBuilder CreatePropertySetterWithNotifyChanged(TypeBuilder typeBuilder,
            FieldBuilder fieldBuilder, string propertyName)
        {
            //Raise
            MethodInfo m = typeof(PocoBase).GetMethod("RaisePropertyChanged",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] {typeof(object), typeof(string)}, null);

            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_" + fieldBuilder.Name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null,
                new Type[] {fieldBuilder.FieldType});

            ILGenerator setIlCode = setMethodBuilder.GetILGenerator();
            setIlCode.Emit(OpCodes.Nop);
            setIlCode.Emit(OpCodes.Ldarg_0);
            setIlCode.Emit(OpCodes.Ldarg_1);
            setIlCode.Emit(OpCodes.Stfld, fieldBuilder);
            setIlCode.Emit(OpCodes.Ldarg_0);
            setIlCode.Emit(OpCodes.Ldarg_0);
            setIlCode.Emit(OpCodes.Ldstr, propertyName);
            setIlCode.Emit(OpCodes.Call, m);
            setIlCode.Emit(OpCodes.Nop);
            setIlCode.Emit(OpCodes.Ret);

            return setMethodBuilder;
        }

        public PropertyBuilder CreateVirtualProperty(TypeBuilder classBuilder, string propertyName,
            Type propertyTypeBuilder)
        {
            FieldBuilder fieldBuilder =
                classBuilder.DefineField("_" + propertyName, propertyTypeBuilder, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = classBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault,
                propertyTypeBuilder, null);

            var getSetAttr = MethodAttributes.Public | MethodAttributes.Virtual;
            var mbIdGetAccessor =
                classBuilder.DefineMethod("get_" + propertyName, getSetAttr, propertyTypeBuilder, Type.EmptyTypes);

            var numberGetIlCode = mbIdGetAccessor.GetILGenerator();
            numberGetIlCode.Emit(OpCodes.Ldarg_0);
            numberGetIlCode.Emit(OpCodes.Ldfld, fieldBuilder);
            numberGetIlCode.Emit(OpCodes.Ret);

            var mbIdSetAccessor = classBuilder.DefineMethod("set_" + propertyName, getSetAttr, null,
                new Type[] {propertyTypeBuilder});

            var numberSetIlCode = mbIdSetAccessor.GetILGenerator();
            numberSetIlCode.Emit(OpCodes.Ldarg_0);
            numberSetIlCode.Emit(OpCodes.Ldarg_1);
            numberSetIlCode.Emit(OpCodes.Stfld, fieldBuilder);
            numberSetIlCode.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(mbIdGetAccessor);
            propertyBuilder.SetSetMethod(mbIdSetAccessor);

            return propertyBuilder;
        }

        private MethodBuilder CreatePropertyGetter(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_" + fieldBuilder.Name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                fieldBuilder.FieldType, Type.EmptyTypes);

            ILGenerator getIlCode = getMethodBuilder.GetILGenerator();

            getIlCode.Emit(OpCodes.Ldarg_0);
            getIlCode.Emit(OpCodes.Ldfld, fieldBuilder);
            getIlCode.Emit(OpCodes.Ret);

            return getMethodBuilder;
        }

        private MethodBuilder CreatePropertySetter(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_" + fieldBuilder.Name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null,
                new Type[] {fieldBuilder.FieldType});

            ILGenerator setIlCode = setMethodBuilder.GetILGenerator();

            setIlCode.Emit(OpCodes.Ldarg_0);
            setIlCode.Emit(OpCodes.Ldarg_1);
            setIlCode.Emit(OpCodes.Stfld, fieldBuilder);
            setIlCode.Emit(OpCodes.Ret);

            return setMethodBuilder;
        }

        private object SetProperty(object target, string name, object value, bool ignoreIfTargetIsNull)
        {
            if (ignoreIfTargetIsNull && target == null) return null;

            object[] values = {value};

            object oldProperty = GetProperty(target, name);

            PropertyInfo targetProperty = target.GetType().GetProperty(name);

            if (targetProperty == null)
            {
                throw new System.Exception($"Object {target} does not have Target Property {name}");
            }

            targetProperty.GetSetMethod().Invoke(target, values);

            return oldProperty;
        }

        private object GetProperty(object target, string name)
        {
            PropertyInfo targetProperty = target.GetType().GetProperty(name);

            if (targetProperty == null)
            {
                return null;
            }
            else
            {
                return targetProperty.GetGetMethod().Invoke(target, null);
            }
        }
    }
}
