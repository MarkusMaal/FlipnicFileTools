using FlipnicLib.Formats.Midi;
using MidiSharp;
using MidiSharp.Events.Meta;
using MidiSharp.Events.Meta.Text;
using MidiSharp.Events.Voice;
using MidiSharp.Events.Voice.Note;
using Syroot.BinaryData;

namespace FlipnicLib.Formats.Jam;

// SeSequencer is the brother of SqSequencer intended for sound effects
// Very very few events supported here, 4 bit channel number not used either
public class SeSeq
{
    private List<SeMessage> Messages { get; set; } = [];

    public void Read(BinaryStream bs)
    {
        // Starting from here, closely matches midi format specification
        byte lastStatus = 0;
        while (true)
        {
            var message = new SeMessage();
            message.Read(bs, lastStatus);
            if (message.Status == 0xFF)
            {
                var meta = message.Event as SeMetaEvent;
                if (meta.Type == 0x2F)
                    break;
            }

            Messages.Add(message);
            lastStatus = message.Status;
        }
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
        return ToString(false);
    }
    
    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["Delta", "Event", "Status"];
        List<string[]> rows = [];
        var plural = (Messages?.Count == 1) ? "" : "s";
        var o = $"{Messages?.Count} message{plural}\n";
        rows.Clear();
        if (Messages == null) return o;
        
        rows.AddRange(Messages.Select(m => (string[])
        [
            m.Delta.ToString(),
            ((m.Event != null ? m.Event.ToString()?.Replace("FlipnicLib.Formats.Jam.", "") : "null") ?? string.Empty),
            m.Status.ToString()
        ]));
        o += StaticUtils.GenerateTable(colHeaders, rows, asCsv);
        return o;
    }

    public void ToMidi(string outputName)
    {
        var midi = new Midi.Midi();
        var msg = new List<SqMessage>();
        var midiSeq = new MidiSequence(Format.Zero, 720);
        midiSeq.Tracks.AddNewTrack();
        var suffix = StaticUtils.IsBeta ? " BETA" : "";
        var noExtName = Path.GetFileNameWithoutExtension(outputName);
        midiSeq.Tracks.Last().Events.Add(new SequenceTrackNameTextMetaMidiEvent(0, noExtName.Split('.')[0] + " (SFX Sequence " + noExtName.Split('.')[1] + ")"));
        midiSeq.Tracks.Last().Events.Add(new CopyrightTextMetaMidiEvent(0, "Sony Computer Entertainment Inc."));
        midiSeq.Tracks.Last().Events.Add(new InstrumentTextMetaMidiEvent(0, noExtName.Split('.')[0] + ".HD"));
        midiSeq.Tracks.Last().Events.Add(new InstrumentTextMetaMidiEvent(0, noExtName.Split('.')[0] + ".BD"));
        midiSeq.Tracks.Last().Events.Add(new TextMetaMidiEvent(0, $"Flipnic file tools {StaticUtils.DotFloatString(StaticUtils.LibVersion)}{suffix}"));
        var channels = new List<int>();
        foreach (var seMsg in Messages)
        {
            switch (seMsg.Event)
            {
                case SeNotePressureEvent @event:
                    msg.Add(new SqMessage
                    {
                        Delta =  seMsg.Delta,
                        Event = new SqNoteOnEvent
                        {
                            Note = (Note)@event.Note,
                            ProgramID = @event.Channel,
                            Velocity = 0x7F,
                        },
                        Status = seMsg.Status
                    });
                    if (!channels.Contains(@event.Channel))
                    {
                        channels.Add(@event.Channel);
                        midiSeq.Tracks.Last().Events.Add(new ProgramChangeVoiceMidiEvent(seMsg.Delta, @event.Channel, @event.Channel));
                    }
                    midiSeq.Tracks.Last().Events.Add(new OnNoteVoiceMidiEvent(seMsg.Delta, @event.Channel, @event.Note, 0x7F));
                    break;
                case SeControllerEvent:
                    msg.Add(new SqMessage
                    {
                        Status = 0xFF,
                        Event = new SqMetaEvent
                        {
                            Length = 0,
                            ProgramID = 0,
                            Type = 0x2F,
                        }
                    });
                    break;
            }
        }
        for (var i = (byte)0; i < 16; i++)
        {
            for (var j = (byte)0; j < 0x7F; j++)
            {
                midiSeq.Tracks.Last().Events.Add(new OffNoteVoiceMidiEvent(2, i, j, 0x7F));
            }
        }
        midiSeq.Tracks.Last().Events.Add(new EndOfTrackMetaMidiEvent(0));

        var oStream = File.OpenWrite(outputName);
        midiSeq.Save(oStream);
        oStream.Close();

        midi.Track = new MTrk
        {
            Messages = msg,
            TicksPerBeat = 480
        };
    }
}

