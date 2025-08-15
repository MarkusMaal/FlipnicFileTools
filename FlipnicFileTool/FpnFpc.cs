using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace FlipnicFileTool;

public class FpnFpc
{
    List<float> SequenceXo = [];
    List<float> SequenceYo = [];
    List<float> SequenceZo = [];
    
    List<float> SequenceXt = [];
    List<float> SequenceYt = [];
    List<float> SequenceZt = [];
    List<float> SequenceFov = [];

    private float FOV;
    float OriginX;
    float OriginY;
    float OriginZ;
    
    float TargetX;
    float TargetY;
    float TargetZ;

    private int TotalFrames;

    private byte[] Data;

    private int NumFrames;
    private int NumSequences;

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

    public FpnFpc(string filename)
    {
        Data = File.ReadAllBytes(filename);
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

            if (!Program.SimpleOutput)
            {
                o += StaticUtils.GenerateTable(colHeaders, rows);
            }
            else
            {
                foreach (var row in rows)
                {
                    for (var i = 0; i < colHeaders.Length; i++)
                    {
                        if (Program.LowMem)
                        {
                            Console.Write(colHeaders[i] + ": " + row[i]);
                            if (i != colHeaders.Length - 1)
                            {
                                Console.Write("; ");
                            }   
                        }
                        else
                        {
                            o += colHeaders[i] + ": " + row[i];
                            if (i != colHeaders.Length - 1)
                            {
                                o += "; ";
                            }   
                        }
                    }

                    if (Program.LowMem)
                    {
                        Console.WriteLine();
                    }
                    else
                    {
                        o += "\n";
                    }
                }
            }
        }

        if (NumFrames != 0)
        {
            o += "\n";
        }
        return o;
    }

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