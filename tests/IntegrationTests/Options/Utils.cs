using StarKid.Tests.Options;

namespace StarKid.Tests;

internal static partial class Utils
{
    public static UnixFileMode ParseUnixFileMode(string s) {
        var res = UnixFileMode.None;

        for (int i = 0; i < 9; i++) {
            var expectedChar = (i % 3) switch { 0 => 'r', 1 => 'w', 2 => 'x', _ => default };
            var currentChar = s[i];
            res = (UnixFileMode)((int)res << 1);
            if (currentChar == expectedChar)
                res |= (UnixFileMode)1;
            else if (currentChar != '-')
                throw new Exception("not a valid unix file mode string");
        }

        return res;
    }

    public static object GetHostState()
        => OptionTest.GetState();
}