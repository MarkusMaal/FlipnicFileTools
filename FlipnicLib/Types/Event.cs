using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Event(byte[] data)
{
    
    /// <summary>
    /// May call a function
    /// </summary>
    private string Label { get; set; } = StaticUtils.GetString(data.Skip(4).Take(0x1C).ToArray());

    /// <summary>
    /// Defines event type
    /// </summary>
    private int EventMagic { get; set; } = StaticUtils.GetInt32(data, 0);

    /// <summary>
    /// Arguments for specific function if Label is not empty
    /// </summary>
    private int[] FuncArgs { get; set; } = [StaticUtils.GetInt32(data, 0x20), StaticUtils.GetInt32(data, 0x24),
        StaticUtils.GetInt32(data, 0x28), StaticUtils.GetInt32(data, 0x2C)];

    /// <summary>
    /// Arguments for the event (if event magic is not 0)
    /// </summary>
    private int[] EventArgs { get; set; } =
    [
        StaticUtils.GetInt32(data, 0x30), StaticUtils.GetInt32(data, 0x34),
        StaticUtils.GetInt32(data, 0x38), StaticUtils.GetInt32(data, 0x3C)
    ];

    private string GetGameEventArgs(FpnSst sst, FpnMsg? msg)
    {
        return (EventEnums.GameEventType) FuncArgs[1] + ", " +  (EventEnums.GameEventType)FuncArgs[1] switch
        {
            EventEnums.GameEventType.SetMission => $"{GetMessageById(msg, FuncArgs[2])}, Status::{(EventEnums.MissionStatus)FuncArgs[3]}",
            EventEnums.GameEventType.SetSpawn => $"AreaCode: {sst.GetStringById("KUIDX", FuncArgs[2])[3..]}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    private static string GetMessageById(FpnMsg? msg, int id)
    {
        if (msg?.GetMessageById(id) == "MASTER") return "MASTER";
        return msg != null ? "\"" + msg.GetMessageById(id) + "\"" : id.ToString();
    }

    private string GetBallEventArgs(FpnSst sst)
    {
        if (FuncArgs[3] == 0x0A)
        {
            return
                $"ToggleLight, AreaCode: {sst.GetStringById("KUIDX", FuncArgs[1])[3..]}, ObjectID: {FuncArgs[2]}, FlashType::{(EventEnums.HexagonFlashType)EventArgs[0]}";
        }
        return (EventEnums.BallEventType) FuncArgs[1] + ", " +  (EventEnums.BallEventType)FuncArgs[1] switch
        {
            EventEnums.BallEventType.ToggleControl => $"Toggle::{(EventEnums.ControllerToggles)FuncArgs[3]}",
            EventEnums.BallEventType.PoleEvent=> $"ID: {FuncArgs[2]}, State: {FuncArgs[3]}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    private string GetSequenceEventArgs(FpnSst sst)
    {
        return (EventEnums.SequenceEventType)FuncArgs[1] + ", " + (EventEnums.SequenceEventType)FuncArgs[1] switch
        {
            EventEnums.SequenceEventType.BgmEvent => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}",
            EventEnums.SequenceEventType.ScreenFade => "FadeOut: " + (FuncArgs[2] == 1 ? "true" : "false") + $", Ticks: {FuncArgs[3]}",
            EventEnums.SequenceEventType.CameraSequence => $"Filename: {sst.GetStringById("CAMN", FuncArgs[2])}",
            EventEnums.SequenceEventType.WonderfulSequence => "DisplayText: " + (FuncArgs[2] == 1 ? "true" : "false") + $", MsgId: {FuncArgs[3]}",
            EventEnums.SequenceEventType.FreezeAndPlaySound => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}",
            EventEnums.SequenceEventType.SfxEvent  => $"SoundID: {FuncArgs[2]}",
            EventEnums.SequenceEventType.GuideSfxEvent  => $"Filename: {sst.GetStringById("INTN", FuncArgs[2])}",
            EventEnums.SequenceEventType.VideoEvent => $"Filename: {sst.GetStringById("IPUN", FuncArgs[2])}, Randomize: " + (FuncArgs[3] == 1 ? "true" : "false") + $", RandomizerSeed: {EventArgs[0]}",
            EventEnums.SequenceEventType.SwitchArea => $"FromAreaCode: {sst.GetStringById("KUIDX", FuncArgs[2])[3..]}, Variation: {FuncArgs[3]}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    private string GetTextEventArgs(FpnSst sst, FpnMsg? msg)
    {
        return (EventEnums.TextEventType)FuncArgs[1] + ", " + (EventEnums.TextEventType)FuncArgs[1] switch
        {
            EventEnums.TextEventType.BonusEvent => $"Points: {FuncArgs[2]}, Font: {sst.GetStringById("FNTN",  FuncArgs[3])}, Message: {GetMessageById(msg, EventArgs[0])}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    public string GetPseudoCodeLine(FpnSst sst, int offset, FpnMsg? msg)
    {
        var o = "";
        var extendArgs = EventMagic == 9 && Label != string.Empty;
        if (Label != string.Empty && FuncArgs.Sum() > 0)
        {
            switch (Label)
            {
                case "GAME_EVENT":
                    var args = "";
                    switch (FuncArgs[1])
                    {
                        case 1:
                            args += $"Balls: [{FuncArgs[2]}, {EventArgs[0]}, {EventArgs[2]}], Credits: [{FuncArgs[3]}, {EventArgs[1]}, {EventArgs[3]}]";
                            break;
                        case 2:
                            if ((FuncArgs[2] == 1) && (FuncArgs[3] == 7))
                            {
                                args += "ExtraBall_Enable";
                            }

                            break;
                    }
                    o += $"\nfunc {Label} ({args}) @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
                    return o;
                case "FONT_EVENT":
                    o += $"\nfunc {Label} (Font: {sst.GetStringById("FNTN",  FuncArgs[2])}, Message: {GetMessageById(msg, FuncArgs[3])}, Duration: {EventArgs[0]}, Entrance: {EventArgs[1]}, Exit: {EventArgs[2]})".PadRight((int)(StaticUtils.WindowWidth / 1.25), ' ') + " @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
                    return o;
                case "FONT":
                    o += $"\nfunc {Label} (Font: {sst.GetStringById("FNTN",  FuncArgs[2])}, Message: {GetMessageById(msg, FuncArgs[3])}, SecondaryFont: {sst.GetStringById("FNTN", EventArgs[0])}, SecondaryMessage: {GetMessageById(msg, EventArgs[1])}, ???: {EventArgs[2]}, ???: {EventArgs[3]})".PadRight((int)(StaticUtils.WindowWidth / 1.25), ' ') + " @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
                    return o;
            }
            var allArgs = FuncArgs;
            if (extendArgs)
            {
                allArgs = new int[FuncArgs.Length + EventArgs.Length];
                FuncArgs.CopyTo(allArgs, 0);
                EventArgs.CopyTo(allArgs, FuncArgs.Length);
            }

            o += (EventEnums.EventType)EventMagic switch
            {
                EventEnums.EventType.GameEvent when FuncArgs[1] == 1 => $"\n\tget {Label}\n",
                EventEnums.EventType.Setter => $"\nfunc {Label} (value={FuncArgs[1]})\n",
                EventEnums.EventType.Breq => $"\n\tif value == {FuncArgs[1]} goto {Label}",
                _ => $"\nfunc {Label} ({string.Join(", ", allArgs)}) @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n"
            };
        }
        else if (Label != string.Empty)
        { 
            o += $"\nfunc {Label} () @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
        }

        o += (EventEnums.EventType)EventMagic switch
        {
            EventEnums.EventType.Breq => "",
            EventEnums.EventType.NoOperation => "nop",
            EventEnums.EventType.Do => "do",
            EventEnums.EventType.EndEvent => "end\n",
            EventEnums.EventType.BallEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetBallEventArgs(sst)})",
            EventEnums.EventType.Loop => "loop",
            EventEnums.EventType.TextEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetTextEventArgs(sst, msg)})",
            EventEnums.EventType.GameEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetGameEventArgs(sst, msg)})",
            EventEnums.EventType.SequenceEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetSequenceEventArgs(sst)})",
            _ => $"\t0x{EventMagic:X} ({string.Join(", ", EventArgs)})"
        };

        o += "\n";
        return o;
    }
}