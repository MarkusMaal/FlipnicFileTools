
using System.Security;
using System.Text;

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
    private List<float[]> verticies = new();

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

    public static void ExportObj(float[] vertices,  string outFile)
    {
        using var writer = new FileStream(outFile, FileMode.Create, FileAccess.Write);
        for (int i = 0; i < vertices.Length; i+=3)
        {
            float[] vertex = [vertices[i], vertices[i + 1], vertices[i + 2]];
            writer.Write(Encoding.ASCII.GetBytes($"v {StaticUtils.DotFloatString(vertex[0])} {StaticUtils.DotFloatString(vertex[1])} {StaticUtils.DotFloatString(vertex[2])}\n"));
        }
        writer.Close();
    }

    public void Read()
    {
        var i = 0;
        while (i < data.Length - 0x20)
        {
            float f, f2, f3, f4;
            f = BitConverter.ToSingle([.. data.Skip(i).Take(4)], 0);
            f2 = BitConverter.ToSingle([.. data.Skip(i + 0x4).Take(4)], 0);
            f3 = BitConverter.ToSingle([.. data.Skip(i + 0xc).Take(4)], 0);
            f4 = BitConverter.ToSingle([.. data.Skip(i + 0x1c).Take(4)], 0);
            if ((f == f2) && (f == f3) && (f4 == 1f))
            {
                var len = BitConverter.ToInt32(data.Skip(i - 0x10).Take(4).ToArray());
                if ((len > 0) && (len < data.Length))
                {
                    AppendVerticies(i);
                    i += len * 0x10;
                }
            }
            i += 0x10;
        }
    }

    private void AppendVerticies(int offset, int forced_length = -1)
    {
        var len = forced_length == -1 ? BitConverter.ToInt32(data, offset) + 1 : forced_length;

        for (var i = offset + 0x10; i < offset + len * 0x10 + 0x10; i += 0x10)
        {
            var x = BitConverter.ToSingle(data.Skip(i).Take(4).ToArray(), 0);
            var y = BitConverter.ToSingle(data.Skip(i + 4).Take(4).ToArray(), 0);
            var z = BitConverter.ToSingle(data.Skip(i + 8).Take(4).ToArray(), 0);
            var w = BitConverter.ToSingle(data.Skip(i + 12).Take(4).ToArray(), 0);
            verticies.Add([x, y, z]);
        }
    }

    public float[] GetVerticies()
    {
        List<float> result = [];
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        foreach (var vertex in verticies)
        {
            foreach (var point in vertex)
            {
                if (point < minValue) minValue = point;
                if (point > maxValue) maxValue = point;
            }
        }

        foreach (var vertex in verticies)
        {
            //result.Add(vertex[0] > 0 ? vertex[0] / maxValue : -vertex[0] / minValue);
            //result.Add(vertex[1] > 0 ? vertex[1] / maxValue : -vertex[1] / minValue);
            //result.Add(vertex[2] > 0 ? vertex[2] / maxValue : -vertex[2] / minValue);
            result.Add(vertex[0]);
            result.Add(vertex[1]);
            result.Add(vertex[2]);
        }
        return result.ToArray();
    }
}