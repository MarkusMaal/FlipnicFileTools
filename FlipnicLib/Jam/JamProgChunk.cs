using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlipnicLib.Vag;

using Syroot.BinaryData;

namespace FlipnicLib.Jam;

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

    public void Read(BinaryStream bs)
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
        for (var i = 0; i < cnt; i++)
        {
            var splitChunk = new JamSplitChunk();
            splitChunk.Read(bs);
            SplitChunks.Add(splitChunk);
        }
    }

    public override string ToString()
    {
        var o = $"""
                 Count: {(CountOrFlag & 0x0F)+1}
                 BaseVolume: {BaseVolume}, Pan: {Pan}
                 LfoTableIndex: {LfoTableIndex}
                 StartNoteRange: {StartNoteRange}, EndNoteRange: {EndNoteRange}
                 
                 """;
        string[] colHeaders =
        [
            "Volume", "Pan", "Note min.", "Note max.", "Base note", "Karaoke", "LFO table idx", "Reverb", "SD_VA_SSA",
            "SD_VP_ADSR1", "SD_VP_ADSR2"
        ];
        List<string[]> rows = [];
        rows.AddRange(SplitChunks.Select(s => (string[]) [s.Volume.ToString(), s.Pan.ToString(),
            s.NoteMin.ToString(), s.NoteMax.ToString(), s.BaseNote.ToString(), s.UnkPitch.ToString(), s.LfoTableIndex.ToString(),
            s.Reverb.ToString(), s.SD_VA_SSA.ToString("X"), s.SD_VP_ADSR1.ToString("X"), s.SD_VP_ADSR2.ToString("X")]));
        return o+StaticUtils.GenerateTable(colHeaders, rows);
    }
}

public class JamSplitChunk
{
    public Note NoteMin { get; set; }
    public Note NoteMax { get; set; }
    public Note BaseNote { get; set; }

    /// <summary>
    /// Pitch correction? No idea, but very important.
    /// </summary>
    public sbyte UnkPitch { get; set; }

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
    public uint SD_VA_SSA { get; set; }

    /// <summary>
    /// <b>Envelope (data 1)</b><br/>
    /// <br/>
    /// PlayStation 2 IOP Library Reference Release 3.0.2 - Sound Libraries<br />
    /// "Low-Level Sound Library - Register Macros" Page 67, SD_VP_ADSR1.<br />
    /// <br />
    /// This is sent through PDISPU2 with:
    /// <code>sceSdSetParam(coreAndVoice | 0x300, (unsigned __int16)ADJ(voice)->SD_VP_ADSR1)</code>
    /// </summary>
    public short SD_VP_ADSR1 { get; set; }

    /// <summary>
    /// <b>Envelope (data 2)</b><br/>
    /// <br/>
    /// PlayStation 2 IOP Library Reference Release 3.0.2 - Sound Libraries<br />
    /// "Low-Level Sound Library - Register Macros" Page 68, SD_VP_ADSR2.<br />
    /// <br />
    /// This is sent through PDISPU2 with:
    /// <code>sceSdSetParam(coreAndVoice | 0x400, ADJ(voice)->SD_VP_ADSR2);</code>
    /// </summary>
    public short SD_VP_ADSR2 { get; set; }

    /// <summary>
    /// In %. 100 is default
    /// </summary>
    public byte Volume { get; set; }

    /// <summary>
    /// 64 is default (center).
    /// </summary>
    public byte Pan { get; set; }

    /// <summary>
    /// Unknown, pitch related? Only used if <see cref="Flags"/> has 0x10, otherwise <see cref="JamProgChunk.UnkPitchRelated_0x04"/> is used.
    /// </summary>
    public byte UnkPitchRelated_0x0E { get; set; }

    /// <summary>
    /// Lfo table index. Only used if <see cref="Flags"/> does NOT have 0x40, otherwise <see cref="JamProgChunk.LfoTableIndex"/> is used. <br />
    /// 0x7F = no lfo in use
    /// </summary>
    public byte LfoTableIndex { get; set; }

    public byte Reverb { get; set; }

    public void Read(BinaryStream bs)
    {
        NoteMin = (Note)bs.Read1Byte();
        NoteMax = (Note)bs.Read1Byte();
        BaseNote = (Note)bs.Read1Byte();
        UnkPitch = bs.ReadSByte();
        Flags = bs.Read1Byte();
        SD_VA_SSA = (uint)((bs.Read1Byte() << 16) | bs.ReadUInt16()); // Game code refers to the offset to audio as Ssa
        SD_VP_ADSR1 = bs.ReadInt16();
        SD_VP_ADSR2 = bs.ReadInt16();
        Volume = bs.Read1Byte();
        Pan = bs.Read1Byte();
        UnkPitchRelated_0x0E = bs.Read1Byte();
        LfoTableIndex = bs.Read1Byte();
        Reverb = bs.Read1Byte();
        bs.Position -= 0x1;
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
        return bs.ReadBytes(0x10 * (int)(lastSampleIndex + 1));
    }

    public override string ToString()
    {
        return $"{NoteMin}->{NoteMax}";
    }
}
