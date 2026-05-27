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

    private readonly float _fov;
    private readonly float _originX;
    private readonly float _originY;
    private readonly float _originZ;

    private readonly float _targetX;
    private readonly float _targetY;
    private readonly float _targetZ;

    public string FoVf => DotFloatString(_fov);
    public string OriginXf => DotFloatString(_originX);
    public string OriginYf => DotFloatString(_originY);
    public string OriginZf => DotFloatString(_originZ);
    public string TargetXf => DotFloatString(_targetX);
    public string TargetYf => DotFloatString(_targetY);
    public string TargetZf => DotFloatString(_targetZ);

    //private int TotalFrames;

    private readonly int _numFrames;
    private readonly int _numSequences;
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
        _originX = 0f;
        _originY = 0f;
        _originZ = 0f;
        _targetX = 0f;
        _targetY = 0f;
        _targetZ = 0f;
        _fov = 90;
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
                if (_sequenceXo.Count > i) cameraFrame.OriginX = DotFloatString(_sequenceXo[i]);
                if (_sequenceYo.Count > i) cameraFrame.OriginY = DotFloatString(_sequenceYo[i]);
                if (_sequenceZo.Count > i) cameraFrame.OriginZ = DotFloatString(_sequenceZo[i]);
                if (_sequenceXt.Count > i) cameraFrame.TargetX = DotFloatString(_sequenceXt[i]);
                if (_sequenceYt.Count > i) cameraFrame.TargetY = DotFloatString(_sequenceYt[i]);
                if (_sequenceZt.Count > i) cameraFrame.TargetZ = DotFloatString(_sequenceZt[i]);
                if (_sequenceFov.Count > i) cameraFrame.Fov = DotFloatString(_sequenceFov[i]) + "°";
                frames.Add(cameraFrame);
            }
            return frames.ToArray();
        }
        set
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (_sequenceFov.Count < i) _sequenceFov[i] = float.Parse(value[i].Fov![..^1]);
                if (_sequenceXo.Count < i) _sequenceXo[i] = float.Parse(value[i].OriginX!);
                if (_sequenceYo.Count < i) _sequenceYo[i] = float.Parse(value[i].OriginY!);
                if (_sequenceZo.Count < i) _sequenceZo[i] = float.Parse(value[i].OriginZ!);
                if (_sequenceXt.Count < i) _sequenceXt[i] = float.Parse(value[i].TargetX!);
                if (_sequenceYt.Count < i) _sequenceYt[i] = float.Parse(value[i].TargetY!);
                if (_sequenceZt.Count < i) _sequenceZt[i] = float.Parse(value[i].TargetZ!);
            } 
        }
    }

    public FpnFpc(Stream stream)
    {
        var data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        _numSequences = GetInt32(data, 0x4);
        _numFrames = GetInt32(data, 0xC);
        _originX = GetFloat(data, 0x10);
        _originY = GetFloat(data, 0x14);
        _originZ = GetFloat(data, 0x18);
        _targetX = GetFloat(data, 0x20);
        _targetY = GetFloat(data, 0x24);
        _targetZ = GetFloat(data, 0x28);
        _fov = GetFloat(data, 0x1C);
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
        _originX = float.Parse(propertiesOrigin!.Attribute("X")!.Value);
        _originY = float.Parse(propertiesOrigin!.Attribute("Y")!.Value);
        _originZ = float.Parse(propertiesOrigin!.Attribute("Z")!.Value);
        _targetX = float.Parse(propertiesTarget!.Attribute("X")!.Value);
        _targetY = float.Parse(propertiesTarget!.Attribute("Y")!.Value);
        _targetZ = float.Parse(propertiesTarget!.Attribute("Z")!.Value);
        _fov = float.Parse(propertiesFov!.Value);
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
        o += $"Field of view: {DotFloatString(_fov)}\n";
        o += $"Origin:  ({DotFloatString(_originX)}; {DotFloatString(_originY)}; {DotFloatString(_originZ)})\n";
        o += $"Target:  ({DotFloatString(_targetX)}; {DotFloatString(_targetY)}; {DotFloatString(_targetZ)})\n";
        o += "\n";
        if (_numFrames != 0)
        {
            string[] colHeaders = ["Frame", "OriginX", "OriginY", "OriginZ", "TargetX", "TargetY", "TargetZ", "FOV"];
            var rows = new List<string[]>();
            for (var i = 0; i < _numFrames; i++)
            {
                var ox = _sequenceXo.Count > i ? _sequenceXo[i] : _originX;
                var oy = _sequenceYo.Count > i ? _sequenceYo[i] : _originY;
                var oz = _sequenceZo.Count > i ? _sequenceZo[i] : _originZ;
                var tx = _sequenceXt.Count > i ? _sequenceXt[i] : _targetX;
                var ty = _sequenceYt.Count > i ? _sequenceYt[i] : _targetY;
                var tz = _sequenceZt.Count > i ? _sequenceZt[i] : _targetZ;
                var fov = _sequenceFov.Count > i ? _sequenceFov[i] : _fov;
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
        ms.Write(BitConverter.GetBytes(_originX));
        ms.Write(BitConverter.GetBytes(_originY));
        ms.Write(BitConverter.GetBytes(_originZ));
        ms.Write(BitConverter.GetBytes(_fov));
        ms.Write(BitConverter.GetBytes(_targetX));
        ms.Write(BitConverter.GetBytes(_targetY));
        ms.Write(BitConverter.GetBytes(_targetZ));
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
        ms.Write(BitConverter.GetBytes(_numFrames));
        ms.Write(BitConverter.GetBytes(0x1000));
            
        var endPos = ms.Position + 4 *  _numFrames;
        foreach (var sv in sequence)
        {
            ms.Write(BitConverter.GetBytes(sv));
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
                    new XAttribute("X", DotFloatString(_originX)),
                    new XAttribute("Y", DotFloatString(_originY)),
                    new XAttribute("Z", DotFloatString(_originZ))),
                
                new XElement("Target",
                    new XAttribute("X", DotFloatString(_targetX)),
                    new XAttribute("Y", DotFloatString(_targetY)),
                    new XAttribute("Z", DotFloatString(_targetZ))),
                new XElement("FieldOfView", DotFloatString(_fov)),
                new XElement("Frames", _numFrames),
                new XElement("Sequences", _numSequences)
            ), frames));
        return doc;
    }
}