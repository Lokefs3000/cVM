using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compile.Config
{
    internal class CompileOptions
    {
        [Option('f', "file", HelpText = "Add a file to be compiled.",Required = true)]
        public IEnumerable<string> InputFiles { get; set; } = new List<string>();

        [Option('o', "out", HelpText = "Output file folder.", Required = true)]
        public string OutputDirectory { get; set; } = "";

        [Option('t', "type", HelpText = "Compile result type.")]
        public CompileMode Mode { get; set; } = CompileMode.Executable;

        [Option('i', "include", HelpText = "Specify an include directory.")]
        public IEnumerable<string> IncludeDirectories { get; set; } = new List<string>();

        [Option('l', "link", HelpText = "Specify a library to link with.")]
        public IEnumerable<string> LibraryFiles { get; set; } = new List<string>();
    }

    public enum CompileMode
    {
        Executable,
        Library
    }
}
