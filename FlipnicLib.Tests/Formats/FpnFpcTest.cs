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
        Assert.AreEqual(90f, fpc.Fov);
        Assert.AreEqual(15f, fpc.OriginX);
        Assert.AreEqual(30f, fpc.OriginY);
        Assert.AreEqual(80.5f, fpc.OriginZ);
        Assert.AreEqual(100.25f, fpc.TargetX);
        Assert.AreEqual(80.75f, fpc.TargetY);
        Assert.AreEqual(30.1f, fpc.TargetZ);
        Assert.AreEqual(10f, fpc.CamFrames[0].OriginY);
        Assert.AreEqual(5f, fpc.CamFrames[1].OriginY);
        Assert.AreEqual(0f, fpc.CamFrames[2].OriginY);
        Assert.AreEqual(-5f, fpc.CamFrames[3].OriginY);
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