using FlipnicLib;

namespace FlipnicScriptCompiler;

public class Help
{
    public static void ShowHelp(string arg0)
    {
        List < Dictionary<string, string> > helpTopics = [
            new(){
                {"--help", "Display this help message"},
            },
            new(){
                {"--decompile", "Decompile SST file"},
                {"--compile", "Generate SST file from source"},
            },
        ];
        StaticUtils.DecodeColors($"~-AUsage~--: {arg0} [mode] [input] [output]");
        Console.WriteLine();
        foreach (var topic in helpTopics)
        {
            Console.WriteLine();
            foreach (var (key, value) in topic)
            {
                StaticUtils.DecodeColors($"~-8{key,-25}~--{value}");
                Console.WriteLine();
            }
        }
    }
}