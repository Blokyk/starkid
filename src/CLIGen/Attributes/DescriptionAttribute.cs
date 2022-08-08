#nullable enable

using System;

namespace CLIGen;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DescriptionAttribute : System.Attribute
{
    public string Desc { get; }
    public DescriptionAttribute(string desc) => Desc = desc;

    public void Deconstruct(
        out string desc
    ) {
        desc = Desc;
    }
}