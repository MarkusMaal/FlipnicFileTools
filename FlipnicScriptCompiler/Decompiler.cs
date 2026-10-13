using System.Text;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicScriptCompiler;

public class Decompiler(FpnSst sst)
{
    
    public void DecompileAsStream(Stream output)
    {
        foreach (var key in sst.TableOfContents.Keys)
        {
            var section = sst.GetSectionEntries(key);
            output.Write(Encoding.UTF8.GetBytes("#" + key + "\n"));
            if (key.StartsWith("GMK"))
            {
                foreach (var gmk in section.Select(s => new Gimmick(s)))
                {
                    output.Write(Encoding.UTF8.GetBytes($"{gmk}\n"));
                }
            }
            switch (key)
            {
                case "SKYN":
                case "CAMN":
                case "LIGHTN":
                case "PATHN":
                case "KUIDX":
                case "BOSSN":
                case "STGNAME":
                    foreach (var s in section.Select(GetString))
                        output.Write(Encoding.UTF8.GetBytes(s + "\n"));
                    break;
                case "BALLN":
                case "VABN":
                case "INTN":
                    foreach (var s in section)
                    {
                        var s1 = GetString([.. s.Take(0x20)]);
                        var s2 = GetString([.. s.Skip(0x20)]);
                        output.Write(key == "VABN"
                            ? Encoding.UTF8.GetBytes(s1 + "," + s2 + "," +
                                                     BitConverter.ToInt32([.. s.Skip(0x40).Take(0x4)]) + "\n")
                            : Encoding.UTF8.GetBytes(s1 + "," + s2 + "\n"));
                    }
                    break;
                case "SEQN":
                case "EFFN":
                case "FNTN":
                case "STGINF":
                    foreach (var s in section)
                    {
                        var s1 = GetString([.. s.Take(0x20)]);
                        long idx;
                        if (s.Length == 0x24)
                        {
                            idx = BitConverter.ToInt32([.. s.Skip(0x20)]);
                        }
                        else
                        {
                            idx = BitConverter.ToInt64([.. s.Skip(0x20)]);
                        }
                        output.Write(Encoding.UTF8.GetBytes(s1 + "," + idx + (s.Length != 0x24 ? "L" : "") + "\n"));
                    }
                    break;
                case "BALLD":
                    foreach (var s in section)
                    {
                        output.Write(Encoding.UTF8.GetBytes(BitConverter.ToSingle([.. s.Take(4)]) + "\n"));
                    }
                    break;
                case "LNEWARP":
                case "KUOFS":
                    foreach (var s in section)
                    {
                        output.Write(Encoding.UTF8.GetBytes(BitConverter.ToInt16([.. s.Take(2)]) + "," +
                                                            BitConverter.ToInt16([.. s.Skip(2).Take(2)]) + ","+
                                                            BitConverter.ToInt16([.. s.Skip(4).Take(2)]) + "," +
                                                            BitConverter.ToInt16([.. s.Skip(6).Take(2)]) + "\n"));
                    }
                    break;
                case "KUINF":
                    foreach (var s in section)
                    {
                        for (var i = 0; i < 0x10; i++)
                        {
                            output.Write(Encoding.UTF8.GetBytes(BitConverter.ToInt16([..s.Skip(i * 2).Take(2)]) + ","));
                        }

                        for (var i = 0; i < 0x4; i++)
                        {
                            output.Write(
                                Encoding.UTF8.GetBytes(BitConverter.ToSingle([.. s.Skip(0x20 + (i * 4)).Take(4)]) + ","));
                        }

                        for (var i = 0; i < 0x6; i++)
                        {
                            output.Write(
                                Encoding.UTF8.GetBytes(BitConverter.ToInt16([.. s.Skip(0x30 + (i * 2)).Take(2)]) + ","));
                        }

                        List<string> byteStrs = [];
                        for (var i = 0; i < 0x14; i++)
                        {
                            byteStrs.Add(s[0x3C + i].ToString());
                        }
                        output.Write(Encoding.UTF8.GetBytes(string.Join(',', byteStrs)));
                        output.Write("\n"u8);
                    }
                    break;
                case "IPUN":
                    foreach (var s in section)
                    {
                        var fmvName = GetString([.. s.Take(0x20)]);
                        var overlay = GetString([.. s.Skip(0x20).Take(0x20)]);
                        var type = BitConverter.ToInt32([.. s.Skip(0x40).Take(4)]);
                        var typeS = type switch
                        {
                            2 => "FmvType::HiRes",
                            3 => "FmvType::LoRes",
                            _ => type.ToString()
                        };
                        var fadeIn = BitConverter.ToInt32([.. s.Skip(0x44).Take(4)]);
                        var whiteIn = BitConverter.ToInt32([.. s.Skip(0x48).Take(4)]) == 1 ? "FadeIn::White" : "FadeIn::Black";
                        var fadeOut = BitConverter.ToInt32([.. s.Skip(0x4C).Take(4)]);
                        var whiteOut = BitConverter.ToInt32([.. s.Skip(0x50).Take(4)]) == 1 ? "FadeOut::White" : "FadeOut::Black";
                        output.Write(Encoding.UTF8.GetBytes($"{fmvName},{overlay},{typeS},{fadeIn},{whiteIn},{fadeOut},{whiteOut}\n"));
                    }
                    break;
                case "CAMD":
                    foreach (var s in section)
                    {
                        var camD = new CamData(s, sst);
                        var line = camD.CameraId.ToString();
                        line += "," + string.Join(",", camD.UnkData);
                        line += "," + (camD.AnchorToTarget ? "Anchor" : "NoAnchor");
                        line += "," + camD.GetAxisString();
                        line += "," + camD.CameraName;
                        line += "," + camD.UnkInt;
                        line += "," + camD.GetStiffnessXyz();
                        output.Write(Encoding.UTF8.GetBytes(line + "\n"));
                    }
                    break;
                case "CAMLD":
                    foreach (var s in section)
                    {
                        var camera = BitConverter.ToInt32([.. s.Take(4)]);
                        var l1 = BitConverter.ToInt32([.. s.Skip(4).Take(4)]);
                        var l2 = BitConverter.ToInt32([.. s.Skip(8).Take(4)]);
                        var s1 = BitConverter.ToSingle([.. s.Skip(0xC).Take(4)]);
                        var s2 = BitConverter.ToSingle([.. s.Skip(0x10).Take(4)]);
                        var u1 = BitConverter.ToInt32([.. s.Skip(0x14).Take(4)]);
                        var u2 = BitConverter.ToInt32([.. s.Skip(0x18).Take(4)]);
                        var u3 = BitConverter.ToInt32([.. s.Skip(0x1C).Take(4)]);
                        var u4 = BitConverter.ToInt32([.. s.Skip(0x20).Take(4)]);
                        var u5 = BitConverter.ToInt32([.. s.Skip(0x24).Take(4)]);
                        output.Write(Encoding.UTF8.GetBytes($"{camera},{l1},{l2},{s1},{s2},{u1},{u2},{u3},{u4},{u5}\n"));
                    }
                    break;
                case "CAMKUD":
                    foreach (var s in section)
                    {
                        var drawDx = BitConverter.ToSingle([.. s.Take(4)]);
                        var drawDz = BitConverter.ToSingle([.. s.Skip(4).Take(4)]);
                        var camIdx = s[0x8];
                        output.Write(Encoding.UTF8.GetBytes($"{drawDx},{drawDz},{camIdx},"));
                        byte b = 0x00;
                        var i = 0x9;
                        List<byte> areaBytes = [];
                        while (b != 0xFF && i < section.Count)
                        {
                            areaBytes.Add(b);
                            b = s[i];
                            i++;
                        }
                        output.Write(Encoding.UTF8.GetBytes($"{string.Join(',', areaBytes)}\n"));
                    }
                    break;
                case "REBIRTH":
                    foreach (var s in section)
                        output.Write(Encoding.UTF8.GetBytes($"{BitConverter.ToInt16([.. s.Take(2)])},{BitConverter.ToInt16([.. s.Skip(2).Take(2)])},{BitConverter.ToInt16([.. s.Skip(4).Take(2)])}\n"));
                    break;
                case "SGKTBL":
                    foreach (var s in section)
                        output.Write(Encoding.UTF8.GetBytes($"{BitConverter.ToInt16([.. s.Take(2)])},{BitConverter.ToInt16([.. s.Skip(2).Take(2)])}\n"));
                    break;
                case "SETBL":
                    foreach (var s in section)
                    {
                        for (var i = 0; i < 8; i++)
                        {
                            output.Write(Encoding.UTF8.GetBytes($"{s[i]},"));
                        }
                        output.Write(Encoding.UTF8.GetBytes($"{s[8]},{s[0xC]}\n"));
                    }
                    break;
                case "MTTBL":
                case "PTTBL":
                case "MTTBLDEF":
                case "EVTBL":
                    foreach (var s in section)
                    {
                        output.Write(Encoding.UTF8.GetBytes($"{string.Join(',', s)}\n"));
                    }
                    break;
                case "DRAWD":
                    foreach (var s in section)
                    {
                        output.Write(Encoding.UTF8.GetBytes($"{BitConverter.ToSingle([.. s.Take(4)])},{s[0x5]}\n"));
                    }
                    break;
                case "EVTIDX":
                case "SGKIDX":
                    foreach (var s in section)
                        output.Write(Encoding.UTF8.GetBytes($"{BitConverter.ToInt32([.. s.Take(4)])}\n"));
                    break;
                case "EVENT":
                    foreach (var s in section)
                        output.Write(Encoding.UTF8.GetBytes($"{DecompileEvent(new Event(s), sst)}"));
                    break;
                case "FLIESFPB":
                    foreach (var s in section)
                        output.Write(Encoding.UTF8.GetBytes($"base_64({Convert.ToBase64String(s)})\n"));
                    break;
                case "EVTINF":
                    foreach (var s in section)
                    {
                        var stgIdx = BitConverter.ToInt32([.. s.Take(4)]);
                        var msgIdx = BitConverter.ToInt32([.. s.Skip(8).Take(4)]);
                        output.Write(Encoding.UTF8.GetBytes($"{stgIdx},{s[4]},{s[6]},{s[7]},{msgIdx}\n"));
                    }
                    break;
                case "RECORD":
                    foreach (var s in section)
                    {
                        var score = BitConverter.ToInt64([.. s.Take(8)]);
                        var diff = BitConverter.ToInt32([.. s.Skip(8).Take(4)]);
                        var combos = BitConverter.ToInt32([.. s.Skip(12).Take(4)]);
                        var initials = GetString([.. s.Skip(16).Take(32)]);
                        var unk = BitConverter.ToInt32([.. s.Skip(0x30).Take(4)]);
                        output.Write(Encoding.UTF8.GetBytes($"{score},{diff},{combos},{initials},{unk}\n"));
                    }
                    break;
            }
            output.Write("\n"u8);
        } 
    }

