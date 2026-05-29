using System.IO;
using FlipnicLib.Formats;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests.Formats;

[TestClass]
[TestSubject(typeof(FpnFpc))]
public class FpnFpcTest
{
    private readonly byte[] _mockFpc =
    [
        0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x70, 0x41, 0x00, 0x00, 0xF0, 0x41, 0x00, 0x00, 0xA1, 0x42, 0x00, 0x00, 0xB4, 0x42, 0x00, 0x80, 0xC8, 0x42,
        0x00, 0x80, 0xA1, 0x42, 0xCD, 0xCC, 0xF0, 0x41, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00,
        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0xA0, 0x40,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA0, 0xC0
    ];

    [TestMethod]
    public void FpcParser()
    {
        var ms = new MemoryStream(_mockFpc);
        var fpc = new FpnFpc(ms);
        Assert.HasCount(4, fpc.CamFrames);
        Assert.AreEqual("90", fpc.FoVf);
        Assert.AreEqual("15", fpc.OriginXf);
        Assert.AreEqual("30", fpc.OriginYf);
        Assert.AreEqual("80.5", fpc.OriginZf);
        Assert.AreEqual("100.25", fpc.TargetXf);
        Assert.AreEqual("80.75", fpc.TargetYf);
        Assert.AreEqual("30.1", fpc.TargetZf);
        Assert.AreEqual("10", fpc.CamFrames[0].OriginY);
        Assert.AreEqual("5", fpc.CamFrames[1].OriginY);
        Assert.AreEqual("0", fpc.CamFrames[2].OriginY);
        Assert.AreEqual("-5", fpc.CamFrames[3].OriginY);
    }
    
    [TestMethod]
    public void FpcRoundTrip()
    {
        var ms = new MemoryStream(_mockFpc);
        var fpc = new FpnFpc(ms);
        Assert.IsNotNull(fpc);
        ms.Seek(0, SeekOrigin.Begin);
        foreach (var b in fpc.GetBytes())
        {
            Assert.AreEqual(ms.ReadByte(), b);
        }
    }
}