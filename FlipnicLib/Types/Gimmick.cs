using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Gimmick : FormatBase
{
    public Gimmick()
    {
        
    }
    public Gimmick(byte[] data)
    {
        Label = GetString(data.Take(0x20).ToArray());
        Type = (GimmickTypes)data[0x20];
        SoundEffect = GetInt32(data, 0x5C);
        Bounciness = GetFloat(data, 0x4C);
        FlipperStrength = GetFloat(data, 0x74);
        Knockback = GetFloat(data, 0x54);
        AnalogRange = data[0x6D];
        NoSpawn = data[0x28] > 0;
        Invisible = data[0x2A] > 0;
        Button = (ControllerButtons)data[0x6C];
    }

    public enum GimmickTypes : byte {
        SoundTrigger,
        Floor,
        Butterfly = 0x04,
        Wall = 0x20,
        Slingshot,
        BlueCoin = 0x23,
        BallSavingBumper = 0x25,
        Flipper = 0x30,
        SideFlipper,
        JackpotMarker,
        Paddle,
        PaddleB = 0x36,
        Block,
        BumperC = 0x39,
        FlipperB = 0x3D,
        Bumper = 0x42,
        BumperE = 0x45,
        BumperF,
        BumperG,
        BumperI = 0x4A,
        BumperD = 0x4C,
        HittableObject,
        BumperJ = 0x4E,
        BumperH,
        BumperK,
        LaneDirSelector = 0x52,
        SpinPole = 0x55,
        SpinPoleB,
        SpinPoleC,
        ElevatorSpinPole,
        IndestructibleObject = 0x59,
        Warp,
        BlueTarget,
        OutholeB,
        BumperB = 0x63,
        ColoredRing,
        DeathLaser = 0x67,
        TrianglePlate = 0x81,
        MissionMarker,
        ScriptTrigger = 0x80,
        Outhole = 0x84,
        StaticPlunger = 0x86,
        RingPlunger,
        PowerPlunger,
        EnemyCrabBaby,
        CrabBaby,
        SpiderCrab,
        CoinTrail,
        RingPlungerB,
        ReverseStaticPlunger = 0x92,
        BigUfo = 0xB0,
        SmallUfo,
        Ufo2D = 0xB3,
        Lane = 0xC8,
        SideLane = 0xCA,
        SpecialLane,
        LaneB = 0xCD,
        Arrow,
        MultihitTarget = 0xE9
    }
    
    public string Label { get; set; }
    public GimmickTypes Type { get; set; }

    public int SoundEffect { get; set; }

    public float Bounciness { get; set; }

    public float FlipperStrength { get; set; }
    public float Knockback { get; set; }

    public byte AnalogRange {get; set;}

    public bool NoSpawn { get; set; }

    public bool Invisible { get; set; }

    public ControllerButtons Button { get; set; }
    
    public string Lp4File
    {
        get
        {
            if (!Label.Contains("__"))
            {
                return "N/A";
            }
            return Label.Split("__").FirstOrDefault() + ".LP4";
        }
    }
}