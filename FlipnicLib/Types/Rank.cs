using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Rank(FpnSave save, int idx, int gameMode)
{

    private enum UpdatedValueType
    {
        Initials,
        Score,
        Combos,
        Difficulty
    }
    
    public int Position => int.Parse(SaveFile.GetScore(GameMode * 5 + idx)[0]);

    public string Initials
    {
        get => SaveFile.GetScore(GameMode * 5 + idx)[1];
        set => WriteChanges(UpdatedValueType.Initials, value);
    }

    public long Score
    {
        get => long.Parse(SaveFile.GetScore(GameMode * 5 + idx)[2]);
        set => WriteChanges(UpdatedValueType.Score, value);
    }

    public int Combos
    {
        get => int.Parse(SaveFile.GetScore(GameMode * 5 + idx)[3]);
        set => WriteChanges(UpdatedValueType.Combos, value);
    }

    public string Difficulty
    {
        get => SaveFile.GetScore(GameMode * 5 + idx)[5];
        set => WriteChanges(UpdatedValueType.Difficulty, value);
    }

    public string Offset => SaveFile.GetScore(GameMode * 5 + idx)[4];

    private int GameMode { get; } = gameMode;

    private FpnSave SaveFile { get; init; } = save;

    private void WriteChanges(UpdatedValueType updatedType, object newValue)
    {
        var combos = (int)(updatedType == UpdatedValueType.Combos ? newValue : Combos);
        var initials = (string)(updatedType == UpdatedValueType.Initials ? newValue : Initials);
        var score = (long)((updatedType == UpdatedValueType.Score) ? newValue : Score);
        var diff = (updatedType == UpdatedValueType.Difficulty ? newValue : Difficulty) switch
        {
            "Easy" => FpnSave.Difficulty.Easy,
            "Normal" => FpnSave.Difficulty.Normal,
            "Hard" => FpnSave.Difficulty.Hard,
            _ => FpnSave.Difficulty.Null
        };
        SaveFile.SetScore(GameMode, idx, score, initials, combos, diff);
    }
}