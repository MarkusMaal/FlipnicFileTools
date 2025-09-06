namespace FlipnicLib.Types;

public class Gimmick(byte[] data)
{
    public enum GimmickTypes : byte {
        Floor = 0x01,
        Wall = 0x20,
        Slingshot,
        BlueCoin = 0x23,
        BallSavingBumper = 0x25,
        Flipper = 0x30,
        Paddle = 0x33,
        PaddleB = 0x36,
        Block,
        Bumper = 0x42,
        Key = 0x4D,
        LaneDirSelector = 0x52,
        Warp = 0x5A,
        BlueTarget,
        BumperB = 0x63,
        DeathLaser = 0x67,
        ColoredRing,
        TrianglePlate = 0x81,
        MissionMarker,
        Outhole = 0x84,
        StaticPlunger = 0x86,
        RingPlunger = 0x87,
        YellowCoin = 0x8C,
        RingPlungerB,
        ReverseStaticPlunger = 0x92,
        Lane = 0xC8,
        Arrow = 0xCE
    }
    
    public string Label { get; set; } = StaticUtils.GetString(data.Take(0x20).ToArray());
    public GimmickTypes Type { get; set; } = (GimmickTypes)data[0x20];

    public int SoundEffect { get; set; } = StaticUtils.GetInt32(data, 0x5C);

    public float Bounciness { get; set; } = StaticUtils.GetFloat(data, 0x4C);

    public float FlipperStrength { get; set; } = StaticUtils.GetFloat(data, 0x74);
    public float Knockback { get; set; } = StaticUtils.GetFloat(data, 0x54);

    public byte AnalogRange {get; set;} = data[0x6D];

    public bool NoSpawn { get; set; } = data[0x28] > 0;

    public bool Invisible { get; set; } = data[0x2A] > 0;

    public StaticUtils.ControllerButtons Button { get; set; } = (StaticUtils.ControllerButtons)data[0x6C];
}