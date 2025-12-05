using Syroot.BinaryData;
using SonyVag = FlipnicLib.Formats.Vag.SonyVag;

namespace FlipnicLib.Formats.Jam;

// Original code from: https://github.com/Nenkai/GT4SoundTool
// Modified by MarkusMaal specifically to support Flipnic's .HD files,
// which use a similar format with some important differences

// Game code refers to this as "JamSplitChunk"
// JAM original documents refer to this as "SplitBlock"
public class JamProgChunk
{
    /// <summary>
    /// If 0xFF = has ALL notes provided for a certain range using <see cref="StartNoteRange" /> and <see cref="EndNoteRange" /> <br />
    /// Otherwise lower 4 bits + 1 <br />
    /// Notes in ranges can still be empty
    /// </summary>
    public byte CountOrFlag { get; set; }

    /// <summary>
    /// Volume of the whole program. Will get multiplied by <see cref="JamSplitChunk.Volume"/>
    /// </summary>
    public byte BaseVolume { get; set; }

    /// <summary>
    /// <b>Maybe</b> pan for the program, but possibly unused
    /// </summary>
    public byte Pan { get; set; }

    /// <summary>
    /// Unknown, Possibly unused
    /// </summary>
    public byte field_0x03 { get; set; }

    /// <summary>
    /// Only used if <see cref="JamSplitChunk.Flags"/> has 0x10
    /// </summary>
    public byte UnkPitchRelated_0x04 { get; set; }

    /// <summary>
    /// Lfo table index. 0x7F = no lfo in use <br/>
    /// Only used if <see cref="JamSplitChunk.Flags"/> has 0x40
    /// </summary>
    public byte LfoTableIndex { get; set; }

    /// <summary>
    /// Starting note range
    /// </summary>
    public Note StartNoteRange { get; set; }

    /// <summary>
    /// End note range
    /// </summary>
    public Note EndNoteRange { get; set; }

    public List<JamSplitChunk> SplitChunks { get; set; } = new List<JamSplitChunk>();

    public void Read(BinaryStream bs, int headerSize)
    {
        CountOrFlag = bs.Read1Byte();
        BaseVolume = bs.Read1Byte();
        Pan = bs.Read1Byte();
        field_0x03 = bs.Read1Byte();
        UnkPitchRelated_0x04 = bs.Read1Byte();
        LfoTableIndex = bs.Read1Byte();
        StartNoteRange = (Note)bs.Read1Byte();
        EndNoteRange = (Note)bs.Read1Byte();
        var cnt = (CountOrFlag > 0x10) ? (CountOrFlag - 0x80) + 1 : CountOrFlag + 1; 
        if (cnt == 0)
        {
            cnt = (int)StartNoteRange - 0x1F; // sound effects
            bs.Position--;
        }
        for (var i = 0; i < cnt; i++)
        {
            var splitChunk = new JamSplitChunk();
            splitChunk.Read(bs, headerSize);
            if (splitChunk.SampleOffset >= 0xFFFF) continue;
            SplitChunks.Add(splitChunk);
        }
    }

    public override string ToString()
    {
        return ToString(false);
    }

    public string ToString(bool asCsv)
    {
        var lfoIdx = LfoTableIndex != 127 ? LfoTableIndex.ToString() : "N/A";
        var o = $"""
                 Count: {(CountOrFlag & 0x0F)+1}
                 BaseVolume: {StaticUtils.DotFloatString((float)Math.Round(BaseVolume/127f*100f, 1))}%, BasePan: {Pan-64} ({(Pan == 64 ? "C" : Pan < 64 ? "L" : "R")}), BasePitch: {UnkPitchRelated_0x04}
                 LfoTableIndex: {lfoIdx}

                 """;
        string[] colHeaders =
        [
            "Volume", "Pan", "Note min.", "Note max.", "Base note", "Fine tune", "Pitch Bend", "LFO index", "Flags", "Offset", "Attack", "Decay", "Sustain", "Release"
        ];
        List<string[]> rows = [];
        rows.AddRange(SplitChunks.Select(s => (string[]) [StaticUtils.DotFloatString((float)Math.Round(s.Volume / 127f * 100f, 1)) + "%", (s.Pan - 64) + " (" + (s.Pan == 64 ? "C" : s.Pan < 64 ? "L" : "R")+ ")",
            StaticUtils.SNote(s.NoteMin), StaticUtils.SNote(s.NoteMax), StaticUtils.SNote(s.BaseNote), s.FineTunePitch.ToString(), (s.PitchBend != 12 ? s.PitchBend.ToString() : "N/A"), (s.LfoTableIndex!=127 ? s.LfoTableIndex.ToString() : "N/A"),
            s.FlagsAsString(), (s.SampleOffset * 8).ToString("X"), $"{StaticUtils.DotFloatString((float)Math.Round(s.Attack, 4))} s",
            $"{StaticUtils.DotFloatString((float)Math.Round(s.Decay, 4))} s",
            $"{StaticUtils.DotFloatString((float)Math.Round(s.Sustain, 4))} s ({StaticUtils.DotFloatString((float)Math.Round(s.SustainL*100.0, 2))} %)",
            $"{StaticUtils.DotFloatString((float)Math.Round(s.Release, 4))} s"]));
        return o+StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }

}

