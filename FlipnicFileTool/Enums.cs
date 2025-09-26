using FlipnicLib;

namespace FlipnicFileTool;

public abstract class Enums
{
    public enum Modes
    {
        ListResources,
        ShowFpc,
        ConvertXml,
        ShowHelp,
        ShowSstToc,
        ShowMessages,
        ListPssStreams,
        ExtractPssStreams,
        ListBin,
        ExtractBin,
        ShowGimmick,
        ShowLp4,
        ShowMlb,
        ShowTim2,
        ConvertTim2,
        GenerateMockup,
        ConvertIpu,
        ConvertInt,
        ConvertPssMov,
        ConvertSvag,
        ShowHd,
        ShowBd,
        ShowMidi,
        ConvertSf2,
        ExtractSamples,
        ShowVsd,
        ShowFpd,
        ShowLay,
        ShowPseudoCode,
        ShowIpu,
        ExportObj,
        ShowIco,
        ConvertIcoTexture,
        ConvertIcoObj,
        GenerateMsg,
        ConflictingModes,
        NoAction
    }
    
    public static Modes GuessAction(string fileName)
    {
        return Path.GetExtension(fileName) switch
        {
            ".FPC" => Modes.ShowFpc,
            ".SST" => Modes.ShowSstToc,
            ".MSG" => Modes.ShowMessages,
            ".PSS" => Modes.ListPssStreams,
            ".BIN" => Modes.ListBin,
            ".LP4" => Modes.ShowLp4,
            ".MLB" => Modes.ShowMlb,
            ".TM2" => Modes.ShowTim2,
            ".HD"  => Modes.ShowHd,
            ".MID" => Modes.ShowMidi,
            ".VSD" => Modes.ShowVsd,
            ".LAY" => Modes.ShowLay,
            ".IPU" => Modes.ShowIpu,
            ".ICO" => Modes.ShowIco,
            _ => Modes.ShowHelp
        };
    }

    public static Modes GetMode(string flag, Modes mode)
    {
        var oldMode = mode;
        mode = flag switch
        {
            "--help" => Modes.ShowHelp,
            "--show-fpc" => Modes.ShowFpc,
            "--show-sst-resources" => Modes.ListResources,
            "--convert-fpc-to-xml" => Modes.ConvertXml,
            "--show-sst-toc" => Modes.ShowSstToc,
            "--show-messages" => Modes.ShowMessages,
            "--list-pss-streams" => Modes.ListPssStreams,
            "--extract-pss-streams" => Modes.ExtractPssStreams,
            "--list-files" => Modes.ListBin,
            "--extract-files" => Modes.ExtractBin,
            "--show-gimmick" => Modes.ShowGimmick,
            "--show-lp4" => Modes.ShowLp4,
            "--show-mlb" => Modes.ShowMlb,
            "--show-tim2" => Modes.ShowTim2,
            "--show-hd" => Modes.ShowHd,
            "--show-midi" => Modes.ShowMidi,
            "--show-vsd" => Modes.ShowVsd,
            "--convert-tim2" => Modes.ConvertTim2,
            "--generate-mockup" => Modes.GenerateMockup,
            "--convert-ipu" => Modes.ConvertIpu,
            "--convert-int" => Modes.ConvertInt,
            "--convert-pss-mov" => Modes.ConvertPssMov,
            "--convert-svag" => Modes.ConvertSvag,
            "--convert-sf2" => Modes.ConvertSf2,
            "--show-lay" => Modes.ShowLay,
            "--get-pseudo-code" => Modes.ShowPseudoCode,
            "--show-fpd" => Modes.ShowFpd,
            "--show-ipu" => Modes.ShowIpu,
            "--export-obj" => Modes.ExportObj,
            "--show-ico" => Modes.ShowIco,
            "--convert-ico-texture" => Modes.ConvertIcoTexture,
            "--convert-ico-obj" => Modes.ConvertIcoObj,
            "--generate-msg" => Modes.GenerateMsg,
            "--show-bd" => Modes.ShowBd,
            "--extract-samples" => Modes.ExtractSamples,
            _ => mode
        };
        
        if (oldMode == mode) StaticUtils.IsModeSet = false;
        if (mode == Modes.ShowHelp) return mode;
        /*if (StaticUtils.IsModeSet)
        {
            mode = Modes.ConflictingModes;
        }*/
        StaticUtils.IsModeSet = true;

        return mode;
    }
}