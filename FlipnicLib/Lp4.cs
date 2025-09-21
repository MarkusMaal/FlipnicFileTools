
using System.Security;
using System.Text;

namespace FlipnicLib;

public class Lp4(byte[] data, string fileName)
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
    
    private List<float> rawVerticies = new();

    private string TexturePath { get; set; } = "";
    
    private string FileName { get; set; } = fileName;
    
    public Tim2? Texture { get; set; }

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
            int f, f2, f3, f4;
            f = BitConverter.ToInt32([.. data.Skip(i).Take(4)], 0);
            f2 = BitConverter.ToInt32([.. data.Skip(i + 0x10).Take(4)], 0);
            f3 = BitConverter.ToInt32([.. data.Skip(i + 0x14).Take(4)], 0);
            f4 = BitConverter.ToInt32([.. data.Skip(i + 0x1c).Take(4)], 0);
            if ((f > 0) && (f2 == f3) && (f3 == f4))
            {
                var len = f2;
                if ((len > 0) && (len < data.Length))
                {
                    AppendVerticies(i+0x20, len);
                    i += 2*len * 0x10 + 0xA0;
                    break;
                }
            }
            i += 0x10;
        }
        TexturePath = StaticUtils.GetString(data.Skip(i).Take(0x20).ToArray());

        if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                TexturePath.ToUpper()))) return;
        var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
            TexturePath.ToUpper()));
        var d = new byte[fs.Length];
        fs.ReadExactly(d, 0, d.Length);
        Texture = new Tim2(d, TexturePath);
    }

    private void AppendVerticies(int offset, int forced_length = -1)
    {
        var len = forced_length == -1 ? BitConverter.ToInt32(data, offset) + 1 : forced_length;

        float tex_x, tex_y, x, y, z;
        //for (var i = offset; i < offset + len * 0x10; i += 0x10)
        List<float> vertices = new();
        for (var i = offset; i < offset + len * 0x10 - 0x20; i += 0x10)
        {

            try
            {
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i+12).Take(4).ToArray(), 0));
                rawVerticies.Add(-BitConverter.ToSingle(data.Skip(i).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i+4).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i+8).Take(4).ToArray(), 0));
                

                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 12).Take(4).ToArray(), 0));
                rawVerticies.Add(-BitConverter.ToSingle(data.Skip(i + 0x20).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 0x24).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 0x28).Take(4).ToArray(), 0));
                
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 12).Take(4).ToArray(), 0));
                rawVerticies.Add(-BitConverter.ToSingle(data.Skip(i + 0x10).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 0x14).Take(4).ToArray(), 0));
                rawVerticies.Add(BitConverter.ToSingle(data.Skip(i + 0x18).Take(4).ToArray(), 0));
            }
            catch
            {
                break;
            }
        }
    }

    public float[] GetVerticies()
    {
        return rawVerticies.ToArray();
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
            result.Add(vertex[3]);
            result.Add(vertex[4]);
        }
        return result.ToArray();
    }
}