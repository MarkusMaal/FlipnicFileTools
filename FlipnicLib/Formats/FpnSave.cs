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

    private readonly string[] _originalModes =
    [
        "Biology A", "Evolution A", "Metallurgy A", "Evolution B", "Optics A", "Evolution C", "Biology B",
        "Metallurgy B", "Optics B", "Geometry A", "Evolution D", "All stages finished"
    ];

    private readonly string[] _validStrings =
    [
        "ja", "", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "BONUS %dpts", "JACKPOT=%d",
        "IN", "%d", "?Go ?Back", "Yes", "No", "CONTINUE?", "RESULT", "FLAMINGO COUNTS", "FLAMINGO BONUS",
        " % d Pts.", " ----- Pts.", "PERFECT BONUS", "TOTAL SCORE", "WHY DON'T YOU", "GET STARS ?", "YOU GET",
        "ALL", "FLAMINGOS!", "%d/10", "COMBO %d", "SLOT CHANCE!", "%d Pts.", "ALIEN HILL !", "GALAXY TENNIS !",
        "AREA 74 !", "SPACE WARP !", "100_BLOCKS !", "WARNING!", "BUMPER AREA", "NON STOP AREA", "AREA EXIT",
        "CREDIT(S) %d", "NO BONUS", "EXP COUNTS %d", "POINTS × %d", "READY", "%dPts.", "DANGER!", "LEVEL %d",
        "BINGO %d!¥n%ldpts.!¥n", "ANSWER", "GOOD!", "FLAMINGO BONUS", "BANANA BONUS", "BONUS", "BONUS",
        "REST BALL BONUS",
        "PERFECT BONUS", "LEVEL BONUS", "PERFECT BONUS", "Pts.", "TOTAL SCORE", "GET ALL THE COLORS!",
        "GET FIVE RED COLORS!",
        "YOU GOT A COLOR", "THIS IS NOT REQUIRED", "START", "EXIT", "KICK OFF", "TIP OFF", "READY", "READY", "GOAL!",
        "%dP WINS", "DRAW", "ZERO GRAVITY", "MULTIBALL 1", "MULTIBALL 2", "MULTIBALL 3", "LANE COUNTS MISSION 1",
        "LANE COUNTS MISSION 2",
        "LANE COUNTS MISSION 3", "TOTAL LANE COUNTS", "TOTAL BUMPER COUNTS", "EXTRA BALL", "EXTRA CREDIT",
        "FREEZE OVER", "HIDDEN PATH DISCOVERY",
        "CIRCLE OF LIFE", "BUMPER VILLAGE", "PERFECT BUMPER VILLAGE", "LUCKY FLAMINGOS", "HUNGRY MONKEY",
        "COLOR PUZZLE", "MONEY MONEY MONEY",
        "UFO QUIZ SHOW", "MOVE ON 1", "MOVE ON 2", "SPIDER CRAB SHOOT-DOWN", "STOP THE FOUR SHAFTS 1",
        "STOP THE FOUR SHAFTS 2", "UFO SHOOT-DOWN",
        "CRAB BABY SHOOT-DOWN", "POINT OF NO RETURN 1", "POINT OF NO RETURN 2", "POINT OF NO RETURN 3",
        "LOOP THE LOOP 1", "LOOP THE LOOP 2",
        "LOOP THE LOOP 3", "CHU CHU MULTIBALL", "SPACE WARP", "ALIEN HILL", "AREA 74", "GALAXY TENNIS", "100 BLOCKS",
        "WARM-COLORED BLOCKS"
    ];

    private static readonly string[] Inputs =
    [
        "L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square", "Unknown 8", "Unknown 9", "Unknown A",
        "Unknown B", "DPad Up", "DPad Right", "DPad Down", "DPad Left"
    ];

    private readonly string[] _stageDir =
    [
        "JUNGLE1", "ISEKI1", "BOSS1", "RETRO1", "HIKARI1", "DEMO1", "JUNGLE2", "ISEKI2", "HIKARI2", "VS1", "VS2", "VS3",
        "VS4", "BOSS2", "BOSS3", "BOSS4"
    ];

    public string ChecksumPrimary => GetChecksum(false).PadLeft(8, '0') +
                                     (ConfirmChecksums(false) ? " (Valid)" : " (Invalid)");

    public string ChecksumSecondary => GetChecksum(true).PadLeft(8, '0') +
                                       (ConfirmChecksums(true) ? " (Valid)" : " (Invalid)");

    public long Score
    {
        get => GetCurrentScore();
        set => SetCurrentScore(value);
    }

    public string CurrentDifficulty
    {
        get => GetCurrentDifficulty();
        set => SetCurrentDifficulty(value);
    }

    public string CurrentStage
    {
        get => GetCurrentStage();
        set => SetCurrentStage(Array.IndexOf(_originalModes, value));
    }

    public string LastStage
    {
        get => GetLastPlayedStage();
        set => SetLastPlayedStage(value);
    }

    public int SfxVolume
    {
        get => _dataList.Count > 0 ? GetVolumeSfx() : 127;
        set => SetOption(Options.SfxVolume, (byte)value);
    }

    public int BgmVolume
    {
        get => _dataList.Count > 0 ? GetVolumeBgm() : 127;
        set => SetOption(Options.BgmVolume, (byte)value);
    }

    public bool Vibration
    {
        get => _dataList.Count <= 0 || GetVibration();
        set => SetOption(Options.Vibration, (byte)(value ? 0x00 : 0x01));
    }

    public int SoundMode
    {
        get => _dataList.Count > 0 ? ReadByte(0x10) : 0;
        set => SetOption(Options.SoundMode, (byte)value);
    }

    public int LeftFlipper
    {
        get => _dataList.Count > 0 ? ReadByte(0x23) : 0;
        set => SetControl(Control.LeftFlipper, (byte)value);
    }

    public int RightFlipper
    {
        get => _dataList.Count > 0 ? ReadByte(0x19) : 0;
        set => SetControl(Control.RightFlipper, (byte)value);
    }

    public int LeftNudge
    {
        get => _dataList.Count > 0 ? ReadByte(0x16) : 0;
        set => SetControl(Control.LeftNudge, (byte)value);
    }

    public int RightNudge
    {
        get => _dataList.Count > 0 ? ReadByte(0x17) : 0;
        set => SetControl(Control.RightNudge, (byte)value);
    }

    public int LeaderboardId { get; set; }
    public int StageId { get; set; }
    public int DataSourceId { get; set; }

    public Unlock[] FreeUnlocks
    {
        get
        {
            List<Unlock> unlocks = [];
            for (var i = 0; i < _originalModes.Length - 1; i++)
            {
                unlocks.Add(new Unlock(_originalModes[i], this, i, true));
            }

            return unlocks.ToArray();
        }
    }

    public Unlock[] OriginalUnlocks
    {
        get
        {
            List<Unlock> unlocks = [];
            for (var i = 0; i < _originalModes.Length - 1; i++)
            {
                unlocks.Add(new Unlock(_originalModes[i], this, i, false));
            }

            return unlocks.ToArray();
        }
    }

    public Mission[] Missions
    {
        get
        {
            List<Mission> missions = [];
            for (var rowIdx = 0; rowIdx < 0x64; rowIdx++)
            {
                try
                {
                    if (GetMissions(StageId)[rowIdx] == "(null)") continue;
                }
                catch
                {
                    continue;
                }
                missions.Add(new Mission(this, rowIdx, DataSourceId, StageId));
            }
            return missions.ToArray();
        }
    }

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

    public StageDirectory[] Dirs
    {
        get
        {
            List<StageDirectory> dirs = [];
            for (var i = 0; i < _originalModes.Length - 1; i++)
            {
                dirs.Add(new StageDirectory(this, i, _originalModes[i]));
            }
            return dirs.ToArray();
        }
    }

    private enum Options {
        SoundMode,
        SfxVolume,
        BgmVolume,
        Vibration
    }

    private enum Control {
        LeftNudge,
        RightNudge,
        LeftFlipper,
        RightFlipper
    }

    public enum Difficulty {
        Easy,
        Normal,
        Hard
    }

    public enum MissionSource {
        OriginalGame,
        FreePlay,
        LastPlayed
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
    private byte ReadByte(int addr, byte[]? data = null) {
        var dataList = _dataList;
        if (data != null) dataList = data.ToList();

        if (addr < dataList.Count) return dataList[addr];
        LogError("Offset " + addr + " out of range!");
        return 0x00;
    }

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

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SetLastPlayedStage(string value)
    {
        // do nothing
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

    private static string DecodeInput(byte input)
    {
        return input < Inputs.Length ? Inputs[input] : "Unknown";
    }

    private string DecodeString(byte idx)
    {
        return idx >= _validStrings.Length ? "(null)" : _validStrings[idx];
    }

    // general methods with abstraction layer
    
    // calculates the first checksum, found at offsets 0x8-0xb
    private int CalcChecksum1() {
        var gameData = ReadBytes(0xc, _dataList.Count - 0xc);
        JamCrc32 chkSum = new();
        return (int)(chkSum.ComputeChecksum(gameData)); // output 32-bit LE
    }

    // calculates the second checksum, found at offsets 0xc-0xf
    private int CalcChecksum2() {
        var gameData = ReadBytes(0x10, _dataList.Count - 0x10);
        JamCrc32 chkSum = new();
        return (int)(chkSum.ComputeChecksum(gameData)); // output 32-bit LE
    }

    private bool ConfirmChecksums(bool secondary) {
        return StaticUtils.GetInt32(_dataList.ToArray(), secondary ? 0xc : 0x8) == (secondary ? CalcChecksum2() : CalcChecksum1());
    }

    private string GetChecksum(bool secondary) {
        return StaticUtils.GetInt32(_dataList.ToArray(), secondary ? 0xc : 0x8).ToString("X");
    }

    public void UpdateChecksum() {
        var cs1 = BitConverter.GetBytes(CalcChecksum2());
        WriteByte(0x0C, cs1[0]);
        WriteByte(0x0D, cs1[1]);
        WriteByte(0x0E, cs1[2]);
        WriteByte(0x0F, cs1[3]);
        var cs2 = BitConverter.GetBytes(CalcChecksum1());
        WriteByte(0x08, cs2[0]);
        WriteByte(0x09, cs2[1]);
        WriteByte(0x0A, cs2[2]);
        WriteByte(0x0B, cs2[3]);
    }

    private bool IsLoaded() {
        return _dataList.Count != 0;
    }

    private bool IsValidSave() {
        return ConfirmChecksums(true) && ConfirmChecksums(false);
    }

    public bool IsValidHeader() {
        var reference = Convert.FromHexString("3402cb0f43553624");
        var actual = ReadBytes(0, 8);
        return reference.SequenceEqual(actual);
    }

    private long GetCurrentScore() {
        return StaticUtils.GetInt64( _dataList.ToArray(), 0x28);
    }

    private void SetCurrentScore(long value) {
        var scoreData = BitConverter.GetBytes(value);
        var offset = 0x28;
        foreach (var b in scoreData)
        {
            WriteByte(offset, b);
            offset++;
        }
    }

    private string GetCurrentDifficulty()
    {
        return ReadByte(0x30) switch
        {
            0 => "Easy",
            1 => "Normal",
            2 => "Hard",
            _ => "(null)"
        };
    }

    public int GetCurrentDifficultyIdx() {
        return ReadByte(0x30);
    }

    private void SetCurrentDifficulty(string difficulty) {
        byte diff = difficulty switch {
            "Normal" => 0x01,
            "Hard" => 0x02,
            _ => 0x00
        };
        WriteByte(0x30, diff);
    }

    private string GetCurrentStage() {
        var stageId = StaticUtils.GetInt32(_dataList.ToArray() , 0x10C8);
        var backupStageId = 11;
        for (var offset = 0x2756; offset > 0x274B; offset--) {
            if (ReadByte(offset) == 0x03) {
                break;
            }
            backupStageId--;
        }
        if (stageId == 0x0B) stageId = backupStageId;
        if ((stageId < _originalModes.Length) && (stageId >= 0)) {
            return _originalModes[stageId];
        }
        return "Out of range";
    }

    private string GetLastPlayedStage() {
        var started = false;
        var stgName = "N/A";
        var i = 0;
        for (var offset = 0x276C; offset < 0x2778; offset++) {
            var cByte = ReadByte(offset);
            if (cByte > 0x00) {
                started = cByte == 1;
                stgName = _originalModes[i];
                break;
            }
            i++;
        }

        if (stgName.Equals("N/A")) return stgName;
        if (started) return stgName + " (Started)";
        return stgName + " (Completed)";
    }

    private void SetCurrentStage(int value) {
        WriteByte(0x10C8, (byte)value);
    }

    public int GetExplicitStage() {
        return StaticUtils.GetInt32(_dataList.ToArray(), 0x10C8);
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


    // options
    public string GetSoundMode()
    {
        var soundMode = ReadByte(0x10);
        return soundMode == 0x00 ? "Mono" : "Stereo";
    }
    private int GetVolumeSfx() {
        return ReadByte(0x11);
    }
    private int GetVolumeBgm() {
        return ReadByte(0x12);
    }

    // true = on
    // false = off
    private bool GetVibration() {
        return ReadByte(0x13) == 0x00;
    }

    private void SetOption(Options idx, byte value) {
        WriteByte(0x10+(int)idx, value);
    }

    
    public string GetLeftFlipper() {
        return DecodeInput(ReadByte(0x23));
    }
    public string GetRightFlipper() {
        return DecodeInput(ReadByte(0x19));
    }
    public string GetLeftNudge() {
        return DecodeInput(ReadByte(0x16));
    }
    public string GetRightNudge() {
        return DecodeInput(ReadByte(0x17));
    }

    private void SetControl(Control control, byte value) {
        if (!IsLoaded()) return;
        var offset = 0x16;
        offset += (int)control;
        offset = control switch
        {
            Control.LeftFlipper => 0x23,
            Control.RightFlipper => 0x19,
            _ => offset
        };
        WriteByte(offset, value);
    }

    public bool[] GetUnlocks(bool isFreePlay) {
        var unlocks = new bool[12];
        var unlockBytes = ReadBytes(!isFreePlay ? 0x274C : 0x275C, 12);
        for (var i = 0; i < unlockBytes.Length; i++) {
            unlocks[i] = unlockBytes[i] == 0x03;
        }
        return unlocks;
    }

    public void ResetGame(bool isFreePlay) {
        var offset = 0x274C;
        if (isFreePlay) { offset+=0x10; }
        for (var i = offset; i < offset + 0x10; i++) {
            WriteByte(i, 0x00);
        }
    }
    public void WriteUnlock(bool isFreePlay, int idx, bool unlocked) {
        var offset = 0x274C;
        if (isFreePlay) { offset += 0x10; }
        offset += idx;
        WriteByte(offset , (byte)(unlocked?0x03:0x00));
    }

    public string[] GetMissionTypes(int idx) {
        try
        {
            if (0x194C + (idx * 0x64) >= _dataList.Count) {
                return [];
            }

            var offset = 0x194C + (idx * 0x40);
            List<string> types = [];

            while (offset < 0x194C + (idx * 0x40) + 0x40) {
                try
                {
                    types.Add(ReadByte(offset) > 0 ? "Red" : "Yellow");
                } catch (Exception) {
                    types.Add("Invalid");
                }
                offset += 2;
            }
            return types.ToArray();
        } catch (Exception) {
            string[] error = ["Corrupted data"];
            return error;
        }
    }

    public void SetMissionType(int stage, int idx, bool isRed) {
        if (0x194C + (stage * 0x40) + (idx*2) >= _dataList.Count) {
            return;
        }
        var offset = 0x194C + (stage * 0x40) + (idx*2);
        byte value = (byte) (isRed ? 0x01 : 0x00);
        WriteByte(offset, value);
    }


    public void SetMission(int stage, int idx, string value) {
        if (0x114C + (stage * 0x80) + idx >= _dataList.Count) {
            return;
        }
        var offset = 0x114C + (stage * 0x80) + (idx*4);
        byte stringIdx = 0;
        foreach (var str in _validStrings) {
            if (str.Equals(value)) {
                break;
            }
            stringIdx++;
        }
        WriteByte(offset, stringIdx);
    }

    public string[] GetMissions(int idx) {
        try
        {
            if (0x114C + (idx * 0x80) >= _dataList.Count) {
                return [];
            }

            var offset = 0x114C + (idx * 0x80);
            List<string> missions = [];

            while (offset < 0x114C + (idx * 0x80) + 0x80) {
                try {
                    var decodedString = DecodeString(ReadByte(offset));
                    if (!decodedString.Equals("ja")) {
                        missions.Add(decodedString);
                    }
                    offset += 4;
                } catch (Exception) {
                    missions.Add("Bad mission");
                    offset += 4;
                }
            }
            return missions.ToArray();
        } catch (Exception) {
            string[] error = ["Corrupted data"];
            return error;
        }
    }

    public string[] GetStageStatus(int idx, MissionSource ms)
    {
        var initialOffset = ms switch
        {
            MissionSource.FreePlay => 0x234C,
            MissionSource.OriginalGame => 0x214C,
            MissionSource.LastPlayed => 0x254C,
            _ => 0x00
        };
        if (initialOffset + (idx * 0x20) >= _dataList.Count) {
            return [];
        }

        var offset = initialOffset + (idx * 0x20);
        List<string> status = [];
        for (var x = offset; x < offset + 0x20; x++) {
            switch (ReadByte(x))
            {
                case 0x0:
                    status.Add("Not completed");
                    break;
                case 0x1:
                    status.Add("Started");
                    break;
                case 0x2:
                case 0x3:
                    status.Add("Completed");
                    break;
                default:
                    status.Add("Invalid");
                    break;
            }
        }
        return status.ToArray();
    }

    public void SetStageStatus(int stage, int idx, string value, MissionSource ms)
    {
        var initialOffset = ms switch
        {
            MissionSource.FreePlay => 0x234C,
            MissionSource.OriginalGame => 0x214C,
            MissionSource.LastPlayed => 0x254C,
            _ => 0x00
        };
        if (initialOffset + (stage * 0x20) + idx >= _dataList.Count) {
            return;
        }
        var offset = initialOffset + (stage * 0x20) + idx;
        byte val = value switch
        {
            "Started" => 0x01,
            "Completed" => 0x03,
            _ => 0x00
        };
        WriteByte(offset, val);

    }

    public string[] GetStageDirs() {
        List<string> dirs = [];
        for (var offset = 4300; offset < 4344; offset+=4) {
            try {
                dirs.Add(_stageDir[StaticUtils.GetInt32(_dataList.ToArray(), offset)]);
            } catch (Exception) {
                LogError("Failed to decode stage directory!");
            }
        }
        return dirs.ToArray();
    }

    public void SetStageDirFromString(int idx, string value)
    {
        var val = Array.IndexOf(_stageDir, value);
        SetStageDir(idx, val);
    }
    
    private void SetStageDir(int idx, int value) {
        WriteByte(4300+(byte)(idx*4), (byte)value);
    }

    public int[] GetMissionIndices(int idx) {
        var indices = new int[0x20];
        var i = 0;
        for (var offset = 0x1D4C + (idx * 0x20); offset < 0x1D4C + (idx * 0x20) + 0x20; offset++) {
            indices[i] = ReadByte(offset);
            i++;
        }
        return indices;
    }

    public int[] GetMissionPages(int idx) {
        var pages = new int[0x20];
        var i = 0;
        for (var offset = 0x1F4C + (idx * 0x20); offset < 0x1F4C + (idx * 0x20) + 0x20; offset++) {
            pages[i] = ReadByte(offset);
            i++;
        }
        return pages;
    }

    public void SetMissionPages(int stage, int row, int value) {
        var offset = 0x1F4C + (stage * 0x20) + row;
        WriteByte(offset, (byte)value);
    }

    public void SetMissionIndex(int stage, int row, int value) {
        var offset = 0x1D4C + (stage* 0x20) + row;
        WriteByte(offset, (byte)value);
    }


    public void ResetControls() {
        SetControl(Control.LeftFlipper, 0x0F);
        SetControl(Control.RightFlipper, 0x05);
        SetControl(Control.LeftNudge, 0x02);
        SetControl(Control.RightNudge, 0x03);
    }


    // diagnostics
    public string[] FixStructure() {
        List<string> fixes = [];
        if (SizeFix()) fixes.Add("Save size fix");
        if (HeaderFix()) fixes.Add("Header fix");
        if (FooterFix()) fixes.Add("Footer fix");
        if (CurrentStageFix()) fixes.Add("Current stage fix");
        if (InputFix()) fixes.Add("Controller input fix");
        if (StageDirFix()) fixes.Add("Stage directory fix");
        if (MissionCountFix()) fixes.Add("Mission count fix");
        if (ChecksumFix()) fixes.Add("Checksum fix"); // always do this one last
        return fixes.ToArray();
    }

    private bool FooterFix() {
        var reference = new byte[]{0x22,0x53,0x33,0x02};
        var actual = ReadBytes(_dataList.Count - 4, 4);
        var match = reference.SequenceEqual(actual);
        if (match) return false;
        WriteByte(0x277C, 0x22);
        WriteByte(0x277D, 0x53);
        WriteByte(0x277E, 0x33);
        WriteByte(0x277F, 0x02);
        return true;
    }

    private bool ChecksumFix() {
        if (IsValidSave()) return false;
        UpdateChecksum();
        return true;
    }

    private bool HeaderFix() {
        if (IsValidHeader()) return false;
        _dataList[0] = 0x34;
        _dataList[1] = 0x02;
        _dataList[2] = 0xCB;
        _dataList[3] = 0x0F;
        _dataList[4] = 0x43;
        _dataList[5] = 0x55;
        _dataList[6] = 0x36;
        _dataList[7] = 0x24;
        return true;
    }

    private bool SizeFix() {
        switch (_dataList.Count)
        {
            case 0x2780:
                return false;
            case > 0x2780:
            {
                var fixData = _dataList.Take(0x277F).ToArray();
                _dataList.Clear();
                _dataList.AddRange(fixData);
                return true;
            }
        }

        while (_dataList.Count < 0x2780) {
            _dataList.Add(0x00);
        }
        return true;
    }


    private int GetExpectedCount() {
        var expected = 0;
        for (var offset = 0x110C; offset < 0x110C+0x2C; offset+=4) {
            expected += StaticUtils.GetInt32(_dataList.ToArray(), offset);
        }
        return expected;
    }

    private bool CurrentStageFix() {
        var stageId = StaticUtils.GetInt32(_dataList.ToArray(), 0x10C8);
        if ((stageId <= _originalModes.Length - 1) && (stageId >= 0)) return false;
        WriteByte(0x10C8, 11);
        WriteByte(0x10C9, 0);
        WriteByte(0x10CA, 0);
        WriteByte(0x10CB, 0);
        return true;
    }

    private bool StageDirFix() {
        var appliedFixes = false;
        for (var offset = 4300; offset < 4344; offset+=4) {
            var stageDir = StaticUtils.GetInt32(_dataList.ToArray(), offset);
            if ((stageDir >= 0) && (stageDir <= _stageDir.Length - 1)) continue;
            WriteByte(offset, 0);
            WriteByte(offset+1, 0);
            WriteByte(offset+2, 0);
            WriteByte(offset+3, 0);
            appliedFixes = true;
        }
        return appliedFixes;
    }

    [SuppressMessage("ReSharper", "PatternIsRedundant")]
    private bool InputFix() {
        var appliedFixes = false;
        var leftFlipper = ReadByte(0x23);
        var rightFlipper = ReadByte(0x19);
        var leftNudge = ReadByte(0x16);
        var rightNudge = ReadByte(0x17);
        if (leftFlipper is > 0x0F or < 0) {
            SetControl(Control.LeftFlipper, 0x0F);
            appliedFixes = true;
        }
        if (rightFlipper is > 0x0F or < 0) {
            SetControl(Control.RightFlipper, 0x05);
            appliedFixes = true;
        }
        if (leftNudge is > 0x0F or < 0) {
            SetControl(Control.LeftNudge, 0x02);
            appliedFixes = true;
        }

        if (rightNudge is <= 0x0F and >= 0) return appliedFixes;
        SetControl(Control.RightNudge, 0x03);
        appliedFixes = true;
        return appliedFixes;
    }

    private bool MissionCountFix() {
        var totalMissions = 0;
        int i;
        for (i = 0; i < 11; i++) {
            totalMissions += GetMissions(i).Length;
        }
        if (totalMissions == GetExpectedCount()) return false;
        i = 0;
        for (var offset = 0x110C; offset < 0x110C+0x2C; offset+=4) {
            var expected = GetMissions(i).Length;
            var writeBytes = BitConverter.GetBytes(expected);
            var offsetB = offset;
            foreach (var b in writeBytes) {
                WriteByte(offsetB, b);
                offsetB++;
            }
            i++;
        }
        return true;
    }
}