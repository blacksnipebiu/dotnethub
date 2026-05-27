
namespace DotNetHub.Server.Models;

public class ChunkStatus
{
    public string FileName { get; set; } = "";
    public int TotalChunks { get; set; }
    public List<int> ReceivedChunks { get; set; } = new();
    public bool Complete { get; set; }
}
