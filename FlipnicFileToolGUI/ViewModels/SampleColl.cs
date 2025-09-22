namespace FlipnicFileToolGUI.ViewModels;

public class SampleColl
{
    public int Id { get; set; }
    public byte[] Data { get; set; }
    public int Offset { get; set; }
    
    public uint LoopStart { get; set; }
    
    public uint LoopEnd { get; set; }

    public string OffsetX => Offset.ToString("X");
    
    public string LoopStartX => LoopStart != LoopEnd ? LoopStart.ToString("X") : "Not applicable";    
    public string LoopEndX => LoopStart != LoopEnd ? LoopEnd.ToString("X") : "Not applicable";
}