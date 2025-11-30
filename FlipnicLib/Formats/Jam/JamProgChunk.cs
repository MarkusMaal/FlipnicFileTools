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
            "Volume", "Pan", "Note min.", "Note max.", "Base note", "Fine tune", "LFO index", "Flags", "Offset", "Attack", "Decay", "Sustain", "Release"
        ];
        List<string[]> rows = [];
        rows.AddRange(SplitChunks.Select(s => (string[]) [StaticUtils.DotFloatString((float)Math.Round(s.Volume / 127f * 100f, 1)) + "%", (s.Pan - 64) + " (" + (s.Pan == 64 ? "C" : s.Pan < 64 ? "L" : "R")+ ")",
            StaticUtils.SNote(s.NoteMin), StaticUtils.SNote(s.NoteMax), StaticUtils.SNote(s.BaseNote), s.FineTunePitch.ToString(), (s.LfoTableIndex!=127 ? s.LfoTableIndex.ToString() : "N/A"),
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

    /* 0x01 = ?? 
     * 0x02 = SetNoiseShiftFrequency, // SE only
     * 0x10 = UseProgChunkForUnkPitchValue - Use prog chunk's unk pitch (?) value
     * 0x20 = PitchModulateSpeedAndDepth - 
     * 0x40 = UseProgChunkForLfoTableIndex - Use prog chunk's lfo table index
     * 0x80 = mixing? maybe for reverb - sets SD_S_VMIXL & SD_S_VMIXR

       don't think there's more
    */
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

    public double Attack { get; set; }
    public double Decay { get; set; }
    public double Sustain { get; set; }
    public double SustainL { get; set; }
    public double Release { get; set; }
    
    // Constants from SPU2OverviewManual.pdf
    private readonly double[] linearReleaseMs = [0.04,0.09,0.18,0.36,0.73,1.5,2.9,5.8,12,23,46,93,190,370,740,1500,3000,5900,12000,24000,48000,95000,190000,380000,760000,1520000,3040000,double.NaN,double.NaN,double.PositiveInfinity];
    private readonly double[] exponentialReleaseMs = [0.07,0.18,0.39,0.81,1.6,3.3,6.7,13,27,53,110,210,430,860,1700,3400,6800,14000,27000,55000,109000,219000,438000,876000,1752000,3504000,7008000,double.NaN,double.NaN,double.PositiveInfinity];

    private readonly double[] decayRateMs = [0.07, 0.18, 0.39, 0.81, 1.6, 3.3, 6.7, 13, 27, 53, 110, 210, 430, 860, 1700, 3400];
    private readonly double[] sustainLevels = [0.0625d, 0.125d, 0.1875d, 0.25d, 0.3125d, 0.375d, 0.4375d, 0.5d, 0.5625d, 0.625d, 0.6875d, 0.75d, 0.8125d, 0.875d,  0.9375d, 1.0];

    private readonly double[] posLinModeMs = [0.05,0.06,0.07,0.09,0.1,0.12,0.15,0.18,0.21,0.24,0.29,0.36,0.41,0.48,0.58,0.73,0.83,0.97,1.2,1.5,1.7,1.9,2.3,2.9,3.3,3.9,4.6,5.8,6.6,7.7,9.3,12,13,15,19,23,27,31,37,
                                              46,53,62,74,93,110,120,150,190,210,250,300,370,420,500,590,740,850,990,1200,1500,1700,2000,2400,3000,3400,4000,4800,5900,6800,7900,9500,12000,14000,16000,19000,24000,27000,32000,
                                              38000,48000,54000,63000,76000,95000,109000,127000,152000,190000,218000,254000,304000,380000,436000,508000,608000,760000,872000,1016000,1216000,1520000,1744000,2032000,2432000,3040000,3488000,4064000,4864000,6080000,
                                              double.NaN,double.NaN,double.NaN,double.PositiveInfinity];

    private readonly double[] posExpModMs =
    [
        0.09, 0.11, 0.13, 0.16, 0.18, 0.21, 0.25, 0.32, 0.36, 0.42, 0.51, 0.64, 0.73, 0.85, 1, 1.3, 1.5, 1.7, 2, 2.5,
        2.9, 3.4, 4.1, 5.1, 5.8, 6.8, 8.1, 10, 12, 14, 16, 20, 23, 27, 33, 41,
        46, 54, 65, 81, 93, 110, 130, 160, 190, 220, 260, 330, 370, 430, 520, 650, 740, 870, 1000, 1300, 1500, 1700,
        2100, 2600, 3000, 3500, 4200, 5200, 5900, 6900, 8300, 10000, 12000, 14000, 17000, 21000,
        24000, 28000, 33000, 42000, 48000, 55000, 67000, 83000, 95000, 111000, 133000, 166000, 190000, 222000, 266000,
        333000, 380000, 444000, 532000, 666000, 760000, 888000, 1064000, 1332000, 1520000, 1776000, 2128000, 2664000,
        double.NaN, double.NaN, double.NaN, double.PositiveInfinity
    ];

    private readonly double[] negLinModeMs = 
    [
        0.04, 0.05, 0.06, 0.07, 0.09, 0.1, 0.12, 0.15, 0.18, 0.21, 0.24, 0.29, 0.36, 0.41, 0.48, 0.58, 0.73, 0.83, 0.97, 1.2, 1.5,
        1.7, 1.9, 2.3, 2.9, 3.3, 3.9, 4.6, 5.8, 6.6, 7.7, 9.3, 12, 13, 15, 19, 23, 27, 31,
        37, 46, 53, 62, 74, 93, 110, 120, 150, 190, 210, 250, 300, 370, 420, 500, 590, 740, 850, 990, 1200, 1500, 1700, 2000, 2400,
        3000, 3400, 4000, 4800, 5900, 6800, 7900, 9500, 12000, 14000, 16000, 19000, 24000, 27000,
        32000, 38000, 48000, 54000, 63000, 76000, 95000, 109000, 127000, 152000, 190000, 218000, 254000, 304000, 380000, 436000,
        508000, 608000, 760000, 872000, 1016000, 1216000, 1520000, 1744000, 2032000, 2432000, 3040000, 3488000, 4064000, 4864000,
        double.NaN, double.NaN, double.NaN, double.PositiveInfinity
    ];

    private readonly double[] negExpModeMs =
    [
        0.07, 0.09, 0.11, 0.14, 0.18, 0.21, 0.25, 0.31, 0.39, 0.45, 0.53, 0.64, 0.81, 0.93, 1.1, 1.3, 1.6, 1.9, 2.2, 2.6, 3.3, 3.8,
        4.4, 5.3, 6.7, 7.6, 8.9, 11, 13, 15, 18, 21, 27, 31, 36, 43, 53, 61, 71,
        86, 110, 120, 140, 170, 210, 240, 290, 340, 430, 490, 570, 680, 860, 980, 1100, 1400, 1700, 2000, 2300, 2700, 3400, 3900, 4600,
        5500, 6800, 7800, 9100, 11000, 14000, 16000, 18000, 22000, 27000, 31000, 36000, 44000, 55000, 63000,
        73000, 88000, 109000, 125000, 146000, 175000, 219000, 250000, 292000, 350000, 438000, 500000, 584000, 700000, 876000, 1000000,
        1168000, 1400000, 1752000, 2000000, 2336000, 2800000, 3504000, 4000000, 4672000, 5600000, 7008000, 8000000, 9344000, 11200000,
        double.NaN, double.NaN, double.NaN, double.PositiveInfinity
    ];
    
    // Flags
    public bool HighPriority => (Flags & 0x80) != 0;
    public bool Noise => (Flags & 0x40) != 0;
    public bool EnablePitchBend => (Flags & 0x08) != 0;
    public bool Modulation => (Flags & 0x04) != 0;
    public bool BreathWaveFromProg => (Flags & 0x02) != 0;
    public bool Reverb => (Flags & 0x01) != 0;

    private enum SustainModes
    {
        LinearIncrement,
        Reserved1,
        LinearDecrement,
        Reserved2,
        PseudoExponentialIncrement,
        Reserved3,
        PseudoExponentialDecrement,
        Reserved4
    };

    public void Read(BinaryStream bs, int headerSize)
    {
        NoteMin = (Note)bs.Read1Byte();
        NoteMax = (Note)bs.Read1Byte();
        BaseNote = (Note)bs.Read1Byte();
        FineTunePitch = bs.ReadSByte();
        SampleOffset = (uint)(bs.ReadInt16()) & 0xFFFF;
        var adsr1 = bs.ReadUInt16();
        var adsr2 = bs.ReadUInt16();
        
        var isPseudoExpIncrementMode = (((adsr1 & 0x80) >> 8) == 0x80);
        var attackIdx = (adsr1 & 0x7F00) >> 8;
        Attack = (isPseudoExpIncrementMode ? posExpModMs[attackIdx] : posLinModeMs[attackIdx]) / 1000.0; // this one I'm fairly confident about
        Decay = decayRateMs[(adsr1 & 0xf0) >> 4] / 128.0;
        var isExponent = ((adsr2 & 0x20) == 0x20);
        Release = (isExponent ? exponentialReleaseMs[adsr2 & 0x1F] : linearReleaseMs[adsr2 & 0x1F]) / 140.0; // this one maybe a bit confident 
        SustainL = sustainLevels[adsr1 & 0x0f];

        var sustainRateIdx = ((adsr1 & 0x3f8) >> 3);
        var sustainMode = (SustainModes)((adsr1 & 0x7));

        switch (sustainMode)
        {
            case SustainModes.LinearDecrement:
                Sustain = negLinModeMs[sustainRateIdx];
                break;
            case SustainModes.LinearIncrement:
                Sustain = posLinModeMs[sustainRateIdx];
                break;
            case SustainModes.PseudoExponentialDecrement:
                Sustain = negExpModeMs[sustainRateIdx];
                break;
            case SustainModes.PseudoExponentialIncrement:
                Sustain = posExpModMs[sustainRateIdx];
                break;
            case SustainModes.Reserved1:
            case SustainModes.Reserved2:
            case SustainModes.Reserved3:
            case SustainModes.Reserved4:
            default:
                Sustain = 0.0;
                break;
        }
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
