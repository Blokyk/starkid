#nullable enable

using System;
using System.IO;

namespace Recline.Parsing
{
    public static class Validators
    {
        // TODO: add name of parsers + implement them
        private const string _progName = "Recline.Generated.ReclineProgram.";

        public const string FileExists = _progName + "FileExists";
        public const string DirectoryExists = _progName + "DirectoryExists";
    }
}

namespace Recline.Generated
{
    internal static partial class ReclineProgram
    {
        public static string? FileExists(FileInfo file) {
            if (!file.Exists) {
                return $"File {file.FullName} doesn't exist.";
            }

            return null;
        }

        public static string? DirectoryExists(DirectoryInfo dir) {
            if (!dir.Exists) {
                return $"File {dir.FullName} doesn't exist.";
            }

            return null;
        }
    }
}