using System.Globalization;
using System.Xml.Linq;
using FlipnicLib.Types;

namespace FlipnicLib.Formats;

public class FpnFpc : FormatBase
{
    private readonly List<float> _sequenceXo = [];
    private readonly List<float> _sequenceYo = [];
    private readonly List<float> _sequenceZo = [];

    private readonly List<float> _sequenceXt = [];
    private readonly List<float> _sequenceYt = [];
    private readonly List<float> _sequenceZt = [];
    private readonly List<float> _sequenceFov = [];

    public float Fov { get; set; }
    public float OriginX { get; set; }
    public float OriginY { get; set; }
    public float OriginZ { get; set; }

    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float TargetZ { get; set; }

    //private int TotalFrames;

    private int _numFrames;
    private int _numSequences;
    public string NumFramesStr => _numFrames.ToString();

    private enum ValueIDs
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
        OriginX = 0f;
        OriginY = 0f;
        OriginZ = 0f;
        TargetX = 0f;
        TargetY = 0f;
        TargetZ = 0f;
        Fov = 90;
        _numSequences = 0;
        _numFrames = 0;
    }
    
    public FpnFpc(string filename) : this(File.OpenRead(filename)) {}

    public CameraFrame[] CamFrames
    {
        get
        {
            List<CameraFrame> frames = [];
            for (var i = 0; i < _numFrames; i++)
            {
                var cameraFrame = new CameraFrame();
                if (_sequenceXo.Count > i) cameraFrame.OriginX = _sequenceXo[i];
                if (_sequenceYo.Count > i) cameraFrame.OriginY = _sequenceYo[i];
                if (_sequenceZo.Count > i) cameraFrame.OriginZ = _sequenceZo[i];
                if (_sequenceXt.Count > i) cameraFrame.TargetX = _sequenceXt[i];
                if (_sequenceYt.Count > i) cameraFrame.TargetY = _sequenceYt[i];
                if (_sequenceZt.Count > i) cameraFrame.TargetZ = _sequenceZt[i];
                if (_sequenceFov.Count > i) cameraFrame.Fov = _sequenceFov[i];
                frames.Add(cameraFrame);
            }
            return frames.ToArray();
        }
        set;
    }

    public float UpdateFrame(int i, int ci, float newValue)
    {
        switch (ci)
        {
            case 0 when _sequenceXo.Count > i:
                _sequenceXo[i] = newValue;
                return newValue;
            case 1 when _sequenceYo.Count > i:
                _sequenceYo[i] = newValue;
                return newValue;
            case 2 when _sequenceZo.Count > i:
                _sequenceZo[i] = newValue;
                return newValue;
            case 3 when _sequenceXt.Count > i:
                _sequenceXt[i] = newValue;
                return newValue;
            case 4 when _sequenceYt.Count > i:
                _sequenceYt[i] = newValue;
                return newValue;
            case 5 when _sequenceZt.Count > i:
                _sequenceZt[i] = newValue;
                return newValue;
            case 6 when _sequenceFov.Count > i:
                _sequenceFov[i] = newValue;
                return newValue;
        }
        return 0;
    }

    public FpnFpc(Stream stream)
    {
        var data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        _numSequences = GetInt32(data, 0x4);
        _numFrames = GetInt32(data, 0xC);
        OriginX = GetFloat(data, 0x10);
        OriginY = GetFloat(data, 0x14);
        OriginZ = GetFloat(data, 0x18);
        TargetX = GetFloat(data, 0x20);
        TargetY = GetFloat(data, 0x24);
        TargetZ = GetFloat(data, 0x28);
        Fov = GetFloat(data, 0x1C);
        if ((_numSequences == 0) || (_numFrames == 0)) return;
        var offset = 0x30;
        while (offset <= data.Length)
        {
            if (offset > data.Length - 1) break;
            var valueType = (ValueIDs)GetInt32(data, offset);
            var valueCount = GetInt32(data, offset + 4);
            var nextOffset = 0x10 + offset + GetInt32(data, offset + 8) * 4;
            offset += 0x10;
            for (var i = 0; i < valueCount; i += 1)
            {
                var f = GetFloat(data, offset + i * 4);
                switch (valueType)
                {
                    case ValueIDs.OriginX:
                        _sequenceXo.Add(f);
                        break;
                    case ValueIDs.OriginY:
                        _sequenceYo.Add(f);
                        break;
                    case ValueIDs.OriginZ:
                        _sequenceZo.Add(f);
                        break;
                    case ValueIDs.TargetX:
                        _sequenceXt.Add(f);
                        break;
                    case ValueIDs.TargetY:
                        _sequenceYt.Add(f);
                        break;
                    case ValueIDs.TargetZ:
                        _sequenceZt.Add(f);
                        break;
                    case ValueIDs.Fov:
                        _sequenceFov.Add(f);
                        break;
                    default:
                        continue;
                }
            }
            offset = nextOffset;
        }
    }

    public FpnFpc(XDocument input)
    {
        var properties = input.Root!.Element("Properties");
        var animation = input.Root!.Element("Animation");
        var propertiesOrigin = properties!.Element("Origin");
        var propertiesTarget = properties!.Element("Target");
        var propertiesFov = properties!.Element("FieldOfView");
        var propertiesFrames = properties!.Element("Frames");
        var propertiesSequences = properties!.Element("Sequences");
        OriginX = float.Parse(propertiesOrigin!.Attribute("X")!.Value);
        OriginY = float.Parse(propertiesOrigin!.Attribute("Y")!.Value);
        OriginZ = float.Parse(propertiesOrigin!.Attribute("Z")!.Value);
        TargetX = float.Parse(propertiesTarget!.Attribute("X")!.Value);
        TargetY = float.Parse(propertiesTarget!.Attribute("Y")!.Value);
        TargetZ = float.Parse(propertiesTarget!.Attribute("Z")!.Value);
        Fov = float.Parse(propertiesFov!.Value);
        _numSequences = int.Parse(propertiesSequences!.Value);
        _numFrames = int.Parse(propertiesFrames!.Value);
        for (var i = 0; i < _numFrames; i++)
        {
            var frame = animation!.Elements().ToArray()[i];
            var frameOx = frame.Element("Origin")!.Attribute("X");
            var frameOy = frame.Element("Origin")!.Attribute("Y");
            var frameOz = frame.Element("Origin")!.Attribute("Z");
            var frameTx = frame.Element("Target")!.Attribute("X");
            var frameTy = frame.Element("Target")!.Attribute("Y");
            var frameTz = frame.Element("Target")!.Attribute("Z");
            var frameFov = frame.Element("FieldOfView");
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            if (frameOx != null) _sequenceXo.Add(float.Parse(frameOx.Value, culture));
            if (frameOy != null) _sequenceYo.Add(float.Parse(frameOy.Value, culture));
            if (frameOz != null) _sequenceZo.Add(float.Parse(frameOz.Value, culture));
            if (frameTx != null) _sequenceXt.Add(float.Parse(frameTx.Value, culture));
            if (frameTy != null) _sequenceYt.Add(float.Parse(frameTy.Value, culture));
            if (frameTz != null) _sequenceZt.Add(float.Parse(frameTz.Value, culture));
            if (frameFov != null) _sequenceFov.Add(float.Parse(frameFov.Value, culture));
        }
    }

    public override string ToString()
    {
        return ToString(false);
    }

    public string ToString(bool asCsv)
    {
        var o = "";
        o += $"Frames: {_numFrames}, Sequences: {_numSequences}\n";
        o += $"Field of view: {DotFloatString(Fov)}\n";
        o += $"Origin:  ({DotFloatString(OriginX)}; {DotFloatString(OriginY)}; {DotFloatString(OriginZ)})\n";
        o += $"Target:  ({DotFloatString(TargetX)}; {DotFloatString(TargetY)}; {DotFloatString(TargetZ)})\n";
        o += "\n";
        if (_numFrames != 0)
        {
            string[] colHeaders = ["Frame", "OriginX", "OriginY", "OriginZ", "TargetX", "TargetY", "TargetZ", "FOV"];
            var rows = new List<string[]>();
            for (var i = 0; i < _numFrames; i++)
            {
                var ox = _sequenceXo.Count > i ? _sequenceXo[i] : OriginX;
                var oy = _sequenceYo.Count > i ? _sequenceYo[i] : OriginY;
                var oz = _sequenceZo.Count > i ? _sequenceZo[i] : OriginZ;
                var tx = _sequenceXt.Count > i ? _sequenceXt[i] : TargetX;
                var ty = _sequenceYt.Count > i ? _sequenceYt[i] : TargetY;
                var tz = _sequenceZt.Count > i ? _sequenceZt[i] : TargetZ;
                var fov = _sequenceFov.Count > i ? _sequenceFov[i] : Fov;
                rows.Add([
                    (i + 1).ToString(), DotFloatString(ox), DotFloatString(oy),
                    DotFloatString(oz), DotFloatString(tx), DotFloatString(ty),
                    DotFloatString(tz), DotFloatString(fov)
                ]);
            }
            o += StaticUtils.GenerateTable(colHeaders, rows, asCsv);
        }

        if (_numFrames != 0)
        {
            o += "\n";
        }
        return o;
    }

    /// <summary>
    /// Get the bytes from this object to generate a .FPC file 
    /// </summary>
    /// <returns></returns>
    public byte[] GetBytes()
    {
        var ms = new MemoryStream();
        ms.Write("\0\0\0\0"u8);
        ms.Write(BitConverter.GetBytes(_numSequences));
        ms.WriteByte(0x10);
        ms.WriteByte(0x0);
        ms.WriteByte(0x0);
        ms.WriteByte(0x0);
        ms.Write(BitConverter.GetBytes(_numFrames));
        ms.Write(BitConverter.GetBytes(OriginX));
        ms.Write(BitConverter.GetBytes(OriginY));
        ms.Write(BitConverter.GetBytes(OriginZ));
        ms.Write(BitConverter.GetBytes(Fov));
        ms.Write(BitConverter.GetBytes(TargetX));
        ms.Write(BitConverter.GetBytes(TargetY));
        ms.Write(BitConverter.GetBytes(TargetZ));
        ms.Write("\0\0\0\0"u8);
        if (_sequenceXo.Count > 0) WriteFpcSection(0, _sequenceXo.ToArray(), ms);
        if (_sequenceYo.Count > 0) WriteFpcSection(1, _sequenceYo.ToArray(), ms);
        if (_sequenceZo.Count > 0) WriteFpcSection(2, _sequenceZo.ToArray(), ms);
        if (_sequenceXt.Count > 0) WriteFpcSection(3, _sequenceXt.ToArray(), ms);
        if (_sequenceYt.Count > 0) WriteFpcSection(4, _sequenceYt.ToArray(), ms);
        if (_sequenceZt.Count > 0) WriteFpcSection(5, _sequenceZt.ToArray(), ms);
        if (_sequenceFov.Count > 0) WriteFpcSection(7, _sequenceFov.ToArray(), ms);
        return ms.ToArray();
    }

    private void WriteFpcSection(int id, float[] sequence, Stream ms)
    {
        ms.Write(BitConverter.GetBytes(id));
        ms.Write(BitConverter.GetBytes(sequence.Length));
        var sectionLength = sequence.Length;
        while (sectionLength % 4 != 0)
        {
            sectionLength++;
        }
        ms.Write(BitConverter.GetBytes(sectionLength));
        ms.Write(BitConverter.GetBytes(0x1000));
            
        var endPos = ms.Position + 4 *  (sectionLength);
        foreach (var sv in sequence)
        {
            ms.Write(BitConverter.GetBytes(sv));
        }

        while (ms.Position % 0x10 != 0)
        {
            ms.WriteByte(0);
            ms.WriteByte(16);
            ms.WriteByte(0);
            ms.WriteByte(0);
        }
        ms.Seek(endPos, SeekOrigin.Begin);
    }

    /// <summary>
    /// Converts FPC to human-readable XML
    /// </summary>
    public XDocument GenerateXml()
    {
        
        var frames = new XElement("Animation");
        
        for (var i = 0; i < _numFrames; i++)
        {
            var origin = new XElement("Origin");
            
            if (_sequenceXo.Count > i) origin.Add(new XAttribute("X", DotFloatString(_sequenceXo[i])));
            if (_sequenceYo.Count > i) origin.Add(new XAttribute("Y", DotFloatString(_sequenceYo[i])));
            if (_sequenceZo.Count > i) origin.Add(new XAttribute("Z", DotFloatString(_sequenceZo[i])));
            
            var target = new XElement("Target");
            if (_sequenceXt.Count > i) target.Add(new XAttribute("X", DotFloatString(_sequenceXt[i])));
            if (_sequenceYt.Count > i) target.Add(new XAttribute("Y", DotFloatString(_sequenceYt[i])));
            if (_sequenceZt.Count > i) target.Add(new XAttribute("Z", DotFloatString(_sequenceZt[i])));
            
            var frame = new XElement("Frame", origin, target);
            if (_sequenceFov.Count > i) frame.Add(new XElement("FieldOfView", DotFloatString(_sequenceFov[i])));
            frames.Add(frame);
        }
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("FpcSequence",
            new XElement("Properties",
                new XElement("Origin",
                    new XAttribute("X", DotFloatString(OriginX)),
                    new XAttribute("Y", DotFloatString(OriginY)),
                    new XAttribute("Z", DotFloatString(OriginZ))),
                
                new XElement("Target",
                    new XAttribute("X", DotFloatString(TargetX)),
                    new XAttribute("Y", DotFloatString(TargetY)),
                    new XAttribute("Z", DotFloatString(TargetZ))),
                new XElement("FieldOfView", DotFloatString(Fov)),
                new XElement("Frames", _numFrames),
                new XElement("Sequences", _numSequences)
            ), frames));
        return doc;
    }

    /// <summary>
    /// Create a linearly interpolated animation sequence based on the information provided
    /// </summary>
    /// <param name="startOrigin">Initial origin values (XYZ)</param>
    /// <param name="startTarget">Initial target values (XYZ)</param>
    /// <param name="startFov">Initial field of view (degrees)</param>
    /// <param name="steps">Steps for each value (Xo,Yo,Zo,Xt,Yt,Zt,FOV)</param>
    /// <param name="numFrames">Number of frames to include in the animation</param>
    public void GenerateSequence(float[] startOrigin, float[] startTarget, float startFov, float[] steps, int numFrames)
    {
        _numFrames = numFrames;
        _numSequences = steps.Count(s => s != 0);
        _sequenceXo.Clear();
        _sequenceYo.Clear();
        _sequenceZo.Clear();
        _sequenceFov.Clear();
        _sequenceXt.Clear();
        _sequenceYt.Clear();
        _sequenceZt.Clear();
        if (steps[0] != 0) _sequenceXo.Add(startOrigin[0]);
        if (steps[1] != 0) _sequenceYo.Add(startOrigin[1]);
        if (steps[2] != 0) _sequenceZo.Add(startOrigin[2]);
        if (steps[3] != 0) _sequenceXt.Add(startTarget[0]);
        if (steps[4] != 0) _sequenceYt.Add(startTarget[1]);
        if (steps[5] != 0) _sequenceZt.Add(startTarget[2]);
        if (steps[6] != 0) _sequenceFov.Add(startFov);
        for (var i = 0; i < numFrames - 2; i++)
        {
            if (steps[0] != 0) _sequenceXo.Add(_sequenceXo[^1] + steps[0]);
            if (steps[1] != 0) _sequenceYo.Add(_sequenceYo[^1] + steps[1]);
            if (steps[2] != 0) _sequenceZo.Add(_sequenceZo[^1] + steps[2]);
            if (steps[3] != 0) _sequenceXt.Add(_sequenceXt[^1] + steps[3]);
            if (steps[4] != 0) _sequenceYt.Add(_sequenceYt[^1] + steps[4]);
            if (steps[5] != 0) _sequenceZt.Add(_sequenceZt[^1] + steps[5]);
            if (steps[6] != 0) _sequenceFov.Add(_sequenceFov[^1] + steps[6]);
        }
        if (steps[0] != 0) _sequenceXo.Add(OriginX);
        if (steps[1] != 0) _sequenceYo.Add(OriginY);
        if (steps[2] != 0) _sequenceZo.Add(OriginZ);
        if (steps[3] != 0) _sequenceXt.Add(TargetX);
        if (steps[4] != 0) _sequenceYt.Add(TargetY);
        if (steps[5] != 0) _sequenceZt.Add(TargetZ);
        if (steps[6] != 0) _sequenceFov.Add(Fov);
    }

    public float[] GetOrigin()
    {
        return [OriginX, OriginY, OriginZ];
    }

    public float[] GetTarget()
    {
        return [TargetX, TargetY, TargetZ];
    }

    public float GetFov()
    {
        return Fov;
    }
}