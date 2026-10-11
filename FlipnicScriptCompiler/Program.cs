using FlipnicLib.Formats;

namespace FlipnicScriptCompiler;

internal abstract class Program
{
    private static bool _simple;
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            FlipnicLib.StaticUtils.DecodeColors("~-CError~--: No arguments provided. Please add --help param to check usage.");
            Console.WriteLine();
            return 1;
        }

        if (args.Any(p => p == "--simple"))
        {
            _simple = true;
        }

        if (!_simple)
        {
            FlipnicLib.StaticUtils.DecodeColors("~-BFlipnic Script Compiler~--");
            Console.WriteLine();
        }

        if (args.Any(p => p == "--help"))
        {
            Help.ShowHelp(Path.GetFileName(Environment.ProcessPath ?? "./FlipnicScriptCompiler"));
            return 0;
        }

        if (!File.Exists(args[1]))
        {
            FlipnicLib.StaticUtils.DecodeColors("~-CError~--: Input file does not exist.");
            Console.WriteLine();
            return 2;
        }

        switch (args[0])
        {
            case "--decompile":
                var dCmp = new Decompiler(new FpnSst(File.OpenRead(args[1])));
                var fs = new FileStream(args[2], FileMode.Create, FileAccess.Write);
                dCmp.DecompileAsStream(fs);
                fs.Close();
                FlipnicLib.StaticUtils.DecodeColors("~-ASuccess~--: File saved");
                break;
        }
        return 0;
    }
}