    public static string GetString(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
        {
            if (b == 0x00) break;
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    private static string DecompileEvent(Event @event, FpnSst sst)
    {
        return @event.Magic switch
        {
            0 => "func " + @event.Label + "{\n",
            8 => "}\n",
            _ => @event switch
            {
                { Magic: 1, Arg1: 0 } => "\tstop()\n",
                { Magic: 2, Arg1: 0 } => "\tgoto " + @event.Label + "\n",
                { Magic: 5, Arg1: 0 } => "\tendif\n",
                { Magic: 3, Arg1: 1 } => "\tif (" + @event.Label + "[" + @event.Arg3 + "] == 0x" +
                                         @event.Arg2.ToString("X2") + ")\n",
                { Magic: 3, Arg1: 2 } => "\tif (" + @event.Label + "[" + @event.Arg3 + "] == 0x" +
                                         @event.Arg2.ToString("X2") + ")\n",
                { Magic: 7, Arg1: 0 } => ":" + @event.Label + "\n",
                { Magic: 0xE, Arg1: 0 } => "\tplay_video(\"" + sst.GetStringById("IPUN", @event.Arg2) + "\", " +
                                           (@event.Arg3 > 0 ? "RANDOMIZER::ENABLED" : "RANDOMIZER::DISABLED") + ", " +
                                           @event.Arg4 + ")\n",
                { Magic: 9, Arg1: 1 } => "\t" + DecodeSymbol(@event, sst) + "\n",
                { Magic: 9, Arg1: 3 } => "\t" + @event.Label + "[" + @event.Arg3 + "] = " + @event.Arg2 + "\n",
                _ => "\top_0x" + @event.Magic.ToString("X2") + (@event.Label == "" ? "" : "_" + @event.Label) + "(" +
                     StripEnd(@event.Arg1 + "," + @event.Arg2 + "," + @event.Arg3 + "," + @event.Arg4 + "," +
                              @event.Arg5 + "," + @event.Arg6 + "," + @event.Arg7) + ")\n"
            }
        };
    }
    
    private static string DecodeSymbol(Event @event,  FpnSst sst)
    {
        return @event.Label.TrimEnd('\0') switch
        {
            
            "GAME_EVENT" => EncodeSymbolTokens(@event.Label, "[" + @event.Arg2 + "," + @event.Arg3 + "], [" + @event.Arg4 + "," +
                            @event.Arg5 + "], [" + @event.Arg6 + "," + @event.Arg7 + "]"),
            "FONT_EVENT" => EncodeSymbolTokens(@event.Label, "(\"" + sst.GetStringById("FNTN", @event.Arg2) + "\",MSG[" + @event.Arg3 +
                                                             "]," + @event.Arg4 + "," + @event.Arg5 + "," + @event.Arg6 + ")"),
            _ => EncodeSymbolTokens(@event.Label, StripEnd(@event.Arg2 + "," + @event.Arg3 + "," + @event.Arg4 + "," +
                                                            @event.Arg5 + "," + @event.Arg6 + "," + @event.Arg7))
        };
    }

    private static string EncodeSymbolTokens(string label, string args)
    {
        return $"trigger_{label}({args})";
    }

    private static string StripEnd(string args)
    {
        while (args.EndsWith(",0"))
        {
            args = args[..^2];
        }

        return args;
    }
}