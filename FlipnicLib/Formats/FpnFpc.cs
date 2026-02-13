using System.Xml.Linq;
using FlipnicLib.Types;

namespace FlipnicLib.Formats;

public class FpnFpc
{
    readonly List<float> SequenceXo = [];
    readonly List<float> SequenceYo = [];
    readonly List<float> SequenceZo = [];

    readonly List<float> SequenceXt = [];
    readonly List<float> SequenceYt = [];
    readonly List<float> SequenceZt = [];
    readonly List<float> SequenceFov = [];

    private readonly float FOV;
    readonly float OriginX;
    readonly float OriginY;
    readonly float OriginZ;

    readonly float TargetX;
    readonly float TargetY;
    readonly float TargetZ;

    public string FOVf => StaticUtils.DotFloatString(FOV);
    public string OriginXf => StaticUtils.DotFloatString(OriginX);
    public string OriginYf => StaticUtils.DotFloatString(OriginY);
    public string OriginZf => StaticUtils.DotFloatString(OriginZ);
    public string TargetXf => StaticUtils.DotFloatString(TargetX);
    public string TargetYf => StaticUtils.DotFloatString(TargetY);
    public string TargetZf => StaticUtils.DotFloatString(TargetZ);

    //private int TotalFrames;

    private readonly byte[] Data;

    private readonly int NumFrames;
    private readonly int NumSequences;
    public string NumFramesStr => NumFrames.ToString();

    private enum ValueIDs : int
    {
        OriginX,
        OriginY,
        OriginZ,
        TargetX,
        TargetY,
        TargetZ,
        Fov = 0x07
    }

    public FpnFpc()
    {
        Data = [];
        OriginX = 0f;
        OriginY = 0f;
        OriginZ = 0f;
        TargetX = 0f;
        TargetY = 0f;
        TargetZ = 0f;
        FOV = 90;
        NumSequences = 0;
        NumFrames = 0;
    }
    
    public FpnFpc(string filename) : this(File.OpenRead(filename)) {}

