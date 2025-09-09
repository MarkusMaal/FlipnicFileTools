namespace FlipnicLib;

public class Lp4(byte[] data)
{
    public enum FileType {
        VariableList,
        StaticModel,
        AnimatedModel,
        Particle,
        HudElement,
        TextAnimation = 0x16
    };
    
    public FileType Type { get; set; } = (FileType)StaticUtils.GetInt32(data, 4);
    public int ModelCount { get; set; } = StaticUtils.GetInt32(data, 0); // not sure if that's what it is anymore
    public bool HasEmbeddedResources { get; set; } = data[0x11] == 0x01;
    public bool Is2dAnimation { get; set; } = data[0x13] == 0x01;

    public override string ToString()
    {
        var er = HasEmbeddedResources ? "Yes" : "No";
        var i2 = Is2dAnimation ? "Yes" : "No";
        var o = $"""
                Type: {Type.ToString()}
                Has embedded resources: {er}
                Is 2D animation: {i2}
                """;
        return o;
    }
}