public class SeMessage
{
    public uint Delta { get; set; }
    public byte Status { get; set; }
    public ISeEvent? Event { get; set; }

    public void Read(BinaryStream bs, byte lastStatus)
    {
        var status = bs.Read1Byte();
        if ((status & 0x80) != 0)
            Status = status;
        else
        {
            Status = lastStatus;
            bs.Position -= 1;
        }

        // This is all that's supported by the SqSequencer
        // Note that SeSequencer may support more events/meta (not implemented for now, it's used by sfx - midi is bundled inside the ins header in that case)
        // NOTE: Channel (lower 4 bits) is never used!
        ISeEvent @event;

        if ((Status & 0xF0) == 0xA0)
        {
            Event = new SeNotePressureEvent();
            Event.Read(bs);
        }
        else if ((Status & 0xF0) == 0xB0)
        {
            Event = new SeControllerEvent();
            Event.Read(bs);
        }
        else if (status == 0xFF)
        {
            Event = new SeMetaEvent();
            Event.Read(bs);

            if (Event is SeMetaEvent meta && meta.Type == 0x2F)
                return;
        }

        Delta = (uint)SeSeq.readVariable(bs);
    }
}

public class SeNotePressureEvent : ISeEvent
{
    public byte Note { get; set; }
    public byte Pressure { get; set; }
    public byte Channel { get; set; }

    public void Read(BinaryStream bs)
    {
        // This one has extra values

        // supported by SeSequencer:
        // - 0x01 (modulation wheel) aka pitch modulate depth
        //   * val1 = value, val2 = channel index, val3 = note
        //
        // - 0x02 (breath) aka pitch modulate speed
        //   * val1 = value, val2 = channel index, val3 = note
        //
        // - 0x07 (volume)
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - 0x0a (pan)
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - something above 0x0a and below 0x60 - set auto pitch
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - 0x60 (data increment)
        //   * concatenate value 1 and 2 into a short as offset to jump, value 3 unk
        //   * read extra var int value as delta possibly

        // There are 3 values here (channel is added)
        Note = bs.Read1Byte();
        Pressure = bs.Read1Byte();
        Channel = bs.Read1Byte();
    }
}

public class SeControllerEvent : ISeEvent
{
    public byte Type { get; set; }
    public byte Value { get; set; }
    public byte Value2 { get; set; }
    public byte Value3 { get; set; }

    public byte Note { get; set; }

    public void Read(BinaryStream bs)
    {
        // This one has extra values

        // supported by SeSequencer:
        // - 0x01 (modulation wheel) aka pitch modulate depth
        //   * val1 = value, val2 = channel index, val3 = note
        //
        // - 0x02 (breath) aka pitch modulate speed
        //   * val1 = value, val2 = channel index, val3 = note
        //
        // - 0x07 (volume)
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - 0x0a (pan)
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - something above 0x0a and below 0x60 - set auto pitch
        //   * use val 1 & 2, val 3 = channel index, use extra note
        //
        // - 0x60 (data increment)
        //   * concatenate value 1 and 2 into a short as offset to jump, value 3 unk
        //   * read extra var int value as delta possibly

        // There are 3 values here
        Type = bs.Read1Byte();
        Value = bs.Read1Byte();
        Value2 = bs.Read1Byte();
        Value3 = bs.Read1Byte();

        if (Type >= 3 && Type < 60)
        {
            Note = bs.Read1Byte();
        }
            
    }
}

public class SeMetaEvent : ISeEvent
{
    public byte Type { get; set; }
    public uint Length { get; set; }

    public void Read(BinaryStream bs)
    {
        Type = bs.Read1Byte();
        Length = (uint)bs.Read7BitInt32();

        // Only end supported
        if (Type == 0x2F)
        {
            // TODO
        }

        try
        {
            bs.ReadBytes((int)Length);
        }
        catch
        {
            
        }
    }
}

public interface ISeEvent
{

    public void Read(BinaryStream bs);
}
