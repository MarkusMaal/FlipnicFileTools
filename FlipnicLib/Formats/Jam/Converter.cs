using System.Globalization;
using System.Runtime.InteropServices;
using FlipnicLib.Formats.Midi;
using Kermalis.SoundFont2;
using Syroot.BinaryData;
using SonyVag = FlipnicLib.Formats.Vag.SonyVag;

namespace FlipnicLib.Formats.Jam;

public abstract class Converter
{
 
    // Based on page 47 of SoundFont 2.01 specification (SFSPEC21.PDF)
    // ReSharper disable once InconsistentNaming
    private enum SF2SampleModeFlags
    {
        /// <summary>
        /// Indicates a sound reproduced with no loop.
        /// </summary>
        NoLoop,
        /// <summary>
        /// Indicates a sound which loops continuously.
        /// </summary>
        Continuous,
        /// <summary>
        /// Unused but should be interpreted as indicating no loop.
        /// </summary>
        UnusedNoLoop,
        /// <summary>
        /// Indicates a sound which loops for the duration of key depression then proceeds to play the remainder of the sample.
        /// </summary>
        StartLoopEnd
    };

    /// <summary>
    /// Converts a .hd/.bd (audio bank)'s instruments to a sound font 2 file.
    /// </summary>
    /// <param name="path"></param>
    public static void InstrumentToSoundFont2(string midiFile, string hdFilePath, string bdFilePath, string outputFilePath)
    {
        // We need the MIDI to find out which instrument should be percussion banks
        // Why? Because channel 10 (index 9) is treated differently SF2 wise
        var ssqt = new Midi.Midi(midiFile);
        ssqt.Read();

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
        var channelToPrograms = new List<(byte Channel, byte Program)>();
        byte _i = 1;
        var preset = 0;
        foreach (var msg in ssqt.Track.Messages)
        {
            if (msg.Event is not SqProgramEvent progChangeEvent) continue;
            var channel = (byte)((msg.Status & 0x0F));
            channelToPrograms.Add((channel, (byte)(progChangeEvent.Program)));
            _i++;
        }
        
        // Add metadata to the sf2
        var sf2 = new SF2
        {
            InfoChunk =
            {
                Bank = Path.GetFileNameWithoutExtension(new FileInfo(hdFilePath).Name) + " (SCEI/JAM Voicebank)",
                Tools = "Flipnic File Tools " + StaticUtils.DotFloatString(StaticUtils.LibVersion) + (StaticUtils.IsBeta ? " BETA" : ""),
                Designer = "Generated",
                Date = new FileInfo(hdFilePath).LastWriteTime.ToShortDateString(),
                Products = "PS-SPU2",
                Copyright = "Sony Computer Entertainment Inc.",
                Comment = $"Header: {new FileInfo(hdFilePath).Name}\nBody: {new FileInfo(bdFilePath).Name}\nSequence: {new FileInfo(midiFile).Name}"
            }
        };


        // Start by adding all the instruments/samples to the sf2
        ms.Position = headerSize;

        // First find all samples in the instrument file by navigating through program chunks
        var vagSamples = new Dictionary<uint, SampleInfo>();
        var doLoops = new Dictionary<uint, bool>();

        var sampleIdx = 0;
        var idx = 0;
        
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        
        for (var j = 0; j < channelToPrograms.Count; j++)
        {
            if (channelToPrograms[j].Program >= instrument.ProgramChunks.Count) continue;
            var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
            if (prog == null) continue;

            uint wavLoopStart = 0;
            uint wavLoopEnd = 0;
            var wavLength = 0;
            foreach (var splitChunk in prog.SplitChunks.Where(splitChunk => !vagSamples.ContainsKey(splitChunk.SampleOffset)))
            {
                ms.Seek( headerSize + splitChunk.SampleOffset * 8,  SeekOrigin.Begin);

                var vag = splitChunk.GetData(new BinaryStream(ms), out uint loopStart, out uint loopEnd);
                
                vagSamples.Add(splitChunk.SampleOffset, new SampleInfo(vag, (ushort)vagSamples.Count));

                // Decode sony vag format into regular waveform (PCM16)
                var decoded = SonyVag.Decode(vag);
                var pcm16 = MemoryMarshal.Cast<byte, short>(decoded);

                var looping = loopStart != 0;

                Console.WriteLine($"SF2: vag{j} (base note: {StaticUtils.SNote(splitChunk.BaseNote)}) - looping: {looping}");
                if (StaticUtils.ExportEnvelopes)
                {
                    Console.WriteLine(
                        $"     ADSR: {Math.Round(splitChunk.Attack, 2)} s -> {Math.Round(splitChunk.Decay, 2)} s -> {Math.Round(splitChunk.Sustain, 2)} s ({splitChunk.SustainL * 100}%) -> {Math.Round(splitChunk.Release, 2)} s");
                }

                if (looping)
                {
                    var a = (pcm16.Length / ((double)vag.Length / 0x10));
                    wavLoopStart = (uint)(a * loopStart);
                    wavLoopEnd = (uint)(a * loopEnd);
                    wavLength = pcm16.Length;
                }
                doLoops.Add(splitChunk.SampleOffset, looping);
                
                // Add the sample to the sound bank. Instruments will then pick which sample to use.
                var sampleId = sf2.AddSample(pcm16, $"sample{sampleIdx++}", looping, wavLoopStart, 44100, (byte)splitChunk.BaseNote, 0);
            }
        }

        // If you need information, refer to the specification
        // https://www.synthfont.com/SFSPEC21.PDF

        // Essentially bags declare new incoming data for context (preset or instrument),
        // "generator" might sound complicated but it's a needlessly complicated name for a single parameter for either presets or instruments.

        sampleIdx = 0;
        for (var j = 0; j < channelToPrograms.Count; j++)
        {
            if (channelToPrograms[j].Program >= instrument.ProgramChunks.Count) continue;
            var prog = instrument.ProgramChunks[channelToPrograms[j].Program];
            if (prog is null) continue;

            string name = $"JAM Program {channelToPrograms[j].Program}";
            StaticUtils.LiveLoadStatus = $"Converting JAM Program {channelToPrograms[j].Program}";

            if (channelToPrograms[j].Channel == 9) // Percussion channel
                sf2.AddPreset(name, channelToPrograms[j].Program, 128);
            else
                sf2.AddPreset(name, channelToPrograms[j].Program, 0);
            
            sf2.AddPresetBag();

            // Preset has a specified range with 0xFF (otherwise instruments provide it)
            //if (prog.CountOrFlag == 0xFF)
            //    sf2.AddPresetGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = prog.FullRangeMin, HighByte = prog.FullRangeMax });
            sf2.AddPresetGenerator(SF2Generator.Instrument, new SF2GeneratorAmount { Amount = (short)sf2.AddInstrument(name) });
            long offset = 0;
            for (var k = 0; k < prog.SplitChunks.Count; k++)
            {
                var splitChunk = prog.SplitChunks[k];
                if ((byte)splitChunk.NoteMin == 0xFF && (byte)splitChunk.NoteMax == 0xFF) // (note does not have data)
                    continue;

                sf2.AddInstrumentBag();

                var pan = (int)Normalize(splitChunk.Pan, 0, 128, -500, 500);
                if (doLoops[splitChunk.SampleOffset])
                {
                    sf2.AddInstrumentGenerator(SF2Generator.SampleModes, new SF2GeneratorAmount { Amount = (short)SF2SampleModeFlags.Continuous }); // enable looping
                }

                sf2.AddInstrumentGenerator(SF2Generator.Pan, new SF2GeneratorAmount { Amount = (short)(pan) });
                if ((byte)splitChunk.FineTunePitch != 0xFF)
                {
                    sf2.AddInstrumentGenerator(SF2Generator.FineTune,
                        new SF2GeneratorAmount
                        {
                            Amount = (short)((splitChunk.EnablePitchBend
                                ? 0
                                : (splitChunk.FineTunePitch * (prog.UnkPitchRelated_0x04 / 2))))
                        });
                }

                if (StaticUtils.AltSf2Method)
                {
                    (splitChunk.Attack, splitChunk.Decay) = (splitChunk.Decay, splitChunk.Attack);
                }
                long vol = prog.BaseVolume * splitChunk.Volume;
                // We divide the above value by 127^2 to get the percent vol it represents. 
                var percentvol = vol / (double) (127 * 127);
                sf2.AddInstrumentGenerator(SF2Generator.InitialAttenuation, new SF2GeneratorAmount() { Amount = (short)((1.0-percentvol) * StaticUtils.AdsrMultipliers[4])});
                // sustain rate itself is not included in the final SF2, since there is no direct way to have support for it
                if (StaticUtils.ExportEnvelopes)
                {
                    sf2.AddInstrumentGenerator(SF2Generator.AttackVolEnv,
                        new SF2GeneratorAmount { Amount = (short)(StaticUtils.AdsrMultipliers[0]*Math.Log2(splitChunk.Attack)) });
                    sf2.AddInstrumentGenerator(SF2Generator.SustainVolEnv,
                        new SF2GeneratorAmount { Amount = (short)(StaticUtils.AdsrMultipliers[1]+40-StaticUtils.AdsrMultipliers[1]*splitChunk.SustainL) });
                    sf2.AddInstrumentGenerator(SF2Generator.DecayVolEnv,
                        new SF2GeneratorAmount { Amount = (short)(StaticUtils.AdsrMultipliers[2] * Math.Log2(splitChunk.Decay)) });
                    sf2.AddInstrumentGenerator(SF2Generator.ReleaseVolEnv,
                        new SF2GeneratorAmount { Amount = (short)(StaticUtils.AdsrMultipliers[3] * Math.Log2(splitChunk.Release)) });
                }

                if (splitChunk.PitchBend != 12)
                {
                    sf2.AddInstrumentGenerator(SF2Generator.ModLfoToPitch, new SF2GeneratorAmount
                    {
                        Amount = (short)(splitChunk.PitchBend * (prog.UnkPitchRelated_0x04 / 2))
                    });
                }

                if (splitChunk.Reverb)
                {
                    sf2.AddInstrumentGenerator(SF2Generator.ReverbEffectsSend, new SF2GeneratorAmount { Amount = StaticUtils.ReverbStrength });
                    //sf2.AddInstrumentGenerator(SF2Generator.ChorusEffectsSend, new SF2GeneratorAmount { Amount = StaticUtils.ReverbStrength });
                }

                if (prog.CountOrFlag == 0xFF)
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)(prog.StartNoteRange + k), HighByte = (byte)(prog.StartNoteRange + k) });
                else
                    sf2.AddInstrumentGenerator(SF2Generator.KeyRange, new SF2GeneratorAmount { LowByte = (byte)prog.SplitChunks[k].NoteMin, HighByte = (byte)prog.SplitChunks[k].NoteMax });
                sf2.AddInstrumentGenerator(SF2Generator.SampleID, new SF2GeneratorAmount { UAmount = vagSamples[splitChunk.SampleOffset].SampleID });
                offset += vagSamples[splitChunk.SampleOffset].SampleData.Length;
                sampleIdx += 1;
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