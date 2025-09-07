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
        // We need the sqt to find out which instrument should be percussion banks
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
;
        // We are creating one sf2 per track
        //for (int i = 0; i < ssqt.Tracks.Count; i++)
        for (var i = 0; i < 1; i++)
        {
            List<(byte Channel, byte Program)> channelToPrograms = new List<(byte Channel, byte Program)>();
            byte _i = 1;
            foreach (SqMessage msg in ssqt.Track.Messages)
            {
                if (msg.Event is not SqProgramEvent progChangeEvent) continue;
                channelToPrograms.Add((msg.Event.ProgramID, (byte)(progChangeEvent.Program)));
                _i++;
            }
            

            // Start by adding all the instruments/samples to the sf2
            var sf2 = new SF2();

            ms.Position = headerSize;

            var rawSamples = new List<byte[]>();
            var loopStarts = new List<uint>();
            var loopEnds = new List<uint>();
            var lengths = new List<int>();

            while (ms.CanRead)
            {

                var breakAll = false;
                for (var j = channelToPrograms.Count - 1; j >= 0; j--)
                {
                    if (instrument.ProgramChunks[channelToPrograms[j].Program] is null) continue;
                    var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
                    foreach (var splitChunk in prog.SplitChunks)
                    {
                        if ((byte)splitChunk.NoteMin == 0xFF && (byte)splitChunk.NoteMax == 0xFF)
                            continue;

                        var bs = new BinaryStream(ms);
                        if (bs.Position >= bodySize + headerSize)
                        {
                            breakAll = true;
                            break;
                        }

                        var vag = splitChunk.GetData(bs, out var loopStart, out var loopEnd);
                        rawSamples.Add(vag);
                        loopStarts.Add(loopStart);
                        loopEnds.Add(loopEnd);
                        lengths.Add(vag.Length);
                    }

                    if (breakAll)
                    {
                        break;
                    }
                }

                if (breakAll) break;
            }
            
            rawSamples.Reverse();
            loopStarts.Reverse();
            loopEnds.Reverse();
            lengths.Reverse();
            try
            {
                var pad16 = rawSamples[18];
                var pad17 = rawSamples[17];
                var xylo1 = rawSamples[11];
                var xylo2 = rawSamples[11];
                (rawSamples[10], rawSamples[1]) = (rawSamples[1], rawSamples[10]);
                rawSamples[1] = xylo2;
                (rawSamples[9], rawSamples[10]) = (rawSamples[10], rawSamples[9]);
                (rawSamples[22], rawSamples[2]) = (rawSamples[2], rawSamples[22]);
                (rawSamples[28], rawSamples[3]) = (rawSamples[3], rawSamples[28]);
                (rawSamples[18], rawSamples[4]) = (rawSamples[4], rawSamples[18]);
                (rawSamples[7], rawSamples[5]) = (rawSamples[5], rawSamples[7]);
                (rawSamples[14], rawSamples[6]) = (rawSamples[6], rawSamples[14]);
                rawSamples[7] = xylo1;
                (rawSamples[17], rawSamples[8]) = (rawSamples[8], rawSamples[17]);
                (rawSamples[33], rawSamples[9]) = (rawSamples[9], rawSamples[33]);
                rawSamples[13] = pad16;
                rawSamples[14] = pad17;
            }
            catch
            {
                // ignored
            }

            // First find all samples in the instrument file by navigating through program chunks
            var vagSamples = new Dictionary<uint, SampleInfo>();

            var sampleIdx = 0;
            var idx = 0;
            for (var j = channelToPrograms.Count - 1; j >= 0; j--)
            {
                if (channelToPrograms[j].Program >= instrument.ProgramChunks.Count) continue;
                if (instrument.ProgramChunks[channelToPrograms[j].Program] == null) continue;
                var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
                foreach (var splitChunk in prog.SplitChunks)
                {
                    if ((byte)splitChunk.NoteMin == 0xFF && (byte)splitChunk.NoteMax == 0xFF)
                        continue;

                    // May refer to same sample so offset
                    if (vagSamples.ContainsKey(splitChunk.SD_VA_SSA))
                    {
                        continue;
                    }

                    idx++;
                    var sI = j;
                    vagSamples.Add(splitChunk.SD_VA_SSA, new SampleInfo(rawSamples[channelToPrograms[sI].Program], (ushort)vagSamples.Count));

                    // Decode sony vag format into regular waveform (PCM16)
                    byte[] decoded = SonyVag.Decode(rawSamples[channelToPrograms[sI].Program]);
                    Span<short> pcm16 = MemoryMarshal.Cast<byte, short>(decoded);

                    bool looping = loopStarts[channelToPrograms[sI].Program] != 0 && loopEnds[channelToPrograms[sI].Program] != 0;

                    Console.WriteLine($"SF2: vag{j} (base note: {splitChunk.BaseNote}) - looping: {looping}");

                    uint wavLoopStart = 0;
                    if (looping)
                    {
                        double a = ((double)pcm16.Length / ((double)lengths[channelToPrograms[sI].Program] / 0x10));
                        wavLoopStart = (uint)(a * loopStarts[channelToPrograms[sI].Program]);
                    }

                    // Add the sample to the sound bank. Instruments will then pick which sample to use.
                    uint sampleId = sf2.AddSample(pcm16, $"sample{sampleIdx++}", looping, wavLoopStart, 44100, (byte)splitChunk.BaseNote, 0);

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
                if (prog == null) continue;
                for (int k = 0; k < prog.SplitChunks.Count; k++)
                {
                    var splitChunk = prog.SplitChunks[k];
                    if ((byte)splitChunk.NoteMin == 0xFF && (byte)splitChunk.NoteMax == 0xFF) // (note does not have data)
                        continue;

                    sf2.AddInstrumentBag();

                    int pan = (int)Normalize(splitChunk.Pan, 0, 128, -500, 500);
                    sf2.AddInstrumentGenerator(SF2Generator.Pan, new SF2GeneratorAmount { Amount = (short)pan });

                    //sf2.AddInstrumentGenerator(SF2Generator.CoarseTune, new SF2GeneratorAmount { Amount = (short)(splitChunk.UnkPitch)});

                    if (prog.CountOrFlag == 0xFF)
                        sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)(prog.StartNoteRange + k), HighByte = (byte)(prog.StartNoteRange + k) });
                    else
                        sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)prog.SplitChunks[k].NoteMin, HighByte = (byte)prog.SplitChunks[k].NoteMax });

                    sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { UAmount = vagSamples[splitChunk.SD_VA_SSA].SampleID });
                }
            }

            sf2.Save(outputFilePath);
        }
    }

    private record SampleInfo(byte[] SampleData, ushort SampleID);

    private static double Normalize(double val, double valmin, double valmax, double min, double max)
    {
        return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
    }
}