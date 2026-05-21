using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Gimmick(byte[] data) : FormatBase
{
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
    
    public string Label { get; set; } = GetString(data.Take(0x20).ToArray());
    public GimmickTypes Type { get; set; } = (GimmickTypes)data[0x20];

    public int SoundEffect { get; set; } = GetInt32(data, 0x5C);

    public float Bounciness { get; set; } = GetFloat(data, 0x4C);

    public float FlipperStrength { get; set; } = GetFloat(data, 0x74);
    public float Knockback { get; set; } = GetFloat(data, 0x54);

    public byte AnalogRange {get; set;} = data[0x6D];

    public bool NoSpawn { get; set; } = data[0x28] > 0;

    public bool Invisible { get; set; } = data[0x2A] > 0;

    public ControllerButtons Button { get; set; } = (ControllerButtons)data[0x6C];
    
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