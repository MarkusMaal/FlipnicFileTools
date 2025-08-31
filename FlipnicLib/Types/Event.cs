namespace FlipnicLib.Types;

public class Event(byte[] data)
{
    private enum EventType
    {
        NoOperation,
        Do = 0x01,
        Loop,
        EndEvent = 0x08,
        GameEvent,
        TextEvent = 0xA,
        BallEvent = 0xC,
        SequenceEvent = 0x0E,        
    }

    private enum GameEventType
    {
        SetSpawn = 0x06,
        SetMission = 0x8
    }

    private enum SequenceEventType
    {
        VideoEvent,
        FreezeAndPlaySound,
        SfxEvent,
        BgmEvent = 0x04,
        MuteEvent,
        ResetBgm,
        ScreenFade = 0x07,
        CameraSequence = 0x09,
        WonderfulSequence = 0x0D,
        GuideSfxEvent
    }

    private enum MissionStatus
    {
        Incomplete,
        Started,
        Completed,
        StartedCompleted
    }

    private enum BallEventType
    {
        ToggleControl = 0x02
    }

    private enum ControllerToggles
    {
        UnlockPlunger = 0x0F,
        LockPlunger,
    }

    private enum TextEventType
    {
        BonusEvent,
    }
    
    /// <summary>
    /// May call a function
    /// </summary>
    public string Label { get; set; } = StaticUtils.GetString(data.Skip(4).Take(0x1C).ToArray());

    /// <summary>
    /// Defines event type
    /// </summary>
    public int EventMagic { get; set; } = StaticUtils.GetInt32(data, 0);

    /// <summary>
    /// Arguments for specific function if Label is not empty
    /// </summary>
    public int[] FuncArgs { get; set; } = [StaticUtils.GetInt32(data, 0x20), StaticUtils.GetInt32(data, 0x24),
        StaticUtils.GetInt32(data, 0x28), StaticUtils.GetInt32(data, 0x2C)];

    /// <summary>
    /// Arguments for the event (if event magic is not 0)
    /// </summary>
    public int[] EventArgs { get; set; } =
    [
        StaticUtils.GetInt32(data, 0x30), StaticUtils.GetInt32(data, 0x34),
        StaticUtils.GetInt32(data, 0x38), StaticUtils.GetInt32(data, 0x3C)
    ];

    private string GetGameEventArgs(FpnSst sst, FpnMsg? msg)
    {
        return (GameEventType) FuncArgs[1] + ", " +  (GameEventType)FuncArgs[1] switch
        {
            GameEventType.SetMission => $"{GetMessageById(msg, FuncArgs[2])}, Status::{(MissionStatus)FuncArgs[3]}",
            GameEventType.SetSpawn => $"AreaCode: {sst.GetStringById("KUIDX", FuncArgs[2])[3..]}",
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
        return (BallEventType) FuncArgs[1] + ", " +  (BallEventType)FuncArgs[1] switch
        {
            BallEventType.ToggleControl => $"Toggle::{(ControllerToggles)FuncArgs[3]}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    private string GetSequenceEventArgs(FpnSst sst)
    {
        return (SequenceEventType)FuncArgs[1] + ", " + (SequenceEventType)FuncArgs[1] switch
        {
            SequenceEventType.BgmEvent => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}",
            SequenceEventType.ScreenFade => "FadeOut: " + (FuncArgs[2] == 1 ? "true" : "false") + $", Ticks: {FuncArgs[3]}",
            SequenceEventType.CameraSequence => $"Filename: {sst.GetStringById("CAMN", FuncArgs[2])}",
            SequenceEventType.WonderfulSequence => "DisplayText: " + (FuncArgs[2] == 1 ? "true" : "false"),
            SequenceEventType.FreezeAndPlaySound => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}",
            SequenceEventType.SfxEvent  => $"SoundID: {FuncArgs[2]}",
            SequenceEventType.GuideSfxEvent  => $"Filename: {sst.GetStringById("INTN", FuncArgs[2])}",
            SequenceEventType.VideoEvent => $"Filename: {sst.GetStringById("IPUN", FuncArgs[2])}, Randomize: " + (FuncArgs[3] == 1 ? "true" : "false") + $", RandomizerSeed: {EventArgs[0]}",
            _ => $"???: {string.Join(", ???: ", FuncArgs.Skip(2).ToArray())}"
        };
    }

    private string GetTextEventArgs(FpnSst sst, FpnMsg? msg)
    {
        return (TextEventType)FuncArgs[1] + ", " + (TextEventType)FuncArgs[1] switch
        {
            TextEventType.BonusEvent => $"Points: {FuncArgs[2]}, Font: {sst.GetStringById("FNTN",  FuncArgs[3])}, Message: {GetMessageById(msg, EventArgs[0])}",
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
                    o += $"\nfunc {Label} ({args})".PadRight((int)(StaticUtils.WindowWidth / 1.25), ' ') + " @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
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
            o += $"\nfunc {Label} ({string.Join(", ", allArgs)})".PadRight((int)(StaticUtils.WindowWidth / 1.25), ' ') + " @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
        }
        else if (Label != string.Empty)
        { 
            o += $"\nfunc {Label} ()".PadRight((int)(StaticUtils.WindowWidth / 1.25), ' ') + " @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
        }

        o += (EventType)EventMagic switch
        {
            EventType.NoOperation => "nop",
            EventType.Do => "do",
            EventType.EndEvent => "end\n",
            EventType.BallEvent => $"\t{(EventType)EventMagic} ({GetBallEventArgs(sst)})",
            EventType.Loop => "loop",
            EventType.TextEvent => $"\t{(EventType)EventMagic} ({GetTextEventArgs(sst, msg)})",
            EventType.GameEvent => $"\t{(EventType)EventMagic} ({GetGameEventArgs(sst, msg)})",
            EventType.SequenceEvent => $"\t{(EventType)EventMagic} ({GetSequenceEventArgs(sst)})",
            _ => $"\t0x{EventMagic:X} ({string.Join(", ", EventArgs)})"
        };

        o += "\n";
        return o;
    }
}