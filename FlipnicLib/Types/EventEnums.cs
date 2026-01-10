namespace FlipnicLib.Types;

internal static class EventEnums
{
    internal enum ConditionChecks
    {
        HasExtraBall = 7,
        NextStageLocked = 14,
        Difficulty = 15
    }
    
    internal enum EventType
    {
        NoOperation,
        Do = 0x01,
        Loop,
        Then = 0x05,
        Setter,
        EndEvent = 0x08,
        GameEvent,
        TextEvent = 0xA,
        Breq = 0xB,
        BallEvent = 0xC,
        SequenceEvent = 0x0E,        
    }

    internal enum GameEventType
    {
        SetSpawn = 0x06,
        SetMission = 0x8
    }

    internal enum SequenceEventType
    {
        VideoEvent,
        FreezeAndPlaySound,
        SfxEvent,
        BgmEvent = 0x04,
        MuteEvent,
        ResetBgm,
        ScreenFade = 0x07,
        UnfreezeCamera,
        CameraSequence,
        SwitchArea,
        WonderfulSequence = 0x0D,
        GuideSfxEvent
    }

    internal enum MissionStatus
    {
        Incomplete,
        Started,
        Completed,
        StartedCompleted
    }

    internal enum HexagonFlashType
    {
        Off,
        Flashing,
        FastFlashing
    }

    internal enum BallEventType
    {
        ToggleControl = 0x02,
        PoleEvent = 0x0F
    }

    internal enum ControllerToggles
    {
        UnlockPlunger = 0x0F,
        LockPlunger,
    }

    internal enum TextEventType
    {
        BonusEvent,
    }
}