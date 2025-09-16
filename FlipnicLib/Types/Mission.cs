namespace FlipnicLib.Types;

public class Mission(FpnSave save, int rowIdx, int dataSource, int stage)
{
    private int DataSource => dataSource;
    private int RowIndex => rowIdx;
    private int Stage => stage;
    private FpnSave Save => save;

    public string MissionColor
    {
        get => Save.GetMissionTypes(DataSource)[RowIndex];
        set => Save.SetMissionType(Stage, RowIndex, value == "Red");
    }

    public string Status
    {
        get => Save.GetStageStatus(Stage, (FpnSave.MissionSource)dataSource)[RowIndex];
        set => Save.SetStageStatus(Stage, RowIndex, value, (FpnSave.MissionSource)dataSource);
    }

    public string MissionLabel
    {
        get => Save.GetMissions(Stage)[RowIndex];
        set => Save.SetMission(Stage, RowIndex, value);
    }

    public int ThumbnailIndex
    {
        get => Save.GetMissionIndicies(Stage)[RowIndex];
        set => Save.SetMissionIndex(Stage, RowIndex, value);
    }

    public int ThumbnailPages
    {
        get => Save.GetMissionPages(Stage)[RowIndex];
        set => Save.SetMissionPages(Stage, RowIndex, value);
    }
}