public class JamSplitChunk
{
    public Note NoteMin { get; set; }
    public Note NoteMax { get; set; }
    public Note BaseNote { get; set; }

    /// <summary>
    /// Fine tune pitch adjustment
    /// </summary>
    public sbyte FineTunePitch { get; set; }
    
    /// <summary>
    /// 0x01 = High priority
    /// 0x02 = Noise
    /// 0x10 = Enable pitch bend
    /// 0x20 = Modulation
    /// 0x40 = BreathWaveFromProg
    /// 0x80 = Reverb
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>
    /// <b>Offset of vag data (ssa).</b><br/>
    /// Multiply by 0x10 for sample data offset starting from bd offset.<br />
    /// <br/>
    /// PlayStation 2 IOP Library Reference Release 3.0.2 - Sound Libraries<br />
    /// "Low-Level Sound Library - Register Macros" Page 66, SD_VA_SSA.<br />
    /// <br />
    /// This is sent through PDISPU2 with:
    /// <code>sceSdSetAddr(coreAndVoice | 0x2040, ADJ(voice)->SD_VA_SSA)</code>
    /// </summary>
    public uint SampleOffset { get; set; }

    /// <summary>
    /// In %. 100 is default
    /// </summary>
    public byte Volume { get; set; }

    /// <summary>
    /// 64 is default (center).
    /// </summary>
    public byte Pan { get; set; }

    /// <summary>
    /// Unknown, pitch related? Only used if <see cref="Flags"/> has 0x10, otherwise <see cref="JamProgChunk.FineTunePitch"/> is used.
    /// </summary>
    public byte PitchBend { get; set; }

    /// <summary>
    /// Lfo table index. Only used if <see cref="Flags"/> does NOT have 0x40, otherwise <see cref="JamProgChunk.LfoTableIndex"/> is used. <br />
    /// 0x7F = no lfo in use
    /// </summary>
    public byte LfoTableIndex { get; set; }

    /// <summary>
    /// In ADSR envelope, this is the time it takes for sound to rise from zero to maximum volume after the key is pressed.
    /// </summary>
    public double Attack { get; set; }
    
    /// <summary>
    /// In ADSR envelope, this is the time it takes to attenuate from maximum level to sustain level after the attack phase.
    /// </summary>
    public double Decay { get; set; }
    
    /// <summary>
    /// This is the sustain rate, this is the time it takes to attenuate from sustain level to off (kind of like a second decay). <br />
    /// Not supported by SF2 or DLS.
    /// </summary>
    public double Sustain { get; set; }
    
    /// <summary>
    /// In ADSR envelope, this is the decrease in level, to which the volume ramps during the decay phase.
    /// </summary>
    public double SustainL { get; set; }
    
    /// <summary>
    /// In ADSR envelope, this is the time it takes for sound to reach a volume of zero in seconds after the key is depressed.
    /// </summary>
    public double Release { get; set; }
    
    // Flags
    
    /// <summary>
    /// Sets the VMIXL and VMIXR on the SPU for reverb effects
    /// </summary>
    public bool Reverb => (Flags & 0x80) != 0;
    
    /// <summary>
    /// Use prog chunk's LFO table index
    /// </summary>
    public bool BreathWaveFromProg => (Flags & 0x40) != 0;
    
    /// <summary>
    /// Modulate speed and depth of pitch 
    /// </summary>
    public bool Modulation => (Flags & 0x20) != 0;
    
    /// <summary>
    /// Use pitch value from ProgChunk 
    /// </summary>
    public bool EnablePitchBend => (Flags & 0x10) != 0;
    
    /// <summary>
    /// Set noise shift frequency (SFX only)
    /// </summary>
    public bool Noise => (Flags & 0x02) != 0;
    
    /// <summary>
    /// No idea honestly...
    /// </summary>
    public bool HighPriority => (Flags & 0x01) != 0;

    private enum SustainModes
    {
        LinearIncrement,
        LinearDecrement = 0x02,
        PseudoExponentialIncrement = 0x04,
        PseudoExponentialDecrement = 0x06
    };

