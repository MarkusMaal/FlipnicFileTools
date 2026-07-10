using System.Collections.Generic;
using FlipnicFileTool;
using FlipnicFileTool.Help;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests;

[TestClass]
[TestSubject(typeof(Validator))]
public class ValidatorTest
{

    [TestMethod]
    public void ShouldValidateInputExtensions()
    {
        HelpUtils.GenerateHelp(true);
        Assert.IsNotNull(HelpUtils.Help);
        foreach (var (key, value) in new Dictionary<string, string> {
            { "--show-bd", "BD" },
            { "--list-files", "BIN" },
            { "--show-col", "COL" },
            { "--show-dummy", "DAT" },
            { "--show-fpc", "FPC" },
            { "--show-fpd", "FPD" },
            { "--show-ftl", "FTL" },
            { "--show-hd", "HD" },
            { "--show-ico", "ICO" },
            { "--show-ipu", "IPU" },
            { "--show-iso", "ISO" },
            { "--show-lay", "LAY" },
            { "--show-lit", "LIT" },
            { "--show-lp4", "LP4" },
            { "--show-midi", "MID" },
            { "--show-mlb", "MLB" },
            { "--show-messages", "MSG"},
            { "--list-pak", "PAK" },
            { "--list-pss-streams", "PSS" },
            { "--show-vss", "SCC" },
            { "--show-sst-resources", "SST" },
            { "--show-sst-toc", "SST" },
            { "--show-sst-missions", "SST" },
            { "--show-cameras", "SST" },
            { "--get-pseudo-code", "SST" },
            { "--show-tim2", "TM2" },
            { "--show-vsd", "VSD" },
            })
        {
            Assert.AreEqual("ok", Validator.ValidateArgs(
                [key, "--input", $"TEST.{value}"], HelpUtils.Help));
            Assert.AreEqual("ok", Validator.ValidateArgs(
                ["--input", $"TEST.{value}", key], HelpUtils.Help));
            var expectedValues = new string[2];
            expectedValues[0] = $"When {key} is used, input must be with extension *.{value}";
            expectedValues[1] = expectedValues[0].Replace(key, $"{key}*");
            var result = Validator.ValidateArgs(["--input", "TEST.NUL", key], HelpUtils.Help);
            Assert.Contains(p => p == result, expectedValues);
        }
    }

    [TestMethod]
    public void ShouldFailSampleArgs()
    {
        const string shouldFail = """
                                  --extract-samples --input TEST.BD --output /this/path/does/not/exist
                                  --replace-file --input INPUT_FILE.DAT --output TEST.BIN
                                  --convert-fpc-to-xml
                                  --convert-pss-mp4 --input TEST.PSS --output TEST.MP4 --pal --crop-alpha --scale-factor Hammy
                                  --change-count PATHN,6.7 --input TEST.SST
                                  --extract-files --input TEST.BIN --output . --alternate-normals --force-brute-force
                                  """;
        HelpUtils.GenerateHelp(true);
        Assert.IsNotNull(HelpUtils.Help);
        foreach (var line in shouldFail.Split('\n'))
        {
            Assert.AreNotEqual("ok", Validator.ValidateArgs(
                line.Split(' '), HelpUtils.Help));
        }
    }

    [TestMethod]
    public void ShouldValidateSampleArgs()
    {
        const string shouldPass = """
                                  --extract-samples --input TEST.BD --output .
                                  --extract-files --input TEST.BIN --output .
                                  --extract-pak --input TEST.BIN --output .
                                  --replace-file DUMMY.ABC --input INPUT_FILE.DAT --output TEST.BIN
                                  --export-col-obj ALL --input TEST.COL --output TEST.OBJ
                                  --convert-fpc-to-xml --input TEST.FPC --output TEST.XML
                                  --convert-xml-to-fpc --input TEST.XML --output TEST.FPC
                                  --generate-animation 32 --input A.FPC --input B.FPC --output C.FPC
                                  --export-fpd-obj --input TEST.FPD --output TEST.OBJ
                                  --convert-sf2 --input A.HD --output B.SF2
                                  --convert-sf2 --input A.HD --synthesize-wav --fake-sustain-rate --reverb-strength 50 --output B.SF2
                                  --convert-sf2 --input A.HD --midi-file B.MID --bd-file C.BD --output D.SF2
                                  --convert-ico-texture --input A.ICO --output B.PNG
                                  --convert-ico-obj --input A.ICO --output B.OBJ
                                  --convert-ipu --input TEST.IPU --output TEST.M2V
                                  --ipu-duct-tape --input TEST.IPU --progressive --pal
                                  --extract-iso --input TEST.ISO --output .
                                  --replace-iso DUMMY.ABC --input INPUT_FILE.DUMMY --output TEST.ISO
                                  --export-lp4-json --input TEST.LP4 --output TEST.JSON
                                  --export-obj --input TEST.LP4 --output TEST.OBJ
                                  --export-obj --input TEST.LP4 --output TEST.OBJ --alternate-normals --force-brute-force
                                  --export-box-obj --input TEST.LP4 --output TEST.OBJ
                                  --generate-mockup --input TEST.MLB --output TEST.PNG
                                  --generate-mockup --input TEST.MLB --output TEST.PNG --pal --mlb-section ABC
                                  --generate-msg --input TEST.TXT --output TEST.MSG
                                  --replace-pak DUMMY.ABC --input INPUT_FILE.DUMMY --output TEST.PAK
                                  --extract-pss-streams --input TEST.PSS --output .
                                  --convert-int --input TEST.INT --output TEST.WAV
                                  --convert-pss-mp4 --input TEST.PSS --output TEST.MP4
                                  --convert-pss-mp4 --input TEST.PSS --output TEST.MP4 --pal --crop-alpha --scale-factor 2
                                  --generate-pss TEST.INT --input TEST.IPU --output TEST.PSS
                                  --generate-pss TEST.INT --input TEST.IPU --output TEST.PSS --progressive --pal
                                  --change-count PATHN,128 --input TEST.SST
                                  --convert-svag --input TEST.SVAG --output TEST.WAV
                                  --convert-tim2 --input TEST.TM2 --output TEST.PNG
                                  """;
        HelpUtils.GenerateHelp(true);
        Assert.IsNotNull(HelpUtils.Help);
        foreach (var line in shouldPass.Split('\n'))
        {
            Assert.AreEqual("ok", Validator.ValidateArgs(
                line.Split(' '), HelpUtils.Help));
        }
    }
}