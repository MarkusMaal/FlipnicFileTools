
using System.Runtime.CompilerServices;
using Syroot.BinaryData;
using FlipnicLib.Midi.Meta;
using FlipnicLib.Jam;

namespace FlipnicLib.Midi;

// Sqt is handled by game code through one of the two sequencers - SqSequencer, for menu bgm.
// Sq is somewhat named in the JAM sdk docs.

// Spu sequence Table? Sound sequence table?
public class Midi
{
    public MTrk Track { get; set; }

    public void Read(string fileName)
    {
        Read(File.OpenRead(fileName));
    }
    public void Read(Stream stream)
    {
        using var fs = stream;
        using var bs = new BinaryStream(fs, ByteConverter.Little);

        var sssq = new MTrk();
        sssq.Read(bs);

        Track = sssq;
    }

    // BIG NOTE: 7bit int implementation is different!
    public static long readVariable(BinaryStream bs)
    {
        long result = 0;
        byte @byte;

        do
        {
            @byte = bs.Read1Byte();
            result = (result << 7) + (@byte & 0x7F);
        }
        while ((@byte & 0x80) != 0);
        return result;
    }

    public override string ToString()
    {
        string[] colHeaders = ["Delta", "Event", "Status", "Channel"];
        List<string[]> rows = [];
        var o = $"MIDI sequence\nName: {new FileInfo(StaticUtils.FileName).Name}";
        o += $"\n\n{Track.TicksPerBeat} ticks/beat, {Track.Messages?.Count} messages\n";
        rows.Clear();
        if (Track.Messages == null) return o;
        
        rows.AddRange(Track.Messages.Select(m => (string[])
        [
            m.Delta.ToString(),
            ((m.Event.ToString() != null ? m.Event.ToString() : "") ?? string.Empty),
            m.Status.ToString(),
            m.Event.ProgramID.ToString()
        ]));
        o += StaticUtils.GenerateTable(colHeaders, rows, rows.Select(row => row[1].Length + 1).Prepend(15).Max());
        return o;
    }
}

// shorthand for "MIDI Track", immediately follows MThd, i.e. "MIDI header"
public class MTrk
{
    public uint TicksPerBeat { get; set; }
    public List<SqMessage> Messages { get; set; } = [];

    public void Read(BinaryStream bs)
    {
        bs.Seek(0xC);
        var d1 = bs.Read1Byte();
        var d2 = bs.Read1Byte(); // reverse bytes, because BE
        TicksPerBeat = BitConverter.ToUInt16([d2, d1]); // should always be 480 for Flipnic .MID files
        bs.Seek(0x16, SeekOrigin.Begin);

        
        // Starting from here, closely matches midi format specification
        byte lastStatus = 0;
        while (true)
        {
            var message = new SqMessage();
            message.Read(bs, lastStatus);
            if (message.Status == 0xFF)
            {
                
                var meta = message.Event as SqMetaEvent;
                if (meta.Type == 0x2F)
                    break;
            }

            if (message.Event is null)
            {
                continue;
            }
            Messages.Add(message);
            lastStatus = message.Status;
        }
    }
}

public class SqMessage
{
    public byte Status { get; set; }
    public uint Delta { get; set; }
    public ISqEvent Event { get; set; }

