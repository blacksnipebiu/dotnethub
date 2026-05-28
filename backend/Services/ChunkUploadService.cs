using System.Text;
using DotNetHub.Server.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;

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

        // 1. 提前验证目录存在
        if (!Directory.Exists(chunkDir))
            throw new DirectoryNotFoundException($"分块目录不存在: {chunkDir}");

        // 2. 合并分块
        _logger.LogInformation("[Combine] Step1 — combining {TotalChunks} chunks into {ZipPath}", totalChunks, zipPath);
        await CombineChunksAsync(chunkDir, zipPath, totalChunks);

        // 3. 清理分块目录
        _logger.LogInformation("[Combine] Step2 — cleaning chunks");
        try { Directory.Delete(chunkDir, true); }
        catch (Exception ex) { _logger.LogWarning(ex, "[Combine] Failed to delete chunk dir {ChunkDir}", chunkDir); }

        // 4. 解压 ZIP（使用 SharpCompress）
        _logger.LogInformation("[Combine] Step3 — extracting zip with SharpCompress");
        await ExtractZipWithSharpCompressAsync(zipPath, projectPath);

        // 5. 清理临时 ZIP
        _logger.LogInformation("[Combine] Step4 — cleaning zip");
        try { File.Delete(zipPath); }
        catch (Exception ex) { _logger.LogWarning(ex, "[Combine] Failed to delete zip {ZipPath}", zipPath); }

        _logger.LogInformation("[Combine] DONE successfully");
        return zipPath;
    }

    // 流式合并分块
    private async Task CombineChunksAsync(string chunkDir, string zipPath, int totalChunks)
    {
        await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        for (int i = 0; i < totalChunks; i++)
        {
            var chunkFile = Path.Combine(chunkDir, $"part_{i:D6}");
            _logger.LogDebug("[Combine]   chunk {Index}: {ChunkFile}", i, chunkFile);

            if (!File.Exists(chunkFile))
                throw new FileNotFoundException($"缺少分块 {i}", chunkFile);

            await using var chunkStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await chunkStream.CopyToAsync(fs);
        }

        _logger.LogInformation("[Combine] Combined {TotalChunks} chunks, total size={Size} bytes", totalChunks, new FileInfo(zipPath).Length);
    }

    // 使用 SharpCompress 解压（支持 Deflate64 等多种压缩算法）
    private async Task ExtractZipWithSharpCompressAsync(string zipPath, string extractPath)
    {
        Directory.CreateDirectory(extractPath);

        var encodingsToTry = new[]
        {
            Encoding.UTF8,
            Encoding.GetEncoding("GBK"),
            Encoding.GetEncoding("BIG5"),
            Encoding.Default
        };

        Exception? lastException = null;

        foreach (var encoding in encodingsToTry)
        {
            _logger.LogInformation("[Combine]   trying encoding: {EncodingName}", encoding.EncodingName);

            try
            {
                // 关键修复点 1：使用 ArchiveFactory.OpenArchive
                using var archive = ArchiveFactory.OpenArchive(zipPath, new ReaderOptions
                {
                    LeaveStreamOpen = false,
                    ArchiveEncoding = new ArchiveEncoding { Default = encoding }
                });

                var totalEntries = archive.Entries.Count();
                var processedEntries = 0;
                var dirCount = 0;
                var fileCount = 0;

                _logger.LogInformation("[Combine]   processing {TotalEntries} entries with {EncodingName}", totalEntries, encoding.EncodingName);

                foreach (var entry in archive.Entries)
                {
                    processedEntries++;

                    // 跳过无路径的条目
                    if (string.IsNullOrEmpty(entry.Key)) continue;

                    // 标准化路径分隔符
                    var entryPath = entry.Key.Replace('\\', '/');
                    var fullPath = Path.GetFullPath(Path.Combine(extractPath, entryPath));
                    ValidatePathSecurity(fullPath, extractPath);

                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(fullPath);
                        dirCount++;
                    }
                    else
                    {
                        var destDir = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(destDir))
                            Directory.CreateDirectory(destDir);

                        // 关键修复点 2：使用手动流式解压，兼容性最好
                        using var entryStream = entry.OpenEntryStream();
                        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                        await entryStream.CopyToAsync(fileStream);
                        fileCount++;
                    }

                    if (processedEntries % 100 == 0)
                    {
                        _logger.LogDebug("[Combine]   processed {Processed}/{Total} entries", processedEntries, totalEntries);
                    }
                }

                _logger.LogInformation("[Combine]   extracted: {DirCount} dirs, {FileCount} files", dirCount, fileCount);
                _logger.LogInformation("[Combine]   encoding {EncodingName} succeeded", encoding.EncodingName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Combine]   encoding {EncodingName} failed: {Message}", encoding.EncodingName, ex.Message);
                lastException = ex;
                CleanDirectory(extractPath);
            }
        }

        throw new InvalidOperationException($"无法解压 ZIP 文件（已尝试 {encodingsToTry.Length} 种编码）", lastException);
    }

    // 路径安全检查
    private static void ValidatePathSecurity(string fullPath, string basePath)
    {
        var normalizedFull = Path.GetFullPath(fullPath);
        var normalizedBase = Path.GetFullPath(basePath);

        if (!normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"路径遍历攻击: {fullPath}");
    }

    // 清理目录
    private void CleanDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        try
        {
            foreach (var dir in Directory.GetDirectories(path))
                Directory.Delete(dir, true);
            foreach (var file in Directory.GetFiles(path))
                File.Delete(file);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Combine]   failed to clean directory {Path}", path);
        }
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