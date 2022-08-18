namespace Recline.Generator;

internal static class AttributeParser
{
    public static bool TryParseCmdAttrib(AttributeData attr, out CommandAttribute cmdAttr) {
        cmdAttr = null!;

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (!Utils.Equals(attr.ConstructorArguments[0].Type, Utils.STR))
            return false;

        cmdAttr = new((string)attr.ConstructorArguments[0].Value!);
        return true;
    }

    public static bool TryParseSubCmdAttrib(AttributeData attr, out SubCommandAttribute subCmdAttr) {
        subCmdAttr = null!;

        if (attr.ConstructorArguments.Length < 2)
            return false;
        if (attr.NamedArguments.Length > 1)
            return false;

        if (!Utils.Equals(attr.ConstructorArguments[0].Type, Utils.STR))
            return false;
        var cmdName = (string)attr.ConstructorArguments[0].Value!;

        if (!Utils.Equals(attr.ConstructorArguments[1].Type, Utils.STR))
            return false;
        var parentName = (string)attr.ConstructorArguments[1].Value!;

        bool inheritOptions = false;

        if (attr.NamedArguments.Length != 0) {
            var inheritOptionsArgPair = attr.NamedArguments.First();

            if (!inheritOptionsArgPair.Equals(default)) {
                if (!Utils.Equals(inheritOptionsArgPair.Value.Type, Utils.BOOL))
                    return false;

                inheritOptions = (bool)inheritOptionsArgPair.Value.Value!;
            }
        }

        subCmdAttr = new(cmdName, parentName) { InheritOptions = inheritOptions };

        return true;
    }

    public static bool TryParseOptAttrib(AttributeData attr, out OptionAttribute optAttr) {
        optAttr = null!;

        string longName;
        char shortName = '\0';

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (!Utils.Equals(attr.ConstructorArguments[0].Type, Utils.STR))
            return false;

        longName = (string)attr.ConstructorArguments[0].Value!;

        if (attr.ConstructorArguments.Length == 2) {
            if (!Utils.Equals(attr.ConstructorArguments[1].Type, Utils.CHAR))
                return false;

            shortName = (char)attr.ConstructorArguments[1].Value!;
        }

        string? argName = null;

        var argNameArg = attr.NamedArguments.FirstOrDefault(kv => kv.Key == nameof(OptionAttribute.ArgName));

        if (!argNameArg.Equals(default))
            argName = (string)argNameArg.Value.Value!;

        optAttr = new OptionAttribute(
            longName,
            shortName
        ) {
            ArgName = argName
        };

        return true;
    }

    public static bool TryParseDescAttrib(AttributeData attr, out DescriptionAttribute descAttr) {
        descAttr = null!;

        if (attr.ConstructorArguments.Length < 1)
            return false;

        if (!Utils.Equals(attr.ConstructorArguments[0].Type, Utils.STR))
            return false;

        descAttr = new((string)attr.ConstructorArguments[0].Value!);

        return true;
    }

    public static bool TryParseCLIAttrib(AttributeData attr, out CLIAttribute cliAttr) {
        cliAttr = null!;

        // appName
        if (!TryGetCtorArg<string>(attr, 0, Utils.STR, out var appName))
            return false;

        // EntryPoint
        if (!TryGetProp<string?>(attr, nameof(CLIAttribute.EntryPoint), Utils.STR, null, out var entryPoint))
            return false;

        if (!TryGetProp<int>(attr, nameof(CLIAttribute.HelpExitCode), Utils.INT32, 0, out var helpIsError))
            return false;

        cliAttr = new(appName) {
            EntryPoint = entryPoint,
            HelpExitCode = helpIsError
        };

        return true;
    }

    public static bool TryGetCtorArg<T>(AttributeData attrib, int ctorIdx, INamedTypeSymbol type, out T val) {
        val = default!;

        var ctorArgs = attrib.ConstructorArguments;

        if (ctorArgs.Length < ctorIdx + 1) {
            return false;
        }

        if (!Utils.Equals(ctorArgs[ctorIdx].Type, type) || ctorArgs[ctorIdx].Value is not T)
            return false;

        val = (T)ctorArgs[ctorIdx].Value!;

        return true;
    }

    public static bool TryGetProp<T>(AttributeData attrib, string propName, INamedTypeSymbol type, T defaultVal, out T val) {
        val = defaultVal;

        var namedArgs = attrib.NamedArguments;

        if (namedArgs.IsDefaultOrEmpty)
            return true;

        var arg = namedArgs.FirstOrDefault(
            kv => kv.Key == propName
        ).Value;

        if (arg.Equals(default))
            return true;

        if (!Utils.Equals(arg.Type, type) || arg.Value is not T)
            return false;

        val = (T)arg.Value!;

        return true;
    }

}