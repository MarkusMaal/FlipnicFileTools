namespace FlipnicLib.Formats;

public class Game : FormatBase
{
    private readonly string? _resourceRoot;
    private readonly string? _streamRoot;
    private readonly string? _tutorialRoot;
    private readonly string? _compiler;
    private readonly string? _fontRoot;
    private readonly string[]? _modules;
    private readonly string? _rom;
    private readonly string? _moduleRoot;
    
    public Game(Stream stream)
    {
        const string waitStr = "Analyzing...";
        Console.Write(waitStr);
        while (stream.Position < stream.Length)
        {
            var testBytes = new byte[8];
            stream.ReadExactly(testBytes, 0, 8);
            var testStr = GetString(testBytes);
            switch (testStr)
            {
                case "cdrom0:":
                {
                    stream.ReadExactly(testBytes, 0, 8);
                    _resourceRoot = GetString(testBytes);
                    stream.Position += 8;
                    stream.ReadExactly(testBytes, 0, 8);
                    _streamRoot = GetString(testBytes);
                    stream.ReadExactly(testBytes, 0, 8);
                    _tutorialRoot = GetString(testBytes);
                    if (_tutorialRoot == "EVTIDX") _tutorialRoot = "N/A";
                    stream.Position += 8;
                    continue;
                }
                case "SLOT03":
                    stream.Position += 0x20;
                    testBytes = new byte[0x18];
                    stream.ReadExactly(testBytes, 0, 0x18);
                    _fontRoot = GetString(testBytes);
                    continue;
                case "*End Of ":
                    stream.Position -= 0x118;
                    testStr = "";
                    List<string> libs = [];
                    while (!testStr.StartsWith("cdrom0:\\"))
                    {
                        if (testStr != "") libs.Add(testStr);
                        testBytes = new byte[0x10];
                        stream.ReadExactly(testBytes, 0, 0x10);
                        testStr = GetString(testBytes);
                    }
                    _moduleRoot = testStr;
                    _modules = libs.ToArray();
                    stream.Position += 0x10;
                    stream.ReadExactly(testBytes, 0, 0x10);
                    _rom =  GetString(testBytes);
                    stream.Position += 0xB0;
                    continue;
            }

            if (stream.Position == stream.Length - 0x198)
            {
                stream.Position += 21L;
                testBytes = new byte[0x30];
                stream.ReadExactly(testBytes, 0, 0x30);
                _compiler =  GetString(testBytes);
                break;
            }
            stream.Position += 0x8;
        }
        Console.Write("\r");
        Console.Write("".PadLeft(waitStr.Length));
        Console.Write("\r");
    }

    public override string? ToString()
    {
        var o = $"""
               Flipnic Game Executable
               
               Resource container: {_resourceRoot}
               Stream container: {_streamRoot}
               Tutorial streams container: {_tutorialRoot}
               Font container: {_fontRoot}
               
               Compiler: {_compiler}
               
               IOP ROM: {_rom}
               Modules root: {_moduleRoot}
               
               Modules:
               """;
        return _modules?.Aggregate(o, (current, module) => current + $"\n - {module}");
    }
}