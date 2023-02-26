using System.IO;

namespace Recline.Generator;

internal static class Resources {
    public static int MAX_LINE_LENGTH = 80;

    public const string CmdGroupAttribName = nameof(Recline.CommandGroupAttribute);
    public const string CmdAttribName = nameof(Recline.CommandAttribute);
    public const string OptAttribName = nameof(Recline.OptionAttribute);
    public const string ParseWithAttribName = nameof(Recline.ParseWithAttribute);
    public const string ValidateWithAttribName = nameof(Recline.ValidateWithAttribute);

    public const string GenNamespace = "Recline.Generated";

    public const string GenFileHeader = $@"
#nullable enable
using System;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace {GenNamespace};
";

    public const string ProgClassName = "ReclineProgram";
}