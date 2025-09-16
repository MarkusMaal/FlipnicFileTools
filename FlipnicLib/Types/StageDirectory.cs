namespace FlipnicLib.Types;

public class StageDirectory(FpnSave save, int idx, string name)
{
    public string StageName => name;
    
    public string StageDir
    {
        get => save.getStageDirs()[idx];
        set => save.SetStageDirFromString(idx, value);
    }
}