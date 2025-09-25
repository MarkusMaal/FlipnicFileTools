using FlipnicLib.Formats;

namespace FlipnicLib.Types;

public class Unlock(string label, FpnSave save, int idx, bool isFreePlay)
{
    public string Label => label;
    private FpnSave Save => save;
    private bool IsFreePlay => isFreePlay;

    public bool IsUnlocked
    {
        get => Save.GetUnlocks(IsFreePlay)[idx];
        set => Save.WriteUnlock(IsFreePlay, idx, value);
    } 
}