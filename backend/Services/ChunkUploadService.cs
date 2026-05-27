
using System.IO.Compression;
using System.Text;
using DotNetHub.Server.Models;

namespace DotNetHub.Server.Services;

public class ChunkUploadService
{
    private readonly ILogger<ChunkUploadService> _logger;
    
    public ChunkUploadService(ILogger<ChunkUploadService> logger)
    {
        _logger = logger;
    }
    
    public async Task<ChunkStatus> SaveChunk(string projectPath, string fileName, int chunkIndex, int totalChunks, Stream chunkStream)
    {
        var chunkDir = Path.Combine(projectPath, "_chunks", SanitizeFileName(fileName));
        Directory.CreateDirectory(chunkDir);
        
        var chunkFile = Path.Combine(chunkDir, $"part_{chunkIndex:D6}");
        using var fs = File.Create(chunkFile);
        await chunkStream.CopyToAsync(fs);
        
        return GetStatus(projectPath, fileName, totalChunks);
    }
    
    public ChunkStatus GetStatus(string projectPath, string fileName, int totalChunks)
    {
        var chunkDir = Path.Combine(projectPath, "_chunks", SanitizeFileName(fileName));
        var received = new List<int>();
        if (Directory.Exists(chunkDir))
        {
            foreach (var f in Directory.GetFiles(chunkDir, "part_*"))
            {
                var name = Path.GetFileName(f);
                if (int.TryParse(name.Replace("part_", ""), out var idx))
                    received.Add(idx);
            }
        }
        return new ChunkStatus
        {
            FileName = fileName,
            TotalChunks = totalChunks,
            ReceivedChunks = received,
            Complete = received.Count >= totalChunks
        };
    }
    
    public async Task<string> CombineAndExtract(string projectPath, string fileName, string projectName, int totalChunks)
    {
        var chunkDir = Path.Combine(projectPath, "_chunks", SanitizeFileName(fileName));
        var zipPath = Path.Combine(projectPath, projectName + ".zip");
        
        // Combine chunks
        using (var fs = File.Create(zipPath))
        {
            for (int i = 0; i < totalChunks; i++)
            {
                var chunkFile = Path.Combine(chunkDir, $"part_{i:D6}");
                if (!File.Exists(chunkFile))
                    throw new FileNotFoundException($"缺少分块 {i}");
                using var cf = File.OpenRead(chunkFile);
                await cf.CopyToAsync(fs);
            }
        }
        
        // Clean chunks directory
        Directory.Delete(chunkDir, true);
        
        // Extract with encoding detection
        using (var ms = new MemoryStream(await File.ReadAllBytesAsync(zipPath)))
        {
            ZipArchive? archive = null;
            try
            {
                archive = new ZipArchive(ms, ZipArchiveMode.Read, true, Encoding.UTF8);
                if (archive.Entries.Any(e => e.Name.Contains('\ufffd')))
                    throw new Exception("Encoding mismatch");
            }
            catch
            {
                archive?.Dispose();
                ms.Position = 0;
                archive = new ZipArchive(ms, ZipArchiveMode.Read, true, Encoding.GetEncoding(936));
            }
            using (archive)
            {
                archive.ExtractToDirectory(projectPath, true);
            }
        }
        
        // Delete zip after extraction
        File.Delete(zipPath);
        
        _logger.LogInformation("Chunks combined and extracted for {FileName}", fileName);
        return zipPath;
    }
    
    public void CleanChunks(string projectPath)
    {
        var chunkRoot = Path.Combine(projectPath, "_chunks");
        if (Directory.Exists(chunkRoot))
            Directory.Delete(chunkRoot, true);
    }
    
    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