    public void Read(BinaryStream bs, byte lastStatus)
    {
        // SDDRV::SqSequencer::statusEventCaller (GT4O US: 0x535238) 
        // (yes i'm reading the delta first here)
        Delta = (uint)Midi.readVariable(bs);
        byte status = bs.Read1Byte();
        if ((status & 0x80) != 0) {
            Status = status;
        }
        else
        {
            Status = lastStatus;
            bs.Position -= 1;
        }

        Status = status;

        // This is all that's supported by the SqSequencer
        // Note that SeSequencer may support more events/meta (not implemented for now, it's used by sfx - midi is bundled inside the ins header in that case)
        ISqEvent @event;
        if ((Status & 0xF0) == 0x80) // SDDRV::SqSequencer::ev_8x (GT4O US: 0x535350)
        {
            Event = new SqNoteOffEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            Event.Read(bs);
        }
        else if ((Status & 0xF0) == 0x90) // SDDRV::SqSequencer::ev_9x (GT4O US: 0x535390)
        {
            Event = new SqNoteOnEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            Event.Read(bs);
        }
        else if ((Status & 0xF0) == 0xB0) // SDDRV::SqSequencer::ev_Bx (GT4O US: 0x535400)
        {
            Event = new SqControllerEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            Event.Read(bs);
        }
        else if ((Status & 0xF0) == 0xC0) // SDDRV::SqSequencer::ev_Cx (GT4O US: 0x535700)
        {
            Event = new SqProgramEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            
            Event.Read(bs);
        }
        else if ((Status & 0xF0) == 0xE0) // SDDRV::SqSequencer::ev_Ex (GT4O US: 0x535770)
        {
            Event = new SqPitchBendEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            Event.Read(bs);
        }
        else if (status == 0xFF) // SDDRV::SqSequencer::ev_Fx (GT4O US: 0x535820)
        {
            Event = new SqMetaEvent()
            {
                ProgramID = (byte)(Status & 0x0F),
            };
            Event.Read(bs);

            if (Event is SqMetaEvent meta && meta.Type == 0x2F)
                return;
        }
    }
}

public class SqNoteOnEvent : ISqEvent
{
    public Note Note { get; set; }

    /// <summary>
    /// NOTE: This is used and indexed to the Jam velocity table first to then get the real velocity
    /// Think of this as a velocity entry index
    /// </summary>
    public byte Velocity { get; set; }

    public byte ProgramID { get; set; }

    public void Read(BinaryStream bs)
    {
        Note = (Note)bs.Read1Byte();
        Velocity = bs.Read1Byte();
    }
}

public class SqNoteOffEvent : ISqEvent
{
    public Note Note { get; set; }

    public byte ProgramID { get; set; }

    public void Read(BinaryStream bs)
    {
        Note = (Note)bs.Read1Byte();
        // No velocity in Sq
    }
}


public class SqControllerEvent : ISqEvent
{
    public byte Type { get; set; }
    public byte Value { get; set; }

    public byte ProgramID { get; set; }

    public void Read(BinaryStream bs)
    {
        Type = bs.Read1Byte();
        Value = bs.Read1Byte();

        // Supported general purpose types:
        // 1 (Modulation Wheel)
        // 2 (Breath)
        // 7 (Volume)
        // 10 (Pan)
        // 11 (Expression)
        // 64 (Hold Pedal #1) - 
        // 91 (Reverb Level)
        // 94 (Celeste Depth) - SDDRV::VoiceFilter::setDirectVolume
        // 102 (???? Custom?)

        // NOTE: SqSequencer supports NRPN (type 99) value 20 and 30
        // 20 is start of loop - saves current midi pointer,
        // 30 is end of loop. resumes midi pointer & jumps back to the start of the loop.
    }
}

public class SqProgramEvent : ISqEvent
{
    public byte Program { get; set; }
    public byte Value { get; set; }
    public byte ProgramID { get; set; }

    public void Read(BinaryStream bs)
    {
        Program = (byte)(bs.Read1Byte() & 0x0F);
        Console.Write("");
    }
}

public class SqPitchBendEvent : ISqEvent
{
    public byte Lsb { get; set; }

    public byte ProgramID { get; set; }
    public void Read(BinaryStream bs)
    {
        Lsb = bs.Read1Byte();
    }
}

public class SqMetaEvent : ISqEvent
{
    public byte Type { get; set; }
    public uint Length { get; set; }
    public ISqMeta Meta { get; set; }

    public byte ProgramID { get; set; }

    public void Read(BinaryStream bs)
    {
        Type = bs.Read1Byte();
        Length = (uint)bs.Read7BitInt32();
        ProgramID = 0;

        if (Type == 0x51)
        {
            Meta = new SqSetTempoEvent();
            Meta.Read(bs);
        }
        else
        {
            bs.ReadBytes((int)Length);
        }
    }
}

public interface ISqEvent
{
    public byte ProgramID { get; set; }
    public void Read(BinaryStream bs);
}
