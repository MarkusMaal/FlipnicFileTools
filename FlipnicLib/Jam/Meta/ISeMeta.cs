using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace FlipnicLib.Jam.Meta;

public interface ISeMeta
{
    public void Read(BinaryStream bs);
}
