


#nullable enable
using System;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using CLIGen.Generated;


namespace CLIGen.Generated;

using System.IO;
static partial class CLIGenProgram {

    private abstract partial class CmdDesc {
        private static readonly Lazy<CmdDesc> _lazyRoot = new(static () => new parsexCmdDesc(), false);
        internal static CmdDesc root => _lazyRoot.Value;


private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

{ "--force", set_force },{ "-f", set_force },
};
private static void set_force(string? arg) => global::SomeStuff.Parsex.forceOption = AsBool(arg, !false);
private static Dictionary<string, Action<string>> _options = new() {
{ "--output", outputAction },
{ "--range", rangeAction },{ "-r", rangeAction },
};
private static void outputAction(string? arg) => global::SomeStuff.Parsex.OutputFile = Parse<FileInfo>(arg ?? "");
private static void rangeAction(string? arg) => ThrowIfNotValid(global::SomeStuff.Parsex.ParseRange(arg));

    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class parsexCmdDesc : CmdDesc {

        

        internal parsexCmdDesc() : base(_switches, _options) {}

        protected parsexCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

{ "--force", set_force },{ "-f", set_force },
};
private static void set_force(string? arg) => global::SomeStuff.Parsex.forceOption = AsBool(arg, !false);
private static Dictionary<string, Action<string>> _options = new() {
{ "--output", outputAction },
{ "--range", rangeAction },{ "-r", rangeAction },
};
private static void outputAction(string? arg) => global::SomeStuff.Parsex.OutputFile = Parse<FileInfo>(arg ?? "");
private static void rangeAction(string? arg) => ThrowIfNotValid(global::SomeStuff.Parsex.ParseRange(arg));
protected override Action<string>[] _posArgs => Array.Empty<Action<string>>();
private static Action _func = global::SomeStuff.Parsex.Silent;
internal override Func<int> Invoke => () => { _func(); return 0; };

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class silentCmdDesc : CmdDesc {

        

        internal silentCmdDesc() : base(_switches, _options) {}

        protected silentCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

};
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => Array.Empty<Action<string>>();
private static Action _func = global::SomeStuff.Parsex.Silent;
internal override Func<int> Invoke => () => { _func(); return 0; };

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class countCmdDesc : CmdDesc {

        protected override bool HasParams => true;

        internal countCmdDesc() : base(_switches, _options) {}

        protected countCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

};
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => new Action<string>[] {
static arg => anotherOne = Parse<String>(arg),
};
private static String anotherOne;
private static Func<String, String[], int> _func = global::SomeStuff.Parsex.CheckAll;
internal override Func<int> Invoke => () => _func(anotherOne, _params.ToArray());

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class printCmdDesc : CmdDesc {

        

        internal printCmdDesc() : base(_switches, _options) {}

        protected printCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

};
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => new Action<string>[] {
static arg => file = Parse<FileInfo>(arg),
};
private static FileInfo file;
private static Func<FileInfo, int> _func = global::SomeStuff.Parsex.Print;
internal override Func<int> Invoke => () => _func(file);

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class hashCmdDesc : CmdDesc {

        

        internal hashCmdDesc() : base(_switches, _options) {}

        protected hashCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

};
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => new Action<string>[] {
static arg => file = Parse<FileInfo?>(arg),
};
private static FileInfo? file = null;
private static Func<FileInfo?, int> _func = global::SomeStuff.Parsex.Hash;
internal override Func<int> Invoke => () => _func(file);

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class graphCmdDesc : CmdDesc {

        

        internal graphCmdDesc() : base(_switches, _options) {}

        protected graphCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

{ "--const", set_const },{ "-c", set_const },
};
private static void set_const(string? arg) => constOption = AsBool(arg, true);
private static bool constOption;
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => Array.Empty<Action<string>>();
private static Func<Boolean, int> _func = global::SomeStuff.Parsex.Graph;
internal override Func<int> Invoke => () => _func(constOption);

    }



#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class constCmdDesc : graphCmdDesc {

        

        internal constCmdDesc() : base(_switches, _options) {}

        protected constCmdDesc(
            Dictionary<string, Action<string?>> switches,
            Dictionary<string, Action<string>> options
        )
            : base(_switches.UpdatedWith(switches), _options.UpdatedWith(options))
        {}
private static Dictionary<string, Action<string?>> _switches = new() {

            { "--help", DisplayHelp },
            { "-h", DisplayHelp },

{ "--range", set_range },{ "-r", set_range },
};
private static void set_range(string? arg) => range = AsBool(arg, true);
private static bool range;
private static Dictionary<string, Action<string>> _options = new() {
};
protected override Action<string>[] _posArgs => Array.Empty<Action<string>>();
private static Func<Boolean, int> _func = global::SomeStuff.Parsex.GraphConst;
internal override Func<int> Invoke => () => _func(range);

    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class parsexCmdDesc : CmdDesc {


        internal override Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {

{ "silent", static () => new silentCmdDesc() },
{ "count", static () => new countCmdDesc() },
{ "print", static () => new printCmdDesc() },
{ "hash", static () => new hashCmdDesc() },
{ "graph", static () => new graphCmdDesc() },
};
}

#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class graphCmdDesc : CmdDesc {


        internal override Dictionary<string, Func<CmdDesc>> SubCmds => _subs;
        private static Dictionary<string, Func<CmdDesc>> _subs = new() {

{ "const", static () => new constCmdDesc() },
};
}


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class silentCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Description:\n  Don't print anything to stdout (errors go to stderr)\n\nUsage:\n  silent\n  \n\nOptions:\n  -h, --help  Print this help message\n\n\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class countCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Usage:\n  count <anotherOne> <files>...\n  \n\nOptions:\n  -h, --help  Print this help message\n\nArguments:\n  anotherOne\n  files       The list of files to count\n\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class printCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Usage:\n  print <file>\n  \n\nOptions:\n  -h, --help  Print this help message\n\nArguments:\n  file  fileDesc\n\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class hashCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Description:\n  Print the hash of the AST graph\n\nUsage:\n  hash <file>\n  \n\nOptions:\n  -h, --help  Print this help message\n\nArguments:\n  file  fileDesc\n\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class graphCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Usage:\n  graph [options]\n  \n  graph [options] [const]\n\nOptions:\n  -c, --const\n  -h, --help   Print this help message\n\n\nCommands:\n  const\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class constCmdDesc : graphCmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Usage:\n  graph const [options]\n  \n\nOptions:\n  -r, --range\n  -h, --help   Print this help message\n\n\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }


#pragma warning disable CS8618
#pragma warning disable CS8625
    private partial class parsexCmdDesc : CmdDesc {

        internal override string HelpString => _helpString;
        private static readonly string _helpString = "Description:\n  A parser/typechecker for lotus\n\nUsage:\n  parsex [options]\n  parsex [options] <command>\n\nOptions:\n  -f, --force          Ignore parsing/compilation errors before executing \n                       commands \n      --output <file>  The file to output stuff to, instead of stdin\n  -r, --range <range>\n  -h, --help           Print this help message\n\n\nCommands:\n  silent                      Don't print anything to stdout (errors go to \n                              stderr) \n  count <anotherOne> <files>\n  print <file>\n  hash <file>                 Print the hash of the AST graph\n  graph\n";

        private static void DisplayHelp(string? val) {
            Console.Error.WriteLine(_helpString);
            System.Environment.Exit(0);
        }
    }
}
// Analysis took 223ms
// Generation took 46ms