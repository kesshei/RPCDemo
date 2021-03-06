using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace RPCDemo
{
    public class DnamicInterfaceProxy
    {
        private static readonly string DllName;
        private static ModuleBuilder ModuleBuilder = null;
        private static AssemblyBuilder AssemblyBuilder = null;

        private static readonly ConcurrentDictionary<Type, Type> Maps = new ConcurrentDictionary<Type, Type>();
        static DnamicInterfaceProxy()
        {
            var assemblyName = new AssemblyName(nameof(DnamicInterfaceProxy));
            DllName = assemblyName.Name + ".dll";
            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(DllName);
        }
        public static T Resolve<T>() where T : class
        {
            var hanlder = new DefaultInvocationHandler<T>();
            var interfaceType = typeof(T);
            if (interfaceType?.IsInterface != true)
            {
                throw new ArgumentException("interfaceType");
            }
            Maps.TryGetValue(interfaceType, out Type newType);
            if (newType == null)
            {
                newType = CreateType(interfaceType);
                Maps.TryAdd(interfaceType, newType);
            }
            return (T)Activator.CreateInstance(newType, hanlder);
        }
        public static void Save()
        {
            AssemblyBuilder.Save(DllName);
        }
        private static Type CreateType(Type interfaceType)
        {
            var tb = ModuleBuilder.DefineType(string.Format("{0}.{1}", typeof(DnamicInterfaceProxy).FullName, interfaceType.Name));
            tb.AddInterfaceImplementation(interfaceType);

            var fb = tb.DefineField("_handler", typeof(InvocationHandler), FieldAttributes.Private);

            CreateConstructor(tb, fb);
            CreateMethods(interfaceType, tb, fb);
            CreateProperties(interfaceType, tb, fb);

            return tb.CreateType();
        }
        private static void CreateConstructor(TypeBuilder tb, FieldBuilder fb)
        {
            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(InvocationHandler) });
            var il = ctor.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fb);
            il.Emit(OpCodes.Ret);
        }

        private static void CreateMethods(Type interfaceType, TypeBuilder tb, FieldBuilder fb)
        {
            foreach (MethodInfo met in interfaceType.GetMethods())
            {
                CreateMethod(met, tb, fb);
            }
        }
        private static MethodBuilder CreateMethod(MethodInfo met, TypeBuilder tb, FieldBuilder fb)
        {
            var args = met.GetParameters();
            var mb = tb.DefineMethod(met.Name, MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
                met.CallingConvention, met.ReturnType, args.Select(t => t.ParameterType).ToArray());
            var il = mb.GetILGenerator();
            il.DeclareLocal(typeof(object[]));

            if (met.ReturnType != typeof(void))
            {
                il.DeclareLocal(met.ReturnType);
            }

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldc_I4, args.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc_0);

            for (int i = 0; i < args.Length; i++)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, 1 + i);
                var type = args[i].ParameterType;
                if (type.IsValueType)
                {
                    il.Emit(OpCodes.Box, type);
                }
                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fb);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, met.MetadataToken);
            il.Emit(OpCodes.Ldstr, met.DeclaringType?.FullName + "+" + met.Name);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, typeof(InvocationHandler).GetMethod(nameof(InvocationHandler.InvokeMember), BindingFlags.Instance | BindingFlags.Public));

            if (met.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(met.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, met.ReturnType);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldloc_1);
            }
            il.Emit(OpCodes.Ret);

            return mb;
        }
        private static void CreateProperties(Type interfaceType, TypeBuilder tb, FieldBuilder fb)
        {
            foreach (var prop in interfaceType.GetProperties())
            {
                var pb = tb.DefineProperty(prop.Name, PropertyAttributes.SpecialName, prop.PropertyType, Type.EmptyTypes);
                var met = prop.GetGetMethod();
                if (met != null)
                {
                    var mb = CreateMethod(met, tb, fb);
                    pb.SetGetMethod(mb);
                }
                met = prop.GetSetMethod();
                if (met != null)
                {
                    var mb = CreateMethod(met, tb, fb);
                    pb.SetSetMethod(mb);
                }
            }
        }
    }
}
