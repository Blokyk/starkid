#nullable enable

using System;

namespace Recline.Parsing
{
    /// <summary>
    /// A collection of names for built-in parsers
    /// </summary>
    internal static class Parsers
    {
        // TODO: add name of parsers + implement them
        private const string _progName = "Recline.Generated.ReclineProgram.";

        /*
        * The generator will check the symbol the name points to and, if it's a type, will
        * add "new " in front of the call.
        *
        * In any case, it will wrap the call in a try-catch block capturing `FormatException`s
        * and printing a message when an error occurred
        */

        // public const string FileInfo = "System.IO.FileInfo";
        // public const string DirectoryInfo = "System.IO.DirectoryInfo";
        // public const string Uri = "System.Uri";
        // public const string Int64 = "System.Int64.Parse";
        // public const string Int32 = "System.Int32.Parse";
        // public const string Int16 = "System.Int16.Parse";
        // public const string Byte = "System.Byte.Parse";
        // public const string Char = "System.Char.Parse";
        // public const string Float = "System.Single.Parse";
        // public const string Double = "System.Double.Parse";
    }
}

namespace Recline.Generated
{
    internal static partial class ReclineProgram
    {

    }
}