    public CameraFrame[] CamFrames
    {
        get
        {
            List<CameraFrame> frames = [];
            for (var i = 0; i < NumFrames; i++)
            {
                var ox = SequenceXo.Count > i ? SequenceXo[i] : OriginX;
                var oy = SequenceYo.Count > i ? SequenceYo[i] : OriginY;
                var oz = SequenceZo.Count > i ? SequenceZo[i] : OriginZ;
                var tx = SequenceXt.Count > i ? SequenceXt[i] : TargetX;
                var ty = SequenceYt.Count > i ? SequenceYt[i] : TargetY;
                var tz = SequenceZt.Count > i ? SequenceZt[i] : TargetZ;
                var fov = SequenceFov.Count > i ? SequenceFov[i] : FOV;
                frames.Add(new CameraFrame
                {
                    Fov = StaticUtils.DotFloatString(fov) + "°",
                    OriginX = StaticUtils.DotFloatString(ox),
                    OriginY = StaticUtils.DotFloatString(oy),
                    OriginZ = StaticUtils.DotFloatString(oz),
                    TargetX = StaticUtils.DotFloatString(tx),
                    TargetY = StaticUtils.DotFloatString(ty),
                    TargetZ = StaticUtils.DotFloatString(tz),
                });
            }
            return frames.ToArray();
        }
        set
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (SequenceFov.Count < i) SequenceFov[i] = float.Parse(value[i].Fov[..^1]);
                if (SequenceXo.Count < i) SequenceXo[i] = float.Parse(value[i].OriginX);
                if (SequenceYo.Count < i) SequenceYo[i] = float.Parse(value[i].OriginY);
                if (SequenceZo.Count < i) SequenceZo[i] = float.Parse(value[i].OriginZ);
                if (SequenceXt.Count < i) SequenceXt[i] = float.Parse(value[i].TargetX);
                if (SequenceYt.Count < i) SequenceYt[i] = float.Parse(value[i].TargetY);
                if (SequenceZt.Count < i) SequenceZt[i] = float.Parse(value[i].TargetZ);
            } 
        }
    }

    public FpnFpc(Stream stream)
    {
        Data = new byte[stream.Length];
        stream.ReadExactly(Data, 0, Data.Length);
        NumSequences = StaticUtils.GetInt32(Data, 0x4);
        NumFrames = StaticUtils.GetInt32(Data, 0xC);
        OriginX = StaticUtils.GetFloat(Data, 0x10);
        OriginY = StaticUtils.GetFloat(Data, 0x14);
        OriginZ = StaticUtils.GetFloat(Data, 0x18);
        TargetX = StaticUtils.GetFloat(Data, 0x20);
        TargetY = StaticUtils.GetFloat(Data, 0x24);
        TargetZ = StaticUtils.GetFloat(Data, 0x28);
        FOV = StaticUtils.GetFloat(Data, 0x1C);
        if ((NumSequences == 0) || (NumFrames == 0)) return;
        var offset = 0x30;
        while (offset <= Data.Length)
        {
            if (offset > Data.Length - 1) break;
            var valueType = (ValueIDs)StaticUtils.GetInt32(Data, offset);
            var valueCount = StaticUtils.GetInt32(Data, offset + 4);
            var nextOffset = 0x10 + offset + StaticUtils.GetInt32(Data, offset + 8) * 4;
            offset += 0x10;
            for (var i = 0; i < valueCount; i += 1)
            {
                var f = StaticUtils.GetFloat(Data, offset + i * 4);
                switch (valueType)
                {
                    case ValueIDs.OriginX:
                        SequenceXo.Add(f);
                        break;
                    case ValueIDs.OriginY:
                        SequenceYo.Add(f);
                        break;
                    case ValueIDs.OriginZ:
                        SequenceZo.Add(f);
                        break;
                    case ValueIDs.TargetX:
                        SequenceXt.Add(f);
                        break;
                    case ValueIDs.TargetY:
                        SequenceYt.Add(f);
                        break;
                    case ValueIDs.TargetZ:
                        SequenceZt.Add(f);
                        break;
                    case ValueIDs.Fov:
                        SequenceFov.Add(f);
                        break;
                    default:
                        continue;
                }
            }
            offset = nextOffset;
        }
    }

    public override string ToString()
    {
        return ToString(false);
    }

    public string ToString(bool asCsv)
    {
        var o = "";
        o += $"Frames: {NumFrames}, Sequences: {NumSequences}\n";
        o += $"Field of view: {StaticUtils.DotFloatString(FOV)}\n";
        o += $"Origin:  ({StaticUtils.DotFloatString(OriginX)}; {StaticUtils.DotFloatString(OriginY)}; {StaticUtils.DotFloatString(OriginZ)})\n";
        o += $"Target:  ({StaticUtils.DotFloatString(TargetX)}; {StaticUtils.DotFloatString(TargetY)}; {StaticUtils.DotFloatString(TargetZ)})\n";
        o += "\n";
        if (NumFrames != 0)
        {
            string[] colHeaders = ["Frame", "OriginX", "OriginY", "OriginZ", "TargetX", "TargetY", "TargetZ", "FOV"];
            var rows = new List<string[]>();
            for (var i = 0; i < NumFrames; i++)
            {
                var ox = SequenceXo.Count > i ? SequenceXo[i] : OriginX;
                var oy = SequenceYo.Count > i ? SequenceYo[i] : OriginY;
                var oz = SequenceZo.Count > i ? SequenceZo[i] : OriginZ;
                var tx = SequenceXt.Count > i ? SequenceXt[i] : TargetX;
                var ty = SequenceYt.Count > i ? SequenceYt[i] : TargetY;
                var tz = SequenceZt.Count > i ? SequenceZt[i] : TargetZ;
                var fov = SequenceFov.Count > i ? SequenceFov[i] : FOV;
                rows.Add([
                    (i + 1).ToString(), StaticUtils.DotFloatString(ox), StaticUtils.DotFloatString(oy),
                    StaticUtils.DotFloatString(oz), StaticUtils.DotFloatString(tx), StaticUtils.DotFloatString(ty),
                    StaticUtils.DotFloatString(tz), StaticUtils.DotFloatString(fov)
                ]);
            }
            o += StaticUtils.GenerateTable(colHeaders, rows, asCsv);
        }

        if (NumFrames != 0)
        {
            o += "\n";
        }
        return o;
    }

    /// <summary>
    /// Converts FPC to human-readable XML
    /// </summary>
    public XDocument GenerateXML()
    {
        
        var Frames = new XElement("Animation");
        
        for (var i = 0; i < NumFrames; i++)
        {
            var ox = SequenceXo.Count > i ? SequenceXo[i] : OriginX;
            var oy = SequenceYo.Count > i ? SequenceYo[i] : OriginY;
            var oz = SequenceZo.Count > i ? SequenceZo[i] : OriginZ;
            var tx = SequenceXt.Count > i ? SequenceXt[i] : TargetX;
            var ty = SequenceYt.Count > i ? SequenceYt[i] : TargetY;
            var tz = SequenceZt.Count > i ? SequenceZt[i] : TargetZ;
            var fov = SequenceFov.Count > i ? SequenceFov[i] : FOV;
            Frames.Add(new XElement("Frame", new XElement("Origin", 
                    new XAttribute("X", StaticUtils.DotFloatString(ox)),
                    new XAttribute("Y", StaticUtils.DotFloatString(oy)),
                    new XAttribute("Z", StaticUtils.DotFloatString(oz))),
                new XElement("Target", 
                    new XAttribute("X", StaticUtils.DotFloatString(tx)),
                    new XAttribute("Y", StaticUtils.DotFloatString(ty)),
                    new XAttribute("Z", StaticUtils.DotFloatString(tz))),
                new XElement("FieldOfView", StaticUtils.DotFloatString(fov))));
        }
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("FpcSequence",
            new XElement("Properties",
                new XElement("Origin",
                    new XAttribute("X", StaticUtils.DotFloatString(OriginX)),
                    new XAttribute("Y", StaticUtils.DotFloatString(OriginY)),
                    new XAttribute("Z", StaticUtils.DotFloatString(OriginZ))),
                
                new XElement("Target",
                    new XAttribute("X", StaticUtils.DotFloatString(TargetX)),
                    new XAttribute("Y", StaticUtils.DotFloatString(TargetY)),
                    new XAttribute("Z", StaticUtils.DotFloatString(TargetZ))),
                new XElement("FieldOfView", StaticUtils.DotFloatString(FOV)),
                new XElement("Frames", NumFrames),
                new XElement("Sequences", NumSequences)
            ), Frames));
        return doc;
    }
}