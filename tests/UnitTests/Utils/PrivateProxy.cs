using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Linq.Expressions;

namespace StarKid.Tests;

public class PrivateProxy<T> : DynamicObject where T : class
{
    private readonly T _obj;
    private readonly Dictionary<string, Delegate> _methods;
    public PrivateProxy(T obj) {
        _obj = obj;
        _methods = new();
    }

    private static Delegate CreateDelegate(MethodInfo methodInfo, object target) {
        var delegateType = AssemblyGen.MakeNewCustomDelegateType(
            methodInfo.ReturnType,
            methodInfo.GetParameters(),
            methodInfo.IsStatic
        );

        return methodInfo.IsStatic
            ? methodInfo.CreateDelegate(delegateType)
            : methodInfo.CreateDelegate(delegateType, target);
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result) {
        var name = binder.Name;
        result = null;

        if (!_methods.TryGetValue(name, out var res)) {
            var method = typeof(T).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method is null)
                return false; // throw new MemberAccessException("Type " + typeof(T).Name + " doesn't contain any method named '" + name + "'");

            res = CreateDelegate(method, _obj);
            _methods.Add(name, res);
        }

        result = res.DynamicInvoke(args);
        return true;
    }

    // from https://source.dot.net/#System.Linq.Expressions/System/Linq/Expressions/Compiler/AssemblyGen.cs
    //    + https://source.dot.net/#System.Linq.Expressions/System/Linq/Expressions/Compiler/DelegateHelpers.cs
    private class AssemblyGen
    {
        private static AssemblyGen? s_assembly;

        private readonly ModuleBuilder _myModule;

        private int _index;

        private static AssemblyGen Assembly {
            get {
                if (s_assembly == null) {
                    Interlocked.CompareExchange(ref s_assembly, new AssemblyGen(), comparand: null);
                }
                return s_assembly;
            }
        }

        private AssemblyGen() {
            var name = new AssemblyName("Proxy");

            var myAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            _myModule = myAssembly.DefineDynamicModule(name.Name!);
        }

        private TypeBuilder DefineType(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, TypeAttributes attr) {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(parent);

            var sb = new System.Text.StringBuilder(name);

            int index = Interlocked.Increment(ref _index);
            sb.Append('$');
            sb.Append(index);

            // An unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
            // is thrown if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
            sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

            name = sb.ToString();

            return _myModule.DefineType(name, attr, parent);
        }

        internal static TypeBuilder DefineDelegateType(string name)
            => Assembly.DefineType(
                    name,
                    typeof(MulticastDelegate),
                    TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass
            );

        internal static Type MakeNewCustomDelegateType(Type returnType, ParameterInfo[] paramInfos, bool isStatic) {
            var builder = DefineDelegateType("Delegate" + Random.Shared.Next());

            const MethodAttributes ctorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
            const MethodImplAttributes implAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            var delegateCtorSignature = new[] { typeof(object), typeof(IntPtr) };

            builder.DefineConstructor(ctorAttributes, CallingConventions.Standard, delegateCtorSignature).SetImplementationFlags(implAttributes);

            const MethodAttributes invokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var callConv = isStatic ? CallingConventions.Standard : CallingConventions.HasThis;

            var parameters = paramInfos.Select(p => p.IsOut && !p.ParameterType.IsByRef ? p.ParameterType.MakePointerType() : p.ParameterType).ToArray();

            builder.DefineMethod("Invoke", invokeAttributes, callConv, returnType, parameters).SetImplementationFlags(implAttributes);

            return builder.CreateType();
        }
    }
}