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
        var o = $"""
                 Count: {(CountOrFlag & 0x0F)+1}
                 BaseVolume: {StaticUtils.DotFloatString((float)Math.Round(BaseVolume/127f*100f, 1))}%, BasePan: {Pan-64} ({(Pan == 64 ? "C" : Pan < 64 ? "L" : "R")}), BasePitch: {UnkPitchRelated_0x04}
                 LfoTableIndex: {LfoTableIndex}

                 """;
        string[] colHeaders =
        [
            "Volume", "Pan", "Note min.", "Note max.", "Base note", "Fine tune", "LFO index", "Flags", "Offset"
        ];
        List<string[]> rows = [];
        rows.AddRange(SplitChunks.Select(s => (string[]) [StaticUtils.DotFloatString((float)Math.Round(s.Volume / 127f * 100f, 1)) + "%", (s.Pan - 64) + " (" + (s.Pan == 64 ? "C" : s.Pan < 64 ? "L" : "R")+ ")",
            StaticUtils.SNote(s.NoteMin), StaticUtils.SNote(s.NoteMax), StaticUtils.SNote(s.BaseNote), s.FineTunePitch.ToString(), s.LfoTableIndex.ToString(),
            s.FlagsAsString(), (s.SampleOffset * 8).ToString("X")]));
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

    public short ADSR1 { get; set; }
    public short ADSR2 { get; set; }

    public double Attack { get; set; }
    public double Decay { get; set; }
    public double Sustain { get; set; }
    public double SustainL { get; set; }
    public double Release { get; set; }

    // Flags
    public bool HighPriority => (Flags & 0x80) != 0;
    public bool Noise => (Flags & 0x40) != 0;
    public bool EnablePitchBend => (Flags & 0x08) != 0;
    public bool Modulation => (Flags & 0x04) != 0;
    public bool BreathWaveFromProg => (Flags & 0x02) != 0;
    public bool Reverb => (Flags & 0x01) != 0;

    public void Read(BinaryStream bs, int headerSize)
    {
        NoteMin = (Note)bs.Read1Byte();
        NoteMax = (Note)bs.Read1Byte();
        BaseNote = (Note)bs.Read1Byte();
        FineTunePitch = bs.ReadSByte();
        SampleOffset = (uint)(bs.ReadInt16()) & 0xFFFF;
        ADSR2 = bs.ReadInt16();
        ADSR1 = bs.ReadInt16();

        bs.Position++; // skip the Volume Override
        Volume = bs.Read1Byte();
        Pan = (byte)(bs.Read1Byte() + 0xC);
        PitchBend = bs.Read1Byte();
        LfoTableIndex = bs.Read1Byte();
        Flags = bs.Read1Byte();
    }

    public void ConvertADSR(byte[] LfoTable)
    {
        byte Am = (byte)((ADSR1 & 0x8000) >> 15);    // if 1, then Exponential, else linear
        byte Ar = (byte)((ADSR1 & 0x7F00) >> 8);
        byte Dr = (byte)((ADSR1 & 0x00F0) >> 4);
        byte Sl = (byte)(ADSR1 & 0x000F);
        byte Rm = (byte)((ADSR2 & 0x0020) >> 5);
        byte Rr = (byte)(ADSR2 & 0x001F);

        // The following are unimplemented in conversion (because DLS and SF2 do not support Sustain Rate)
        byte Sm = (byte)((ADSR2 & 0x8000) >> 15);
        byte Sd = (byte)((ADSR2 & 0x4000) >> 14);
        byte Sr = (byte)((ADSR2 >> 6) & 0x7F);

        var adsrObj = PsxConvADSR(Am, Ar, Dr, Sl, Sm, Sd, Sr, Rm, Rr, true, LfoTable);
        Attack = adsrObj.attack_time;
        Decay = adsrObj.decay_time;
        Sustain = adsrObj.sustain_time;
        SustainL = adsrObj.sustain_level;
        Release = adsrObj.release_time;
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

    private static int RoundToZero(double val)
    {
        return (int)(val < 0 ? Math.Ceiling(val) : Math.Floor(val));
    }

    private static double LinearAmpDecayTimeToLinDBDecayTime(double secondsToFullAtten,
                                          double targetDb_LeastSquares = 70,
                                          double targetDb_InitialSlope = 140)
    {
        if (secondsToFullAtten <= 0.0) return 0.0;

        const double ln10 = 2.302585092994046;
        double k_short = targetDb_InitialSlope / (20.0 / ln10);
        double k_long = targetDb_LeastSquares * ln10 / 45.0;

        // Knee near temporal integration (100–150 ms). p controls sharpness.
        const double T_knee = 0.12; // seconds
        const double p = 2.0;

        double x = secondsToFullAtten / T_knee;
        double w = 1.0 / (1.0 + Math.Pow(x, p)); // w≈1 for very short; →0 for long

        return secondsToFullAtten * (w * k_short + (1.0 - w) * k_long);
    }


    public static ADSR PsxConvADSR(
       byte Am, byte Ar, byte Dr, byte Sl,
       byte Sm, byte Sd, byte Sr, byte Rm, byte Rr, bool bPS2,
       byte[] RateTable)
    {
        var realADSR = new ADSR();

        // Validate ranges
        if (((Am & ~0x01) != 0) ||
            ((Ar & ~0x7F) != 0) ||
            ((Dr & ~0x0F) != 0) ||
            ((Sl & ~0x0F) != 0) ||
            ((Rm & ~0x01) != 0) ||
            ((Rr & ~0x1F) != 0) ||
            ((Sm & ~0x01) != 0) ||
            ((Sd & ~0x01) != 0) ||
            ((Sr & ~0x7F) != 0))
        {
            return null;
        }

        double sampleRate = bPS2 ? 48000.0 : 44100.0;
        double samples = 0;
        int l;


        // Attack time
        if ((Ar ^ 0x7F) < 0x10)
            Ar = 0;

        if (Am == 0)
        {
            uint rate = RateTable[RoundToZero((Ar ^ 0x7F) - 0x10)];
            samples = Math.Ceiling(0x7FFFFFFF / (double)rate);
        }
        else if (Am == 1)
        {
            uint rate = RateTable[RoundToZero((Ar ^ 0x7F) - 0x10)];
            samples = 0x60000000 / (double)rate;
            uint remainder = 0x60000000 % rate;
            rate = RateTable[RoundToZero((Ar ^ 0x7F) - 0x18)];
            samples += Math.Ceiling(Math.Max(0, 0x1FFFFFFF - remainder) / (double)rate);
        }

        realADSR.attack_time = samples / sampleRate;

        // Decay time
        long envelope_level = 0x7FFFFFFF;
        bool bSustainLevFound = false;
        uint realSustainLevel = 0;

        for (l = 0; envelope_level > 0; l++)
        {
            if (4 * (Dr ^ 0x1F) < 0x18)
                Dr = 0;

            int idxBase = RoundToZero((4 * (Dr ^ 0x1F)) - 0x18);
            int shift = (int)((envelope_level >> 28) & 0x7);

            envelope_level -= RateTable[idxBase + shift switch
            {
                0 => 0,
                1 => 4,
                2 => 6,
                3 => 8,
                4 => 9,
                5 => 10,
                6 => 11,
                7 => 12,
                _ => 0
            }];

            if (!bSustainLevFound && ((envelope_level >> 27) & 0xF) <= Sl)
            {
                realSustainLevel = (uint)envelope_level;
                bSustainLevFound = true;
            }
        }

        samples = l;
        realADSR.decay_time = samples / sampleRate;

        // Sustain time
        envelope_level = 0x7FFFFFFF;
        if (Sd == 0)
        {
            realADSR.sustain_time = -1;
        }
        else if (Sr == 0x7F)
        {
            realADSR.sustain_time = -1;
        }
        else
        {
            if (Sm == 0)
            {
                uint rate = RateTable[RoundToZero((Sr ^ 0x7F) - 0x0F)];
                samples = Math.Ceiling(0x7FFFFFFF / (double)rate);
            }
            else
            {
                l = 0;
                while (envelope_level > 0)
                {
                    long envelope_level_diff = 0;
                    long envelope_level_target = 0;

                    switch ((envelope_level >> 28) & 0x7)
                    {
                        case 0: envelope_level_target = 0x00000000; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 0)]; break;
                        case 1: envelope_level_target = 0x0FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 4)]; break;
                        case 2: envelope_level_target = 0x1FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 6)]; break;
                        case 3: envelope_level_target = 0x2FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 8)]; break;
                        case 4: envelope_level_target = 0x3FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 9)]; break;
                        case 5: envelope_level_target = 0x4FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 10)]; break;
                        case 6: envelope_level_target = 0x5FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 11)]; break;
                        case 7: envelope_level_target = 0x6FFFFFFF; envelope_level_diff = RateTable[RoundToZero((Sr ^ 0x7F) - 0x1B + 12)]; break;
                    }

                    long steps = (envelope_level - envelope_level_target + (envelope_level_diff - 1)) / envelope_level_diff;
                    envelope_level -= envelope_level_diff * steps;
                    l += (int)steps;
                }

                samples = l;
            }

            double timeInSecs = samples / sampleRate;
            realADSR.sustain_time = LinearAmpDecayTimeToLinDBDecayTime(timeInSecs, 0x800);
        }

        // Sustain level
        if (Sl == 0)
            realSustainLevel = 0x07FFFFFF;

        realADSR.sustain_level = realSustainLevel / (double)0x7FFFFFFF;

        // Decay/sustain heuristic adjustment
        if ((realADSR.decay_time < 2 || (Dr == 0x0F && Sl >= 0x0C)) && Sr < 0x7E && Sd == 1)
        {
            realADSR.sustain_level = 0;
            realADSR.decay_time = realADSR.sustain_time;
        }

        // Release time
        envelope_level = 0x7FFFFFFF;

        if (Rm == 0)
        {
            uint rate = RateTable[RoundToZero((4 * (Rr ^ 0x1F)) - 0x0C)];
            samples = rate != 0 ? Math.Ceiling(envelope_level / (double)rate) : 0;
        }
        else if (Rm == 1)
        {
            if ((Rr ^ 0x1F) * 4 < 0x18)
                Rr = 0;

            for (l = 0; envelope_level > 0; l++)
            {
                if (envelope_level == 0xFFFFFFF) break;
                int idx = RoundToZero((4 * (Rr ^ 0x1F)) - 0x18);
                int shift = (int)((envelope_level >> 28) & 0x7);
                envelope_level -= RateTable[idx + shift switch
                {
                    0 => 0,
                    1 => 4,
                    2 => 6,
                    3 => 8,
                    4 => 9,
                    5 => 10,
                    6 => 11,
                    7 => 12,
                    _ => 0
                }];
            }

            samples = l;
        }

        double releaseTimeSecs = samples / sampleRate;
        realADSR.release_time = LinearAmpDecayTimeToLinDBDecayTime(releaseTimeSecs, 0x800);
        return realADSR;
    }

    public class ADSR
    {
        public double attack_time;
        public double decay_time;
        public double sustain_time;
        public double sustain_level;
        public double release_time;
    }

}
