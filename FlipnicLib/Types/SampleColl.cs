namespace FlipnicLib.Types;

public class SampleColl
{
    /// <summary>
    /// Identifier (ascending)
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Sample data
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    /// Physical offset of the sample relative to the beginning of the .BD file
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// Physical offset for the start of the loop (may not be accurate)
    /// </summary>
    public uint LoopStart { get; set; }
    
    /// <summary>
    /// Physical offset for the end of the loop (may not be accurate)
    /// </summary>
    public uint LoopEnd { get; set; }

    /// <summary>
    /// Formatted physical offset of the sample
    /// </summary>
    public string OffsetX => Offset.ToString("X");
    
    /// <summary>
    /// Formatted physical offset for the start of the loop
    /// </summary>
    public string LoopStartX => LoopStart != LoopEnd ? LoopStart.ToString("X") : "Not applicable";    
    
    /// <summary>
    /// Formatted physical offset for the end of the loop
    /// </summary>
    public string LoopEndX => LoopStart != LoopEnd ? LoopEnd.ToString("X") : "Not applicable";
}