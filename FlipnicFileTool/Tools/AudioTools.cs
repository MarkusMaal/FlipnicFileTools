using FlipnicLib;
using FlipnicLib.Formats.Jam;
using FlipnicLib.Formats.Midi;
using FlipnicLib.Types;
using Syroot.BinaryData;

namespace FlipnicFileTool.Tools;

public class AudioTools
{
    private string FileName { get; set; }
    private string Output { get; set; }
    public AudioTools(Config cfg)
    {
        FileName = cfg.FileName;
        Output = cfg.Output;
        
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowHd:
                ShowHd();
                break;
            case Enums.Modes.ShowBd:
                ShowBd();
                break;
            case Enums.Modes.ExtractSamples:
                ExtractSamples();
                break;
            case Enums.Modes.ShowMidi:
                ShowMidi();
                break;
            case Enums.Modes.ConvertSf2:
                Converter.InstrumentToSoundFont2(cfg.MidiFile != "" ? cfg.MidiFile : (FileName[..^3] + ".MID"), FileName,
                    cfg.BdFile != "" ? cfg.BdFile : (FileName[..^2] + "BD"), Output, cfg.SynthesizeWav, cfg.SimulateSustainRate);
                break;
            case Enums.Modes.ConvertInt:
            case Enums.Modes.ConvertSvag:
                StaticUtils.ConvertAudio(Output, FileName, cfg.Mode == Enums.Modes.ConvertSvag);
                Console.WriteLine($"File saved as {cfg.Output}");
                break;
        }
    }

    /// <summary>
    /// Show information about the JAM soundbank header
    /// </summary>
    private void ShowHd()
    {
        var jh = new JamHeader();
        jh.Read(new BinaryStream(new FileStream(FileName, FileMode.Open, FileAccess.Read)));
        Console.Write(jh.ToString(StaticUtils.SimpleOutput));
    }

    /// <summary>
    /// Show information about the JAM soundbank body file
    /// </summary>
    private void ShowBd()
    {
        var s = new Samples(File.OpenRead(FileName));
        var samples = new List<SampleColl>();
        var offset = 0;
        for (var i = 0; i < s.RawSamples.Count; i++)
        {
            samples.Add(new SampleColl
            {
                Data = s.RawSamples[i],
                Id = i,
                Offset = offset + 0x10,
                LoopStart = s.LoopStarts[i],
                LoopEnd = s.LoopEnds[i],
            });
            offset += s.Lengths[i];
        }
        string[] cols = ["Id", "Offset", "Loop start", "Loop end"];
        List<string[]> rows = [];
        rows.AddRange(samples.Select(sm => (string[])[sm.Id.ToString(), $"0x{sm.OffsetX}", $"0x{sm.LoopStart:X}", $"0x{sm.LoopEnd:X}"]));
        Console.WriteLine(StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput));
    }

    /// <summary>
    /// Extract samples from the .BD file
    /// </summary>
    private void ExtractSamples()
    {
        var s = new Samples(File.OpenRead(FileName));
        var justName = new FileInfo(FileName).Name;
        for (var i = 0; i < s.RawSamples.Count; i++)
        {
            var sample = s.RawSamples[i];
            StaticUtils.ConvertAudio(Path.Combine(Output, $"{justName}.{i}.WAV"), sample, true, 32000);
        }
    }

    /// <summary>
    /// Show events in the MIDI file
    /// </summary>
    private void ShowMidi()
    {
        var midi = new Midi(FileName);
        midi.Read();
        Console.Write(midi.ToString(StaticUtils.SimpleOutput));
    }
}