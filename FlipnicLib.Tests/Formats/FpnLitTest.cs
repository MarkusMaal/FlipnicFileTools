using System.IO;
using System.Linq;
using FlipnicLib.Formats;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests.Formats;

[TestClass]
[TestSubject(typeof(FpnLit))]
public class FpnLitTest
{
    private readonly byte[] _mockLit =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xC8, 0x42, 0x00, 0x80, 0xD2, 0x42, 0x00, 0x80, 0xDD, 0x42, 0x00, 0x00, 0x00, 0x00
    ];

    [TestMethod]
    public void TestRoundTrip()
    {
        var ms = new MemoryStream(_mockLit);
        var lit =  new FpnLit(ms);
        Assert.IsNotNull(lit);
        foreach (var (i, b) in lit.GetBytes().Index())
        {
            Assert.AreEqual(_mockLit[i], b);
        }
    }

    [TestMethod]
    public void TestRgb()
    {
        var ms = new MemoryStream(_mockLit);
        var lit = new FpnLit(ms);
        Assert.IsNotNull(lit);
        Assert.HasCount(1, lit.LightMaps);
        Assert.AreEqual(100f, lit.LightMaps[0].Red);
        Assert.AreEqual(105.25f, lit.LightMaps[0].Green);
        Assert.AreEqual(110.75f, lit.LightMaps[0].Blue);
    }
}