using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests;

[TestClass]
[TestSubject(typeof(BinFile))]
public class BinFileTest
{
    private static readonly MemoryStream MockStream = new();

    [AssemblyInitialize]
    public static void RunBefore(TestContext testContext)
    {
        // Mock .BIN file
        var fsEntries = new Dictionary<string, int>
        {
            { "*Top Of CD Data", 1 },
            { "TESTFILE.DAT", 1},
            { "TESTFILE2.DAT", 2},
            { "SUBDIR\\", 4},
            { "*End Of CD Data", 256 },
        };

        var subDirEntries = new Dictionary<string, int>()
        {
            {"SUBTEST.TXT", 0x100}, // Flipnic file tools testing suite
            {"SUBTEST2.TXT", 0x120}, // Hi
            {"ABCDEF.DAT", 0x122}, // Padding
            {"*End Of Mem Data", 0x7E000},
        };

        foreach (var f in fsEntries)
        {
            MockStream.Write(Encoding.ASCII.GetBytes(f.Key));
            while ((MockStream.Position + 4) % 0x40 != 0)
            {
                MockStream.WriteByte(0);
            }
            MockStream.Write(BitConverter.GetBytes(f.Value));
        }

        MockStream.Position = 2048;
        for (var i = 0; i < 2048; i++) { MockStream.WriteByte(0xA7); }
        for (var i = 0; i < 4096; i++) { MockStream.WriteByte(0x80); }
        foreach (var f in subDirEntries)
        {
            MockStream.Write(Encoding.ASCII.GetBytes(f.Key));
            while ((MockStream.Position + 4) % 0x40 != 0)
            {
                MockStream.WriteByte(0);
            }
            MockStream.Write(BitConverter.GetBytes(f.Value));
        }

        MockStream.Position = 8192 + 0x100;
        MockStream.Write("Flipnic file tools testing suite"u8);
        MockStream.Write("Hi"u8);
        for (var i = 0; i < 0x7DEDE; i++)
        {
            MockStream.WriteByte(0xFF);
        }
        
        MockStream.Seek(0, SeekOrigin.Begin);
    }

    [TestMethod]
    public void BinFileList()
    {
        BinFile bf = new();
        var ls = bf.GetListBin(MockStream);
        
        Assert.AreEqual(@"\TESTFILE.DAT", ls[0].Path);
        Assert.AreEqual(2048, ls[0].Length);
        Assert.IsTrue(ls[0].LargeBuffer);
        Assert.AreEqual(2048, ls[0].Offset);
        Assert.AreEqual(0x40, ls[0].TocOffset);
        
        Assert.AreEqual(@"\TESTFILE2.DAT", ls[1].Path);
        Assert.AreEqual(4096, ls[1].Length);
        Assert.IsTrue(ls[1].LargeBuffer);
        Assert.AreEqual(4096, ls[1].Offset);
        Assert.AreEqual(0x80, ls[1].TocOffset);
        
        Assert.AreEqual(@"\SUBDIR\", ls[2].Path);
        Assert.AreEqual(516096, ls[2].Length);
        Assert.IsTrue(ls[2].LargeBuffer);
        Assert.AreEqual(8192, ls[2].Offset);
        Assert.AreEqual(0xC0, ls[2].TocOffset);
        
        Assert.AreEqual(@"\*End Of CD Data", ls[3].Path);
        Assert.IsTrue(ls[3].LargeBuffer);
        Assert.AreEqual(524288, ls[3].Offset);
        Assert.AreEqual(0x100, ls[3].TocOffset);
        
        Assert.AreEqual(@"\SUBDIR\SUBTEST.TXT", ls[4].Path);
        Assert.AreEqual(32, ls[4].Length);
        Assert.IsFalse(ls[4].LargeBuffer);
        Assert.AreEqual(8448, ls[4].Offset);
        Assert.AreEqual(0x2000, ls[4].TocOffset);
        
        Assert.AreEqual(@"\SUBDIR\SUBTEST2.TXT", ls[5].Path);
        Assert.AreEqual(2, ls[5].Length);
        Assert.IsFalse(ls[5].LargeBuffer);
        Assert.AreEqual(8480, ls[5].Offset);
        Assert.AreEqual(0x2040, ls[5].TocOffset);
        
        Assert.AreEqual(@"\SUBDIR\ABCDEF.DAT", ls[6].Path);
        Assert.AreEqual(515806, ls[6].Length);
        Assert.IsFalse(ls[6].LargeBuffer);
        Assert.AreEqual(8482, ls[6].Offset);
        Assert.AreEqual(0x2080, ls[6].TocOffset);
    }
}