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
                var ox = _sequenceXo.Count > i ? _sequenceXo[i] : _originX;
                var oy = _sequenceYo.Count > i ? _sequenceYo[i] : _originY;
                var oz = _sequenceZo.Count > i ? _sequenceZo[i] : _originZ;
                var tx = _sequenceXt.Count > i ? _sequenceXt[i] : _targetX;
                var ty = _sequenceYt.Count > i ? _sequenceYt[i] : _targetY;
                var tz = _sequenceZt.Count > i ? _sequenceZt[i] : _targetZ;
                var fov = _sequenceFov.Count > i ? _sequenceFov[i] : _fov;
                frames.Add(new CameraFrame
                {
                    Fov = DotFloatString(fov) + "°",
                    OriginX = DotFloatString(ox),
                    OriginY = DotFloatString(oy),
                    OriginZ = DotFloatString(oz),
                    TargetX = DotFloatString(tx),
                    TargetY = DotFloatString(ty),
                    TargetZ = DotFloatString(tz),
                });
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
    /// Converts FPC to human-readable XML
    /// </summary>
    public XDocument GenerateXml()
    {
        
        var frames = new XElement("Animation");
        
        for (var i = 0; i < _numFrames; i++)
        {
            var ox = _sequenceXo.Count > i ? _sequenceXo[i] : _originX;
            var oy = _sequenceYo.Count > i ? _sequenceYo[i] : _originY;
            var oz = _sequenceZo.Count > i ? _sequenceZo[i] : _originZ;
            var tx = _sequenceXt.Count > i ? _sequenceXt[i] : _targetX;
            var ty = _sequenceYt.Count > i ? _sequenceYt[i] : _targetY;
            var tz = _sequenceZt.Count > i ? _sequenceZt[i] : _targetZ;
            var fov = _sequenceFov.Count > i ? _sequenceFov[i] : _fov;
            frames.Add(new XElement("Frame", new XElement("Origin", 
                    new XAttribute("X", DotFloatString(ox)),
                    new XAttribute("Y", DotFloatString(oy)),
                    new XAttribute("Z", DotFloatString(oz))),
                new XElement("Target", 
                    new XAttribute("X", DotFloatString(tx)),
                    new XAttribute("Y", DotFloatString(ty)),
                    new XAttribute("Z", DotFloatString(tz))),
                new XElement("FieldOfView", DotFloatString(fov))));
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