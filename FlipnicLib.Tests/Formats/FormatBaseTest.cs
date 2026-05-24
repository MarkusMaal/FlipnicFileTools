using System;
using System.Linq;
using FlipnicLib.Formats;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests.Formats;

[TestClass]
[TestSubject(typeof(FormatBase))]
public class FormatBaseTest
{

    [TestMethod]
    public void TestWriteByteArray()
    {
        byte[] modifiedArray = [0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x10];
        var originalArray = new byte[modifiedArray.Length];
        Array.Copy(modifiedArray, originalArray, modifiedArray.Length);
        byte[] insertableArray = [0x64, 0x21, 0x11, 0xFF];
        const int offset = 2;
        
        TestFormat.TestWriteByteArray(modifiedArray, offset, insertableArray);


        for (var i = 0; i < offset; i++)
        {
            Assert.AreEqual(modifiedArray[i], originalArray[i]);
        }
        
        for (var i = 0; i < insertableArray.Length; i++)
        {
            Assert.AreEqual(modifiedArray[offset + i], insertableArray[i]);
        }

        for (var i = offset + insertableArray.Length; i < modifiedArray.Length; i++)
        {
            Assert.AreEqual(modifiedArray[i], originalArray[i]);
        }
        Assert.HasCount(originalArray.Length, modifiedArray);
    }

    [TestMethod]
    public void TestGetFloat()
    {
        byte[] rawData = [0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x3F, 0x53, 0x84, 0xFF, 0x02];
        
        var interpretedFloatA = TestFormat.TestGetFloat(rawData, 0);
        var interpretedFloatB = TestFormat.TestGetFloat(rawData, 4);
        var interpretedFloatC = TestFormat.TestGetFloat(rawData, 8);
        
        Assert.AreEqual(1.0f, interpretedFloatA);
        Assert.AreEqual(0.5f, interpretedFloatB);
        Assert.IsLessThan(1 * Math.Pow(10, -36), interpretedFloatC);
    }

    [TestMethod]
    public void TestGetString()
    {
        var rawData = "Hello world!\0Second string\0\0\0\0\0\0"u8.ToArray();

        var firStr = TestFormat.TestGetString(rawData);
        var secStr = TestFormat.TestGetString(rawData.Skip(13).ToArray());
        var thiStr = TestFormat.TestGetString(rawData.Skip(12).ToArray());
        
        Assert.AreEqual("Hello world!", firStr);
        Assert.AreEqual("Second string", secStr);
        Assert.AreEqual("", thiStr);
    }

    [TestMethod]
    public void TestGetStringAt()
    {
        var rawData = "Hello world!\0Second string\0\0\0\0\0\0"u8.ToArray();

        var firStr = TestFormat.TestGetStringAt(rawData, 0);
        var secStr = TestFormat.TestGetStringAt(rawData, 13);
        var thiStr = TestFormat.TestGetStringAt(rawData, 12);
        
        Assert.AreEqual("Hello world!", firStr);
        Assert.AreEqual("Second string", secStr);
        Assert.AreEqual("", thiStr);
    }

    [TestMethod]
    public void TestGetFilesizeString()
    {
        const int sampleSize = 1536;
        const int sampleSizeB = 122;
        const int sampleSizeMb = 1310720;
        const int sampleSizeGb = 1879048192;
        
        Assert.AreEqual("1.5 kiB", TestFormat.TestGetFilesizeString(sampleSize));
        Assert.AreEqual("1.25 MiB", TestFormat.TestGetFilesizeString(sampleSizeMb));
        Assert.AreEqual("1.75 GiB", TestFormat.TestGetFilesizeString(sampleSizeGb));
        Assert.AreEqual("122 B", TestFormat.TestGetFilesizeString(sampleSizeB));
    }

    [TestMethod]
    public void TestDotFloatString()
    {
        const float testValue = 1.33f;
        Assert.AreEqual("1.33", TestFormat.TestDotFloatString(testValue));
    }
}