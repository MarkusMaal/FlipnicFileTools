using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class CamData : FormatBase
{
    public int CameraId { get; }
    public string CameraName { get; }
    public bool AnchorToTarget { get; }
    public bool LockXAxis  { get; }
    public bool LockYAxis  { get; }
    public bool LockZAxis  { get; }
    public float StiffnessX { get; }
    public float StiffnessY { get; }
    public float StiffnessZ { get; }

    public CamData(byte[] data, FpnSst stage)
    {
        CameraId = GetInt32(data, 0); // seems to refer to KUIDX
        CameraName = stage.GetStringById("CAMN", GetInt32(data, 0x1C));
        // how "smooth" the camera moves towards the target
        //
        // the higher the value, the smoother the movement looks
        // lower values are more "stiff"
        StiffnessX = GetFloat(data, 0x24);
        StiffnessY = GetFloat(data, 0x28);
        StiffnessZ = GetFloat(data, 0x2C);
        
        // Examples:
        // +------------+--------------------------------------------------------------------+
        // | Binary/hex | Result                                                             |
        // +------------+--------------------------------------------------------------------+
        // | 0000b (0h) | Target on the ball, origin is static (puking simulator)            |
        // | 0111b (7h) | Camera is static (uses values from FPC file)                       |
        // | 0101b (5h) | Camera is static except when moving up/down (e.g. zero gravity)    |
        // | 1000b (8h) | Camera is anchored to ball position (e.g. bumper area in optics)   |
        // | 1011b (Bh) | Follows the ball along the X-axis (e.g. starting area in geometry) |
        // +------------+--------------------------------------------------------------------+
        var flags = data[0x18];
        AnchorToTarget = (flags & 0x08) == 0x08;  // if True, the origin gets anchored to ball position
        // if any of these are False, gets the target
        // value for specific axis from current ball
        // position instead of FPC file
        LockXAxis = (flags & 0x04) == 0x04;
        LockYAxis = (flags & 0x02) == 0x02;
        LockZAxis = (flags & 0x01) == 0x01;
    }

    public string GetAxisString()
    {
        return (LockXAxis ? "X" : "-") + (LockYAxis ? "Y" : "-") + (LockZAxis ? "Z" : "-");
    }

    public string GetStiffnessXyz()
    {
        return DotFloatString(StiffnessX) + "/" + 
               DotFloatString(StiffnessY) + "/" +
               DotFloatString(StiffnessZ) ;
    }
}