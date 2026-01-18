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

    // generate a comment for the user describing what the function should do
    private static string GenerateComment(string label)
    {
        var c = label switch
        {
            "SYOKI_SETTEI" => "Initial setup",
            "START" => "Initial entry point",
            "RESET_EVENT" => "Reset the stage after completing it (also runs when you first start the stage)",
            "EVENT_FLIES" => "Zero gravity",
            "AIR_HOKKEY" => "Galaxy tennis",
            "EVENT_AH" => "Galaxy tennis",
            "EXP_COUNT_EVT" => "Total EXP Counts",
            "EVENT_SOCCER" => "Setup 2P specific gameplay features (e.g. hiding the OSD, scoring, etc.)",
            "BMP_VILLAGE" => "Bumper village",
            "P_BMP_VILLAGE" => "Perfect bumper village",
            "TEST_TUBE_AREA" => "Loop the loop",
            "REBIRTH_BALL" => "Respawn",
            "EVENT_PON" => "Point of no return (or \"Pong\" in VS4.SST)",
            "LOST_BALL" =>
                "Normally not used, but it's triggered in case the ball flies out of bounds without \"touching\" the outhole",
            "CRAB_BABY" => "Crab baby shoot down",
            "UFO_AREA_CLOSURE" => "Close the lane that normally takes you to the UFO area (pink)",
            "COLOR_POLE_R" => "Red tower bumper (at initial spawn area)",
            "COLOR_POLE_G" => "Green tower bumper (at initial spawn area)",
            "WARP_EVENT" => "Teleport to Zero Gravity after going through the specified lane",
            "DOMOGRAM_WARN" => "Spider crab warning",
            "COIN_COMB_CHK" => "Coin combo checks",
            "JACKPOTAGAIN" => "Jackpot revived",
            "SMB_MAIN" => "Spider crab multiball (SMB = Small Multiball?)",
            "STAGE_CLEAR" => "Stage clear sequence",
            "SLOT_EVENT" => "Reset areas to default and reset music after a slot chance minigame",
            "TAKI_WARI" => "Waterfall i.e. hidden path discovery",
            "LUCKEY_FLAMINGOS" => "Lucky Flamingos (misspelled?)",
            "HUNGLY_MONKEY" => "Hungry Monkey (misspelled?)",
            "TREE_HIT_CHECK" => "At the multiball area",
            "BANZAI_COIN" => "Waterfall coins",
            "BANZAI_GOAL" => "Triggers when all coins are collected from the waterfall",
            "HELP_UFO13131" =>
                "\"Help ufo\" seems to refer to the idling UFOs that fly in and out every 25 bumpers hit",
            "TOTAL_LANE_EVT" => "Total lane counts",
            "TOTAL_BMP_EVT" => "Total bumper counts",
            "MULTI_COMET" => "In Evolution/Theology stages, changing MaxBalls here will allow you to have more (or less) than 3 balls at once, setting the value to 0 will disable multiball",
            "CONTINUE" => "Triggered when you lose all balls and are asked if you want to continue the game or give up",
            "GAME_OVER" => "Triggered when you either lose all balls and extra credits, time runs out or when you select \"No\" from the \"CONTINUE?\" prompt",
            "TILT" => "Triggered when you nudge the board too much",
            _ => ""
        };

        return c == "" ? "" : $"#\n#  {c}\n#\n\n";
    }

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
            if (EventArgs[0] > 0x9)
            {
                return
                    $"ToggleGate, AreaCode: {sst.GetStringById("KUIDX", FuncArgs[1])[3..]}, ObjectID: {FuncArgs[2]}, GateState::{(EventEnums.GateState)EventArgs[0]}";
            } else { 
                return
                    $"ToggleLight, AreaCode: {sst.GetStringById("KUIDX", FuncArgs[1])[3..]}, ObjectID: {FuncArgs[2]}, FlashType::{(EventEnums.HexagonFlashType)EventArgs[0]}";
            }
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
        if ((EventEnums.SequenceEventType)FuncArgs[1] == EventEnums.SequenceEventType.UnfreezeCamera) return "UnfreezeCamera";
        return (EventEnums.SequenceEventType)FuncArgs[1] + ", " + (EventEnums.SequenceEventType)FuncArgs[1] switch
        {
            EventEnums.SequenceEventType.BgmEvent => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}",
            EventEnums.SequenceEventType.ScreenFade => "FadeOut: " + (FuncArgs[2] == 1 ? "true" : "false") + $", Ticks: {FuncArgs[3]}",
            EventEnums.SequenceEventType.CameraSequence => $"Filename: {sst.GetStringById("CAMN", FuncArgs[2])}",
            EventEnums.SequenceEventType.WonderfulSequence => "DisplayText: " + (FuncArgs[2] == 1 ? "true" : "false") + $", MsgId: {FuncArgs[3]}",
            EventEnums.SequenceEventType.FreezeAndPlaySound => $"Filename: {sst.GetStringById("SEQN", FuncArgs[2])}, RestoreFilename: {sst.GetStringById("SEQN", FuncArgs[0])}",
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
        var offsetStr = offset.ToString("X").PadLeft(4, '0');
        if (Label != string.Empty && FuncArgs.Sum() > 0)
        {

            if ((EventMagic == 9) && (FuncArgs[1] == 2))
            {
                o += $"\n\t{Label}()\n";
                return o;
            }

            if ((EventMagic == 3) && (Label == "THIS"))
            {
                o += $"\njump here when (value[{FuncArgs[1]}] == {FuncArgs[2]})\n";
                return o;
            }
            switch (Label)
            {
                case "COMET_MULTI_BALL":
                    if (EventMagic == 9)
                    {
                        o +=
                            $"\nfunc {Label} (UnkValue0: {FuncArgs[1]}, MaxBalls: {FuncArgs[2]}, UnkValue2: {FuncArgs[3]}, UnkValue3: {EventArgs[0]}) @ 0x" +
                            offsetStr + "\n";
                    }
                    break;
                case "TIMER_EVENT":
                    if (FuncArgs[1] == 1)
                    {
                        o += $"\n\t{Label} (Flag: {FuncArgs[1]}, FrameCount: {FuncArgs[2]}, GracePeriodFrameCount: {FuncArgs[3]}, Font: {sst.GetStringById("FNTN", EventArgs[0])}, AnimFont: {sst.GetStringById("FNTN", EventArgs[1])})";
                    }
                    break;
                case "SMART_BALL":
                    if (EventMagic == 9)
                    {
                        o += $"\nfunc {Label} (AreaId1: {sst.GetStringById("KUIDX", FuncArgs[2])}, AreaId2: {sst.GetStringById("KUIDX", FuncArgs[3])}, Balls: {EventArgs[0]}, BuzzerSoundId: {(EventArgs[1] >> 16) & 0xFF }, ???: {EventArgs[2]}, ???: {EventArgs[3]}) @ 0x" + offsetStr + "\n";
                    }
                    break;
                case "GAME_EVENT":
                    var args = "";
                    if (EventMagic == 3)
                    {
                        o += $"\nswitchcase ({(EventEnums.ConditionChecks)FuncArgs[3]} == {FuncArgs[2]})\n";
                        return o;
                    }
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
                    o += $"\nfunc {Label} ({args}) @ 0x" + offsetStr + "\n";
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
                EventEnums.EventType.GameEvent when FuncArgs[1] == 1 => $"\n:{Label}\n",
                EventEnums.EventType.Setter => $"\nfunc {Label} (value={FuncArgs[1]})\n",
                EventEnums.EventType.Breq => $"\ngoto {Label} when (value == {FuncArgs[1]})",
                _ => $"\nfunc {Label} ({string.Join(", ", allArgs)}) @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n"
            };
        }
        else if (Label != string.Empty)
        { 
            o += $"\n{GenerateComment(Label)}func {Label} () @ 0x" + offset.ToString("X").PadLeft(4, '0') + "\n";
        }

        o += (EventEnums.EventType)EventMagic switch
        {
            EventEnums.EventType.Breq => "",
            EventEnums.EventType.NoOperation => "nop",
            EventEnums.EventType.Do => "loopstart",
            EventEnums.EventType.Then => "\nnext",
            EventEnums.EventType.EndEvent => "}\n",
            EventEnums.EventType.BallEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetBallEventArgs(sst)})",
            EventEnums.EventType.Loop => "loopend",
            EventEnums.EventType.TextEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetTextEventArgs(sst, msg)})",
            EventEnums.EventType.GameEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetGameEventArgs(sst, msg)})",
            EventEnums.EventType.SequenceEvent => $"\t{(EventEnums.EventType)EventMagic} ({GetSequenceEventArgs(sst)})",
            _ => $"\t0x{EventMagic:X} ({string.Join(", ", EventArgs)})"
        };

        o += "\n";
        return o;
    }
}