    public void Read(BinaryStream bs, int headerSize)
    {
        NoteMin = (Note)bs.Read1Byte();
        NoteMax = (Note)bs.Read1Byte();
        BaseNote = (Note)bs.Read1Byte();
        FineTunePitch = bs.ReadSByte();
        SampleOffset = (uint)(bs.ReadInt16()) & 0xFFFF;
        
        /* Start of ADSR decoding */
        var adsr1 = bs.ReadUInt16();
        var adsr2 = bs.ReadUInt16();
        
        // So the bits I'm using here for ADSR are very likely correct (according to SPU2 documentation).
        //
        // The thing I'm concerned about is the dividers I'm using for Decay and Release
        // these are brute-forced and therefore may be slightly inaccurate.
        //
        // The sustain rate is shown when the user queries information about the
        // HD file, but is completely unused when doing the SF2 conversion.
        // 
        // However, the Sustain Level IS used during the conversion.
        //
        
        var isPseudoExpIncrementMode = (((adsr1 & 0x80) >> 8) == 0x80);
        var attackIdx = (adsr1 & 0x7F00) >> 8;
        Attack = (isPseudoExpIncrementMode ? Constants.PosExpModMs[attackIdx] : Constants.PosLinModeMs[attackIdx]) / 1000.0; // this one I'm fairly confident about
        Decay = Constants.DecayRateMs[(adsr1 & 0xf0) >> 4] / 250.0;
        
        var isExponent = ((adsr2 & 0x20) == 0x20);
        Release = (isExponent ? Constants.ExponentialReleaseMs[adsr2 & 0x1F] : Constants.LinearReleaseMs[adsr2 & 0x1F]) / 250.0; // this one maybe a bit confident 
        SustainL = Constants.SustainLevels[adsr1 & 0x0f];

        var sustainRateIdx = ((adsr2 & 0x1fc0) >> 6);
        var sustainMode = (SustainModes)((adsr2 & 0xe000) >> 13);
        if (sustainRateIdx < Constants.PosLinModeMs.Length)
        {
            Sustain = sustainMode switch
            {
                SustainModes.LinearDecrement => Constants.NegLinModeMs[sustainRateIdx],
                SustainModes.LinearIncrement => Constants.PosLinModeMs[sustainRateIdx],
                SustainModes.PseudoExponentialDecrement => Constants.NegExpModeMs[sustainRateIdx],
                SustainModes.PseudoExponentialIncrement => Constants.PosExpModMs[sustainRateIdx],
                _ => 0.0
            };
        }
        else
        {
            // Ignore sustain rate if the value is 0x7F (which seems to be the default)
            // Otherwise the program would crash due to an index array exception
            Sustain = 0.0;
        }
        
        Sustain /= 1000.0; // value in seconds instead of ms
        /* End of ADSR decoding */
        
        bs.Position++; // skip the Volume Override
        Volume = bs.Read1Byte();
        Pan = (byte)(bs.Read1Byte() + 0xC);
        PitchBend = bs.Read1Byte();
        LfoTableIndex = bs.Read1Byte();
        Flags = bs.Read1Byte();
    }

    public string FlagsAsString()
    {
        return (HighPriority ? "P" : "-") + (Noise ? "N" : "-") + (EnablePitchBend ? "B" : "-") + (Modulation ? "M" : "-") + (BreathWaveFromProg ? "W" : "-") + (Reverb ? "R" : "-");
    }

    public byte[] GetData(BinaryStream bs, out uint loopStart, out uint loopEnd)
    {
        loopStart = 0;
        loopEnd = 0;

        // Size of vag is not provided, we must find it using vag flags
        long basePos = bs.Position;
        long absoluteSsaOffset = basePos;
        bs.Position = absoluteSsaOffset;

        uint lastSampleIndex = 0;
        while (bs.Position < bs.Length)
        {
            byte decodingCoef = bs.Read1Byte();
            var flag = (SonyVag.VagFlag)bs.Read1Byte();

            if (flag == SonyVag.VagFlag.VagfLoopStart)
                loopStart = lastSampleIndex;

            if (flag == SonyVag.VagFlag.VagfLoopLastBlock || flag == SonyVag.VagFlag.VagfLoopEnd)
            {
                if (flag == SonyVag.VagFlag.VagfLoopEnd)
                    loopEnd = lastSampleIndex;

                break;
            }
                

            lastSampleIndex++;

            bs.Position += 0x0E;
        }

        bs.Position = absoluteSsaOffset;
        try
        {
            return bs.ReadBytes(0x10 * (int)(lastSampleIndex + 1));
        }
        catch (EndOfStreamException)
        {
            return [];
        }
    }

    public override string ToString()
    {
        return $"{NoteMin}->{NoteMax}";
    }

}
