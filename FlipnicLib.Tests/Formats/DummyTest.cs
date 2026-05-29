using System.IO;
using FlipnicLib.Formats;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlipnicLib.Tests.Formats;

[TestClass]
[TestSubject(typeof(Dummy))]
public class DummyTest
{

    [TestMethod]
    public void DummyZeroFill()
    {
        const string prefix = "Zero padded: ";
        var ms = new MemoryStream();
        for (var i = 0; i < 2048; i++)
        {
            ms.WriteByte(0);
        }
        ms.Seek(0, SeekOrigin.Begin);
        
        var ms2 = new MemoryStream();
        for (var i = 0; i < 2048; i++)
        {
            ms2.WriteByte(0xFF);
        }
        ms2.Seek(0, SeekOrigin.Begin);
        
        var emptyDummy = new Dummy(ms);
        var filledDummy = new Dummy(ms2);
        
        Assert.Contains(prefix + "Yes", emptyDummy.ToString());
        Assert.Contains(prefix + "No", filledDummy.ToString());
        
        ms.Close();
        ms2.Close();
    }
}