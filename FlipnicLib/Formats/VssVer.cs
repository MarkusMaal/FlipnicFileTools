namespace FlipnicLib.Formats;

public class VssVer
{
    private readonly List<VssSect> _sections;
    public VssVer(byte[] data)
    {
        _sections = [];
        var offsets = FindOffsets(data);
        for (var i = 0; i < offsets.Length - 1; i++)
        {
            _sections.Add(new VssSect(data.Skip(offsets[i]).Take(offsets[i+1] - offsets[i]).ToArray()));
        }
    }

    private static int[] FindOffsets(byte[] data)
    {
        var offsets = new List<int>();
        for (var i = 0; i < data.Length; i += 0x10)
        {
            if (StaticUtils.GetUInt32(data, i) == 0x11234)
            {
                offsets.Add(i);
            }
        }

        offsets.Add(data.Length);
        return offsets.ToArray();
    }

    public override string ToString()
    {
        return _sections.Aggregate("Microsoft Visual SourceSafe\nSource Code Control file\n\n", (current, sect) => current + (sect + "\n"));
    }

    private class VssSect
    {
    
        private readonly string _guid;
        private readonly string _checksum;
        private readonly string _projectId;
        private readonly List<string[]> _files;
        public VssSect(byte[] data)
        {
            _guid = "{" + StaticUtils.GetUInt32(data, 4).ToString("X").PadLeft(8, '0');
            for (var i = 8; i < 0xE; i += 2)
            {
                _guid += "-" + StaticUtils.GetUInt16(data, i).ToString("X").PadLeft(4, '0');
            }

            _guid += "-";
            for (var i = 0xE; i < 0x14; i += 1)
            {
                _guid += data[i].ToString("X").PadLeft(2, '0');
            }

            _guid += "}";
            _checksum = StaticUtils.GetUInt32(data, 0x14).ToString("X").PadLeft(8, '0');
            _projectId = StaticUtils.GetUInt32(data, 0x18).ToString("X").PadLeft(4, '0');
            _files = [];
            var offset = 0x20;
            while (offset < data.Length && StaticUtils.GetUInt32(data, offset) != 0x11234)
            {
                var fileId = StaticUtils.GetUInt32(data, offset);
                var checksum = StaticUtils.GetUInt32(data, offset + 4);
                var timestamp = StaticUtils.GetUInt32(data, offset + 8);
                var revision = StaticUtils.GetUInt32(data, offset + 12);
                if (fileId + checksum + timestamp + revision == 0) break;
                _files.Add([fileId.ToString("X") + "h", checksum.ToString("X"), timestamp.ToString("X"), revision.ToString()]);
                offset += 0x10;
            }
        }

        public override string ToString()
        {
            string[] colHeaders = ["File ID", "Checksum", "Timestamp", "Revision"];
            return $"Project GUID: {_guid}\nChecksum: {_checksum}\nProject ID: {_projectId}h\n\nAssociated files:\n{StaticUtils.GenerateTable(colHeaders, _files, StaticUtils.SimpleOutput)}";
        }
    }
}
