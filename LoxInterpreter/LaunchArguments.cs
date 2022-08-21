using System;

namespace CommandLine
{
    [Serializable]
    internal class LaunchArguments
    {
        [Option('i', nameof(FilePath), HelpText = "The source file to load.")]
        public string FilePath { get; set; }
    }
}
