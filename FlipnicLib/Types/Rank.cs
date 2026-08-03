using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Rank
{
    private readonly int _idx;

    public Rank()
    {
        
    }

    public Rank(FpnSave save, int idx, int gameMode)
    {
        _idx = idx;
        GameMode = gameMode;
        SaveFile = save;
    }

    private enum UpdatedValueType
    {
        Name,
        Score,
        Combos,
        Difficulty
    }
    
    public int Position => int.Parse(SaveFile.GetScore(GameMode * 5 + _idx)[0]);

    public string Name
    {
        get => SaveFile.GetScore(GameMode * 5 + _idx)[1];
        set => WriteChanges(UpdatedValueType.Name, value);
    }

    public long Score
    {
        get => long.Parse(SaveFile.GetScore(GameMode * 5 + _idx)[2]);
        set => WriteChanges(UpdatedValueType.Score, value);
    }

    public int Combos
    {
        get => int.Parse(SaveFile.GetScore(GameMode * 5 + _idx)[3]);
        set => WriteChanges(UpdatedValueType.Combos, value);
    }

    public string Difficulty
    {
        get => SaveFile.GetScore(GameMode * 5 + _idx)[5];
        set => WriteChanges(UpdatedValueType.Difficulty, value);
    }

    public string Offset => SaveFile.GetScore(GameMode * 5 + _idx)[4];

    private int GameMode { get; }

    private FpnSave SaveFile { get; init; }

    private void WriteChanges(UpdatedValueType updatedType, object newValue)
    {
        var combos = (int)(updatedType == UpdatedValueType.Combos ? newValue : Combos);
        var initials = (string)(updatedType == UpdatedValueType.Name ? newValue : Name);
        var score = (long)((updatedType == UpdatedValueType.Score) ? newValue : Score);
        var diff = ((updatedType == UpdatedValueType.Difficulty ? newValue : Difficulty).ToString() ?? "").ToLower() switch
        {
            "easy" => FpnSave.Difficulty.Easy,
            "normal" => FpnSave.Difficulty.Normal,
            "hard" => FpnSave.Difficulty.Hard,
            _ => FpnSave.Difficulty.Null
        };
        SaveFile.SetScore(GameMode, _idx, score, initials, combos, diff);
    }
}