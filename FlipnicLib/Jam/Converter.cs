using System.Runtime.InteropServices;
using FlipnicLib.Midi;
using FlipnicLib.Vag;
using Kermalis.SoundFont2;
using Syroot.BinaryData;


namespace FlipnicLib.Jam;

public abstract class Converter
{
    
    /// <summary>
    /// Converts a .hd/.bd (audio bank)'s instruments to a sound font 2 file.
    /// </summary>
    /// <param name="path"></param>
    public static void InstrumentToSoundFont2(string midiFile, string hdFilePath, string bdFilePath, string outputFilePath)
    {
        // We need the MIDI to find out which instrument should be percussion banks
        // Why? Because channel 10 (index 9) is treated differently SF2 wise
        var ssqt = new Midi.Midi();
        ssqt.Read(midiFile);

        // combine .hd/.bd files
        var hdS = new FileStream(hdFilePath, FileMode.Open);
        var buff = new byte[4];
        hdS.ReadExactly(buff, 0, 4);
        var headerSize = hdS.Length;
        hdS.ReadExactly(buff, 0, 4);
        var bodySize = BitConverter.ToInt32(buff, 0);
        hdS.Seek(0,  SeekOrigin.Begin);
        var bdS = new FileStream(bdFilePath, FileMode.Open);
        var ms = new MemoryStream();
        hdS.CopyTo(ms, (int)headerSize);
        bdS.CopyTo(ms, bodySize);
        ms.Seek(0);
        var instrument = new JamHeader();
        instrument.Read(new BinaryStream(ms));
        List<(byte Channel, byte Program)> channelToPrograms = new List<(byte Channel, byte Program)>();
        byte _i = 1;
        foreach (SqMessage msg in ssqt.Track.Messages)
        {
            if (msg.Event is not SqProgramEvent progChangeEvent) continue;
            byte channel = (byte)(msg.Status & 0x0F);
            channelToPrograms.Add((channel, progChangeEvent.Program));
            _i++;
        }
        

        // Start by adding all the instruments/samples to the sf2
        var sf2 = new SF2();

        ms.Position = headerSize;

        // First find all samples in the instrument file by navigating through program chunks
        var vagSamples = new Dictionary<uint, SampleInfo>();

        var sampleIdx = 0;
        var idx = 0;
        for (var j = 0; j < channelToPrograms.Count; j++)
        {
            var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
            if (prog is null) continue;
            foreach (var splitChunk in prog.SplitChunks)
            {
                // May refer to same sample so offset
                if (vagSamples.ContainsKey(splitChunk.SampleOffset))
                {
                    continue;
                }


                ms.Seek( headerSize + splitChunk.SampleOffset * 8,  SeekOrigin.Begin);

                byte[] vag = splitChunk.GetData(new BinaryStream(ms), out uint loopStart, out uint loopEnd);
                
                vagSamples.Add(splitChunk.SampleOffset, new SampleInfo(vag, (ushort)vagSamples.Count));

                // Decode sony vag format into regular waveform (PCM16)
                byte[] decoded = SonyVag.Decode(vag);
                Span<short> pcm16 = MemoryMarshal.Cast<byte, short>(decoded);

                bool looping = loopStart != 0 && loopEnd != 0;

                Console.WriteLine($"SF2: vag{j} (base note: {splitChunk.BaseNote}) - looping: {looping}");

                uint wavLoopStart = 0;
                if (looping)
                {
                    double a = (pcm16.Length / ((double)vag.Length / 0x10));
                    wavLoopStart = (uint)(a * loopStart);
                }

                // Add the sample to the sound bank. Instruments will then pick which sample to use.
                uint sampleId = sf2.AddSample(pcm16, $"sample{sampleIdx++}", looping, wavLoopStart, 44100, (byte)splitChunk.BaseNote, splitChunk.FineTunePitch);

                // Dump instrument noises (debug)
                /*WaveFormat waveFormat = new WaveFormat(44100, 16, 1);
                Directory.CreateDirectory("samples");
                using (WaveFileWriter writer = new WaveFileWriter($"samples/instrument{j}_{splitChunk.BaseNote}.wav", waveFormat))
                    writer.WriteSamples(pcm16.ToArray(), 0, pcm16.Length);*/

            }
        }

        // If you need information, refer to the specification
        // https://www.synthfont.com/SFSPEC21.PDF

        // Essentially bags declare new incoming data for context (preset or instrument),
        // "generator" might sound complicated but it's a needlessly complicated name for a single parameter for either presets or instruments.

        for (int j = 0; j < channelToPrograms.Count; j++)
        {
            if (channelToPrograms[j].Program >= instrument.ProgramChunks.Count) continue;
            var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
            if (prog is null) continue;

            string name = $"JAM Program {channelToPrograms[j].Program}";

            if (channelToPrograms[j].Channel == 9) // Percussion channel
                sf2.AddPreset(name, channelToPrograms[j].Program, 128);
            else
                sf2.AddPreset(name, channelToPrograms[j].Program, 0);

            sf2.AddPresetBag();

            // Preset has a specified range with 0xFF (otherwise instruments provide it)
            //if (prog.CountOrFlag == 0xFF)
            //    sf2.AddPresetGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = prog.FullRangeMin, HighByte = prog.FullRangeMax });

            sf2.AddPresetGenerator(SF2Generator.Instrument, new SF2GeneratorAmount { Amount = (short)sf2.AddInstrument(name) });
            
            for (int k = 0; k < prog.SplitChunks.Count; k++)
            {
                var splitChunk = prog.SplitChunks[k];
                if ((byte)splitChunk.NoteMin == 0xFF && (byte)splitChunk.NoteMax == 0xFF) // (note does not have data)
                    continue;

                sf2.AddInstrumentBag();

                var pan = splitChunk.Pan - 64;
                sf2.AddInstrumentGenerator(SF2Generator.Pan, new SF2GeneratorAmount { Amount = (short)(pan) });
                sf2.AddInstrumentGenerator(SF2Generator.FineTune, new SF2GeneratorAmount { Amount = (short)((splitChunk.EnablePitchBend ? splitChunk.PitchBend * 0x50 : splitChunk.FineTunePitch * 6.5) )});

                sf2.AddInstrumentGenerator(SF2Generator.DelayModEnv, new SF2GeneratorAmount { Amount = (short)(splitChunk.Delay * 8) });
                sf2.AddInstrumentGenerator(SF2Generator.SustainModEnv, new SF2GeneratorAmount { Amount = (short)(splitChunk.Sustain * 600) });
                sf2.AddInstrumentGenerator(SF2Generator.ReleaseModEnv, new SF2GeneratorAmount { Amount = (short)(splitChunk.Release * 8) });

                if (splitChunk.Reverb)
                {
                    sf2.AddInstrumentGenerator(SF2Generator.ReverbEffectsSend, new SF2GeneratorAmount { Amount = 800 });
                }

                if (prog.CountOrFlag == 0xFF)
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)(prog.StartNoteRange + k), HighByte = (byte)(prog.StartNoteRange + k) });
                else
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)prog.SplitChunks[k].NoteMin, HighByte = (byte)prog.SplitChunks[k].NoteMax });
                sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { UAmount = vagSamples[splitChunk.SampleOffset].SampleID });
            }
        }

        sf2.Save(outputFilePath);
    }

    private record SampleInfo(byte[] SampleData, ushort SampleID);

    private static double Normalize(double val, double valmin, double valmax, double min, double max)
    {
        return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
    }
}