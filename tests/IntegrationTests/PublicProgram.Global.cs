namespace StarKid.Generated;

internal static partial class StarKidProgram {
    // trick to "intercept" all the System.Environment.Exit() calls
    private static class Environment {
        public static void Exit(int exitCode) => throw new EnvironmentExitException(exitCode);
        public static string? GetEnvironmentVariable(string envvar) => System.Environment.GetEnvironmentVariable(envvar);
    }

    public static void Reset() {
        ResetParser();
        ResetCmdDescs();
    }

#nullable disable
    private static void ResetParser() {
        _currCmdName = "";
        _prevCmdName = "";
        _posArgActions = default;
        _addParams = default;
        _requiredArgsMissing = default;
        _hasParams = default;
        _helpString = default;
        _invokeCmd = default;
        _tryExecFlag = default;
        _tryExecOption = default;
        _tryUpdateCmd = default;
        _displayHelp = false;
        argv = default;
        argvIdx = default;
        posArgIdx = 0;
        _paramsCount = 0;
        Initialize();
    }
#nullable restore

    // todo: try to automate this (and cache as many reflection stuff as possible, if not the whole method)
    static partial void ResetCmdDescs();

    public static int TestMain(params string[] args) => TestMain(args, out _, out _);
    public static int TestMain(string[] args, out string stdout, out string stderr) {
        Reset();

        var oldStdout = Console.Out;
        var oldStderr = Console.Error;
        var stdoutStream = new StringWriter();
        var stderrStream = new StringWriter();

        Console.SetOut(stdoutStream);
        Console.SetError(stderrStream);
        try {
            return Main(args);
        } catch (EnvironmentExitException e) {
            return e.ExitCode;
        } finally {
            Console.SetError(oldStderr);
            Console.SetOut(oldStdout);

            stdout = stdoutStream.ToString();
            stderr = stderrStream.ToString();
            stdoutStream.Dispose();
            stderrStream.Dispose();

            if (stdout.EndsWith('\n'))
                stdout = stdout[..^1];
            if (stderr.EndsWith('\n'))
                stderr = stderr[..^1];
        }
    }
}