using System.Diagnostics.CodeAnalysis;
using System.Text;
using FlipnicLib.Types;

namespace FlipnicLib.Formats;

public class FpnSave
{

    // declarations
    private readonly List<byte> _dataList = [];

    private readonly string[] _gameModes =
    [
        "Original game", "Biology A", "Biology B", "Metallurgy A", "Metallurgy B", "Optics A", "Optics B", "Geometry A",
        "Biology A (Time Attack)", "Biology B (Time Attack)", "Metallurgy A (Time Attack)",
        "Metallurgy B (Time Attack)", "Optics A (Time Attack)", "Optics B (Time Attack)", "Geometry A (Time Attack)"
    ];

    public int LeaderboardId { get; set; }

    public Rank[] Rank
    {
        get
        {
            List<Rank> ranks = [];
            var startIdx = LeaderboardId * 5;
            for (var i = startIdx; i < startIdx + 5; i++)
            {
                ranks.Add(new Rank(this, i % 5, i / 5));
            }
            return ranks.ToArray();
        }
    }

    public enum Difficulty {
        Easy,
        Normal,
        Hard
    }

    // primary constructor
    public FpnSave(byte[] data) {
        _dataList.AddRange(data);
    }

    public void Save(string filename)
    {
        using var fos = new FileStream(filename, FileMode.Create, FileAccess.Write);
        foreach (var b in _dataList) {
            fos.WriteByte(b);
        }
        fos.Close();
    }

    public byte[] Read() {
        return _dataList.ToArray();
    }

    // internal methods
    private byte[] ReadBytes(int addr, int count, byte[]? data = null) {
        var dataList = _dataList;
        if (data != null) dataList = data.ToList();

        if (addr + count > dataList.Count) {
            LogError("Invalid range: " + addr + " to " + (addr+count));
            return new byte[count];
        }
        var returnArray = new byte[count];
        var j = 0;
        for (var i = addr; i < addr + count; i++) {
            returnArray[j] = dataList[i];
            j++;
        }
        return returnArray;
    }
    
    private static string ClockTime() {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    private static void LogError(string error) {
        Console.WriteLine("[" + ClockTime() + "] " + error);
    }

    private void WriteByte(int addr, byte value) {
        if (addr >= _dataList.Count) {
            LogError("Offset " + addr + " out of range!");
            return;
        }
        _dataList[addr] = value;
    }

    // general methods with abstraction layer
    
    private bool IsLoaded() {
        return _dataList.Count != 0;
    }
    public bool IsValidHeader() {
        var reference = Convert.FromHexString("3402cb0f43553624");
        var actual = ReadBytes(0, 8);
        return reference.SequenceEqual(actual);
    }
    
    public void SetScore(int mode, int idx, long score, string initials, int combos, Difficulty difficulty) {
        if (!IsLoaded()) {
            return;
        }
        var offset = 0x60+((5*mode+idx) * 0x38);
        var scoreData = ReadBytes(offset, 0x38);

        // Convert values to little-endian byte arrays
        var scoreBytes = BitConverter.GetBytes(score); // long -> 8 bytes
        var comboBytes = BitConverter.GetBytes(combos); // int -> 4 bytes
        var diffBytes = BitConverter.GetBytes((int)difficulty); // enum ordinal -> int -> 4 bytes

        // Encode initials as UTF-8
        var inb = Encoding.UTF8.GetBytes(initials);

        // Copy score
        for (var i = 0; i < 8; i++)
        {
            scoreData[i] = scoreBytes[i];
        }

        // Copy initials (3 chars max, assumes at least 3 bytes in inb)
        scoreData[0x10] = inb.Length > 0 ? inb[0] : (byte)0;
        scoreData[0x11] = inb.Length > 1 ? inb[1] : (byte)0;
        scoreData[0x12] = inb.Length > 2 ? inb[2] : (byte)0;

        // Difficulty (4 bytes)
        Array.Copy(diffBytes, 0, scoreData, 0x8, 4);

        // Combos (4 bytes)
        Array.Copy(comboBytes, 0, scoreData, 0xC, 4);

        // Write updated block back
        for (var i = offset; i < offset + scoreData.Length; i++)
        {
            WriteByte(i, scoreData[i - offset]);
        }
    }

    public string[] GetScore(int idx) {
        if (!IsLoaded()) {
            return [];
        }
        var offset = 0x60+(idx * 0x38);
        var scoreVal = StaticUtils.GetInt64(_dataList.ToArray(), offset);
        var modeIdx = (idx - (idx % 5)) / 5;
        var rank = idx % 5;
        rank++;
        var initials = StaticUtils.GetStringAt(_dataList.ToArray(), offset+0x10);
        var combos = StaticUtils.GetInt32(_dataList.ToArray(), offset + 0xC);
        var difficultyId = StaticUtils.GetInt32(_dataList.ToArray(), offset + 0x8);
        var difficulty = difficultyId switch {
            0 => "Easy",
            1 => "Normal",
            2 => "Hard",
            _ => "(null)"
        };
        return ((rank) + ";" + initials + ";" + scoreVal + ";" + combos + ";0x" + offset.ToString("X") + ";" + difficulty + ";" + _gameModes[modeIdx]).Split(";");
    }

    
}