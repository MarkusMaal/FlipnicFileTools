using FlipnicLib;
using FlipnicLib.Formats;
using System.Text;

namespace FlipnicFileTool.Tools;

public class SstTools
{
    public SstTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ListResources:
                Console.Write(OpenSst(cfg).GenerateMagicNumbers());
                break;
            case Enums.Modes.ShowSstToc:
                Console.Write(OpenSst(cfg).ListEntries());
                break;
            case Enums.Modes.ShowPseudoCode:
                Console.Write(OpenSst(cfg).GeneratePseudoCode());
                break;
            case Enums.Modes.ShowGimmick:
                OpenSst(cfg).ShowGimmick(cfg.SecondaryFileName);
                break;
            case Enums.Modes.ShowCameras:
                Console.Write(OpenSst(cfg).GetCamData(StaticUtils.SimpleOutput));
                break;
            case Enums.Modes.SstResize:
                ResizeToc(File.Open(cfg.FileName, FileMode.Open, FileAccess.ReadWrite), cfg.VFile, cfg.Count);
                break;
            case Enums.Modes.ShowSstMissions:
                Console.Write(OpenSst(cfg).GetEvtInf());
                break;
            case Enums.Modes.ShowSstRespawns:
                Console.Write(OpenSst(cfg).GetRespawns());
                break;
            case Enums.Modes.ShowDrawDistance:
                Console.Write(OpenSst(cfg).GetDrawDistance());
                break;
        }
    }

    private static FpnSst OpenSst(Config cfg) => new(File.OpenRead(cfg.FileName));

    private void ResizeToc(Stream stream, string label, int newSize)
    {
        Console.WriteLine("NOTE: This tool does not resize the actual data, it only updates TOC entries");
        stream.Position = 8;
        var buffer = new byte[4];
        stream.ReadExactly(buffer, 0, buffer.Length);
        var entryCount = BitConverter.ToInt32(buffer);
        stream.Position = 0x10;
        var delta = 0;
        var updateStart = entryCount;
        Console.WriteLine("Searching for resizable TOC entry");
        for (var i = 0; i < entryCount; i++)
        {
            buffer = new byte[8];
            stream.ReadExactly(buffer, 0, buffer.Length);
            var readLabel = Encoding.ASCII.GetString(buffer).Replace("\0", "");
            if (readLabel != label)
            {
                stream.Position += 8;
                continue;
            }
            Console.WriteLine($"Found at offset 0x{stream.Position - 8:X}");
            buffer = new byte[2];
            stream.ReadExactly(buffer, 0, buffer.Length);
            var oldCount = BitConverter.ToInt16(buffer);
            Console.WriteLine($"Change count from {oldCount} to {newSize}");
            buffer = new byte[2];
            stream.ReadExactly(buffer, 0, buffer.Length);
            var entrySize = BitConverter.ToInt16(buffer);
            delta = newSize * entrySize - oldCount * entrySize;
            Console.WriteLine($"\tSize delta: {delta} bytes");
            updateStart = i;
            stream.Position -= 4;
            stream.Write(BitConverter.GetBytes((short)newSize));
            stream.Position += 6;
            break;
        }
        for (var i = updateStart; i < entryCount; i++)
        {
            stream.Position += 0xC;
            buffer = new byte[4];
            stream.ReadExactly(buffer, 0, buffer.Length);
            var oldOffset = BitConverter.ToInt32(buffer);
            stream.Position -= 4;
            if (oldOffset != 0)
            {
                Console.WriteLine($"Change offset from 0x{oldOffset:X} to 0x{oldOffset + delta:X} @ 0x{stream.Position:X}");
                stream.Write(BitConverter.GetBytes(oldOffset + delta));
            }
        }
        Console.WriteLine("Finished!");
    }
}