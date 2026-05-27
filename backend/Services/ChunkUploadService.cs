
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
        _logger.LogInformation("[Combine] START — path={ProjectPath}, file={FileName}, chunks={TotalChunks}", 
            projectPath, fileName, totalChunks);
        
        var chunkDir = Path.Combine(projectPath, "_chunks", SanitizeFileName(fileName));
        var zipPath = Path.Combine(projectPath, projectName + ".zip");
        
        // Step 1: Combine chunks
        _logger.LogInformation("[Combine] Step1 — combining {TotalChunks} chunks into {ZipPath}", totalChunks, zipPath);
        try
        {
            using (var fs = File.Create(zipPath))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkFile = Path.Combine(chunkDir, $"part_{i:D6}");
                    _logger.LogDebug("[Combine]   chunk {Index}: {ChunkFile} exists={Exists}", i, chunkFile, File.Exists(chunkFile));
                    if (!File.Exists(chunkFile))
                        throw new FileNotFoundException($"缺少分块 {i}");
                    using var cf = File.OpenRead(chunkFile);
                    await cf.CopyToAsync(fs);
                }
            }
            _logger.LogInformation("[Combine] Step1 — combined OK, zip size={Size}", new FileInfo(zipPath).Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Combine] Step1 FAILED");
            throw;
        }
        
        // Step 2: Clean chunks
        _logger.LogInformation("[Combine] Step2 — deleting chunk dir {ChunkDir}", chunkDir);
        Directory.Delete(chunkDir, true);
        
        // Step 3: Read zip into memory
        _logger.LogInformation("[Combine] Step3 — reading zip into memory");
        byte[] zipBytes;
        try { zipBytes = await File.ReadAllBytesAsync(zipPath); }
        catch (Exception ex) { _logger.LogError(ex, "[Combine] Step3 FAILED"); throw; }
        _logger.LogInformation("[Combine] Step3 — read {ByteCount} bytes OK", zipBytes.Length);
        
        // Step 4: Extract with UTF-8 encoding, safe extraction
        _logger.LogInformation("[Combine] Step4 — extracting with UTF-8 encoding");
        using (var ms = new MemoryStream(zipBytes))
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Read, true, Encoding.UTF8))
        {
            _logger.LogInformation("[Combine]   entries={Count}", archive.Entries.Count);
            var basePath = Path.GetFullPath(projectPath);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                var dest = Path.GetFullPath(Path.Combine(basePath, entry.FullName));
                if (!dest.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"路径遍历攻击: {entry.FullName}");
                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(dest, true);
            }
        }
        _logger.LogInformation("[Combine] Step4 — extract OK");

        // Step 5: Clean up zip
        _logger.LogInformation("[Combine] Step5 — deleting zip {ZipPath}", zipPath);
        File.Delete(zipPath);
        
        _logger.LogInformation("[Combine] DONE successfully");
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
