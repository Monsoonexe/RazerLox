using System;

namespace CommandLine
{
    [Serializable]
    internal class LaunchArguments
    {
        [Option('i', nameof(FilePath), HelpText = "The source file to load.")]
        public string FilePath { get; set; }

        [Option(nameof(NoExit), HelpText = "Should the terminal remain open after a script has been run?")]
        public bool NoExit { get; set; } = false;
    }
}
