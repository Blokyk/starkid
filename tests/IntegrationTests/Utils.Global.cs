using StarKid.Generated;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace StarKid.Tests;

internal static partial class Utils {
    internal static readonly object DefaultHostState;
    private static readonly IDictionary<string, object> _defaultHostStateDict;
    private static readonly Func<object?, object?, bool, EquivalentException> _equivalent;
    static Utils() {
        StarKidProgram.Reset();
        DefaultHostState = GetHostState();
        _defaultHostStateDict = DefaultHostState.AsMemberDictionary();

        var asm = typeof(Assert).Assembly;
        var helperType = asm.GetType("Xunit.Internal.AssertHelper", throwOnError: true)!;

        _equivalent = ((MethodInfo)helperType.GetMember("VerifyEquivalence").First()).CreateDelegate<Func<object?, object?, bool, EquivalentException>>();
    }

    public static bool AreEquivalent(object expected, object actual, bool strict = false)
        => _equivalent(expected, actual, strict) is null;

    private static IEnumerable<MemberInfo> GetFieldsAndProps(object obj)
        => obj
            .GetType()
            .GetMembers()
            .Where(m => m is FieldInfo { IsSpecialName: false } or PropertyInfo);

    public static Dictionary<string, object> AsMemberDictionary(this object obj)
        => GetFieldsAndProps(obj).ToDictionary(
                m => m.Name,
                m => m switch {
                    FieldInfo f => f.GetValue(obj),
                    PropertyInfo p => p.GetValue(obj),
                    _ => null
                }
            )!;

    public static object With(this object target, object modifiedMembers) {
        var targetType = target.GetType();

        if (!isAnonymous(targetType) || !isAnonymous(modifiedMembers.GetType()))
            throw new InvalidOperationException("The With() method is meant to be used with anonymous objects only.");

        // anon types only have one ctor, with every property in order
        var ctor = targetType.GetConstructors().First();

        var propInfos = GetFieldsAndProps(target).Cast<PropertyInfo>().ToArray();
        var modifiedValues = AsMemberDictionary(modifiedMembers);

        var changedValues = 0;

        var newValues = propInfos.Select(prop => {
            if (!modifiedValues.TryGetValue(prop.Name, out var val)) {
                return prop.GetValue(target);
            }

            changedValues++;

            var valType = val.GetType();
            var propType = withoutNullable(prop.PropertyType);

            if (isAnonymous(propType) && isAnonymous(valType))
                return prop.GetValue(target)?.With(val) ?? val;

            if (propType == valType)
                return val;

            throw new InvalidOperationException($"Type mismatch for property {prop.Name}: trying to assign value of type {valType} to {propType}.");
        }).ToArray();

        if (modifiedValues.Count != changedValues) {
            var nonExistantNames = modifiedValues.Keys.Except(propInfos.Select(p => p.Name));

            throw new InvalidOperationException(
                $"Tried to change non-existant properties of anonymous object. "+
                $"Changed {changedValues} values, but expected to modify {modifiedValues.Count}! "+
                $"Leftovers: {String.Join(", ", nonExistantNames)}"
            );
        }

        return ctor.Invoke(newValues);

        // i know this not actually correct but... i'm lazy
        static bool isAnonymous(Type t) => t.Name.Contains("AnonymousType");

        static Type withoutNullable(Type t)
            =>  t.Name.StartsWith("Nullable`1")
                    ? withoutNullable(t.GenericTypeArguments[0])
                    : t;
    }

    public static Dictionary<string, object> GetHostDiff() {
        var dict = new Dictionary<string, object>();
        var state = GetHostState().AsMemberDictionary();

        foreach (var (prop, value) in state) {
            if (!AreEquivalent(_defaultHostStateDict[prop], value))
                dict.Add(prop, value);
        }

        return dict;
    }
}