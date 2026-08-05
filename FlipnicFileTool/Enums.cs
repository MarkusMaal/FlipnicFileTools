using FlipnicLib;

namespace FlipnicFileTool;

public abstract class Enums
{
    /// <summary>
    /// Includes all the modes this app can operate in
    /// </summary>
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
        ReplaceBin,
        ShowGimmick,
        ShowLp4,
        ShowMlb,
        ShowTim2,
        ConvertTim2,
        GenerateMockup,
        ConvertIpu,
        ConvertInt,
        ConvertPssMpeg,
        ConvertSvag,
        ShowHd,
        ShowBd,
        ShowMidi,
        ConvertSf2,
        ConvertSfxSf2,
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
        ShowIso,
        ExtractIso,
        ReplaceIso,
        NoAction,
        Quit,
        NotImplemented,
        ShowCameras,
        ShowCol,
        ExportColObj,
        ExportFpdObj,
        ShowLit,
        ExportBbox,
        ShowVss,
        ShowFtl,
        ShowElf,
        ShowDummy,
        GeneratePss,
        IpuFix,
        SstResize,
        ExtractPak,
        ListPak,
        ReplacePak,
        ConvertFpc,
        GenerateAnimation,
        Playground,
        ShowSstMissions,
        ExportLp4Json,
        ShowSstRespawns,
        ShowDrawDistance,
        ShowControllableGimmicks
    }
    
    /// <summary>
    /// Attempt to guess the default action based on the extension of the filename provided
    /// </summary>
    /// <param name="fileName">Full path to the input file</param>
    /// <returns>The mode we guessed, default is help</returns>
    public static Modes GuessAction(string fileName)
    {
        return Path.GetExtension(fileName.ToUpper()) switch
        {
            ".FPC" => Modes.ShowFpc,
            ".FPD" => Modes.ShowFpd,
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
            ".ISO" => Modes.ShowIso,
            ".COL" => Modes.ShowCol,
            ".LIT" => Modes.ShowLit,
            ".SCC" => Modes.ShowVss,
            ".FTL" => Modes.ShowFtl,
            ".DAT" => Modes.ShowDummy,
            ".PAK" => Modes.ListPak,
            _ => Modes.ShowHelp
        };
    }

    /// <summary>
    /// Switches to a specific mode based on the CLI flag
    /// </summary>
    /// <param name="flag">The flag from CLI args, must start with two dashes (e.g. --help)</param>
    /// <param name="mode">Current mode of the app before switching</param>
    /// <returns>The new mode the app should operate in</returns>
    public static Modes GetMode(string flag, Modes mode)
    {
        var oldMode = mode;
        mode = flag switch
        {
            "--help" => Modes.ShowHelp,
            "--show-fpc" => Modes.ShowFpc,
            "--show-sst-resources" => Modes.ListResources,
            "--convert-fpc-to-xml" => Modes.ConvertXml,
            "--convert-xml-to-fpc" => Modes.ConvertFpc,
            "--show-sst-toc" => Modes.ShowSstToc,
            "--show-sst-missions" => Modes.ShowSstMissions,
            "--show-messages" => Modes.ShowMessages,
            "--show-draw-distance" => Modes.ShowDrawDistance,
            "--show-controllable-objects" => Modes.ShowControllableGimmicks,
            "--list-pss-streams" => Modes.ListPssStreams,
            "--extract-pss-streams" => Modes.ExtractPssStreams,
            "--list-files" => Modes.ListBin,
            "--extract-files" => Modes.ExtractBin,
            "--replace-file" => Modes.ReplaceBin,
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
            "--convert-pss-mp4" => Modes.ConvertPssMpeg,
            "--convert-svag" => Modes.ConvertSvag,
            "--convert-sfx-sf2" => Modes.ConvertSfxSf2,
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
            "--show-iso" => Modes.ShowIso,
            "--extract-iso" => Modes.ExtractIso,
            "--replace-iso" => Modes.ReplaceIso,
            "--show-cameras" => Modes.ShowCameras,
            "--show-col" => Modes.ShowCol,
            "--export-col-obj" => Modes.ExportColObj,
            "--export-fpd-obj" => Modes.ExportFpdObj,
            "--show-lit" => Modes.ShowLit,
            "--show-vss" => Modes.ShowVss,
            "--show-ftl" => Modes.ShowFtl,
            "--show-elf" => Modes.ShowElf,
            "--show-dummy" => Modes.ShowDummy,
            "--show-sst-respawns" => Modes.ShowSstRespawns,
            "--export-box-obj" => Modes.ExportBbox,
            "--generate-pss" => Modes.GeneratePss,
            "--ipu-duct-tape" => Modes.IpuFix,
            "--change-count" => Modes.SstResize,
            "--extract-pak" => Modes.ExtractPak,
            "--list-pak" => Modes.ListPak,
            "--replace-pak" => Modes.ReplacePak,
            "--generate-animation" => Modes.GenerateAnimation,
            "--playground" => Modes.Playground,
            "--export-lp4-json" => Modes.ExportLp4Json,
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