using CommandLine;
using compile.Config;

namespace compile
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CompileOptions>(args)
                .WithParsed(PerformCompileWithOptions)
                .WithNotParsed(InformArgumentParseFailure);
        }

        private static void PerformCompileWithOptions(CompileOptions options)
        {
            List<string> includes = options.IncludeDirectories.ToList();

            Preprosessor preprosessor = new Preprosessor();
            Scanner scanner = new Scanner();
            Parser parser = new Parser();

            foreach (string file in options.InputFiles)
            {
                preprosessor.Execute(file, includes);
                scanner.Execute(preprosessor.RetrieveAsString());
                parser.Execute(scanner.RetrieveTokens());
            }
        }

        private static void InformArgumentParseFailure(IEnumerable<Error> errors)
        {
         
        }
    }
}
