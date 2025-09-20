using SASRip.Data;
using SASRip.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

namespace SASRip.Services;

public class MediaCacheCleanupWatchdog
{
    private int lifetime;
    private string fileLocation;
    private bool verboseLogging;

    private IMediaCache mediaCache;
    private Timer scanTimer;

    public MediaCacheCleanupWatchdog(int scanRepeatTime, int lifetime, string fileLocation, IMediaCache mediaCache, bool verboseLogging = false, bool cleanOnStartup = true)
    {
        this.mediaCache = mediaCache;
        this.lifetime = lifetime;
        this.fileLocation = fileLocation;
        this.verboseLogging = verboseLogging;

        Console.WriteLine($"[MediaCacheCleanupWatchdog] Initializing cleanup watchdog with scan interval: {scanRepeatTime}s, lifetime: {lifetime}s, location: {fileLocation}");

        if (cleanOnStartup)
        {
            Console.WriteLine("[MediaCacheCleanupWatchdog] Performing startup cleanup");
            ClearAllMedia();
        }
        else
        {
            Console.WriteLine("[MediaCacheCleanupWatchdog] Skipping startup cleanup");
        }

        scanTimer = new Timer(1000 * scanRepeatTime);
        scanTimer.Elapsed += Cleanup;
        scanTimer.AutoReset = true;
        scanTimer.Enabled = true;
        scanTimer.Start();

        Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup timer started with {scanRepeatTime}s interval");
    }

    ~MediaCacheCleanupWatchdog()
    {
        Console.WriteLine("[MediaCacheCleanupWatchdog] Cleanup watchdog finalizing - stopping timer");
        scanTimer.Stop();
        scanTimer.Dispose();
    }

    private void ClearAllMedia()
    {
        string relativePath = fileLocation + "/";
        string path = Path.GetFullPath(relativePath);

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearAllMedia: Checking path {path}");
        }

        if (Directory.Exists(path))
        {
            string[] folders = Directory.GetDirectories(path);
            Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearAllMedia: Found {folders.Length} directories to clear");

            foreach (string folder in folders)
            {
                if (verboseLogging)
                {
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearAllMedia: Deleting directory {folder}");
                }
                RecursiveDelete(folder);
            }

            Console.WriteLine("[MediaCacheCleanupWatchdog] ClearAllMedia: Startup cleanup completed");
        }
        else
        {
            Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearAllMedia: Path {path} does not exist, skipping cleanup");
        }
    }

    private void Cleanup(Object source, ElapsedEventArgs e)
    {
        List<string> keys = mediaCache.MediaCacheStatus.Keys.ToList();
        int processedCount = 0;
        int removedCount = 0;
        int errorCount = 0;

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Starting cleanup scan with {keys.Count} cache entries");
        }

        foreach (string key in keys)
        {
            processedCount++;
            string path = mediaCache.MediaCacheStatus[key].AbsolutePath;
            CacheInfo cacheEntry = mediaCache.MediaCacheStatus[key];
            TimeSpan age = DateTime.Now.Subtract(cacheEntry.TimeOfCreation);

            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Processing key '{key}', status: {cacheEntry.Status}, age: {age.TotalSeconds:F1}s, path: '{path}'");
            }

            // Handle entries with empty/null paths (often from failed downloads or queued items)
            if (string.IsNullOrEmpty(path))
            {
                bool isEmptyPathFailed = cacheEntry.Status == CacheInfo.Statuses.Failed;
                bool isEmptyPathExpired = age.TotalSeconds > lifetime;

                if (verboseLogging)
                {
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Empty path entry - Failed: {isEmptyPathFailed}, Expired: {isEmptyPathExpired}");
                }

                // Remove failed entries or old processing entries with no path
                if (isEmptyPathFailed || isEmptyPathExpired)
                {
                    mediaCache.MediaCacheStatus.Remove(key);
                    removedCount++;
                    if (verboseLogging)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Removed empty path entry '{key}'");
                    }
                }
                continue;
            }

            bool lifetimeExceeded = age.TotalSeconds > lifetime;
            bool isFailed = cacheEntry.Status == CacheInfo.Statuses.Failed;
            string parent;

            try
            {
                parent = new DirectoryInfo(path).Parent.FullName;
            }
            catch (Exception ex)
            {
                // Path exists but is invalid (malformed, access denied, etc.)
                // Remove the invalid entry and continue with other entries
                Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Error processing path '{path}' for key '{key}': {ex.Message}");
                mediaCache.MediaCacheStatus.Remove(key);
                removedCount++;
                errorCount++;
                continue;
            }

            if (lifetimeExceeded || isFailed)
            {
                if (verboseLogging)
                {
                    string reason = lifetimeExceeded ? "lifetime exceeded" : "failed status";
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Removing entry '{key}' - {reason}");
                }

                if (Directory.Exists(parent))
                {
                    if (verboseLogging)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Deleting directory '{parent}'");
                    }

                    try
                    {
                        RecursiveDelete(parent);
                        if (!Directory.Exists(parent))
                        {
                            mediaCache.MediaCacheStatus.Remove(key);
                            removedCount++;
                            if (verboseLogging)
                            {
                                Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Successfully removed entry '{key}' and deleted directory");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Warning - Directory '{parent}' still exists after deletion attempt");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Error deleting directory '{parent}': {ex.Message}");
                        errorCount++;
                    }
                }
                else
                {
                    // Directory doesn't exist, just remove from cache
                    mediaCache.MediaCacheStatus.Remove(key);
                    removedCount++;
                    if (verboseLogging)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Directory '{parent}' doesn't exist, removed cache entry '{key}'");
                    }
                }
            }
        }

        if (verboseLogging || removedCount > 0 || errorCount > 0)
        {
            Console.WriteLine($"[MediaCacheCleanupWatchdog] Cleanup: Scan completed - Processed: {processedCount}, Removed: {removedCount}, Errors: {errorCount}");
        }
    }

    private void RecursiveDelete(string path)
    {
        if (!Directory.Exists(path))
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Path '{path}' does not exist");
            }
            return;
        }

        try
        {
            string[] subdirectories = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);

            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Deleting '{path}' - {subdirectories.Length} subdirs, {files.Length} files");
            }

            foreach (string folder in subdirectories)
            {
                RecursiveDelete(folder);
            }

            foreach (string file in files)
            {
                try
                {
                    // Clear file attributes before deletion (important for Linux compatibility)
                    ClearFileAttributes(file);
                    File.Delete(file);
                    if (verboseLogging)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Deleted file '{file}'");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Permission denied for file '{file}': {ex.Message}");
                    // Try to change permissions on Linux/Unix systems
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        TryChangeFilePermissions(file);
                        try
                        {
                            File.Delete(file);
                            if (verboseLogging)
                            {
                                Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Deleted file '{file}' after permission change");
                            }
                        }
                        catch (Exception retryEx)
                        {
                            Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Still unable to delete file '{file}': {retryEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Error deleting file '{file}': {ex.Message}");
                }
            }

            try
            {
                Directory.Delete(path);
                if (verboseLogging)
                {
                    Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Deleted directory '{path}'");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Permission denied for directory '{path}': {ex.Message}");
                // Try to change directory permissions on Linux/Unix systems
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    TryChangeDirectoryPermissions(path);
                    try
                    {
                        Directory.Delete(path);
                        if (verboseLogging)
                        {
                            Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Deleted directory '{path}' after permission change");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Still unable to delete directory '{path}': {retryEx.Message}");
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Error deleting directory '{path}': {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MediaCacheCleanupWatchdog] RecursiveDelete: Error deleting directory '{path}': {ex.Message}");
            throw;
        }
    }

    private void ClearFileAttributes(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ||
                    (attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                    (attributes & FileAttributes.System) == FileAttributes.System)
                {
                    // Remove problematic attributes
                    attributes &= ~FileAttributes.ReadOnly;
                    attributes &= ~FileAttributes.Hidden;
                    attributes &= ~FileAttributes.System;
                    File.SetAttributes(filePath, attributes);

                    if (verboseLogging)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearFileAttributes: Cleared attributes for '{filePath}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] ClearFileAttributes: Unable to clear attributes for '{filePath}': {ex.Message}");
            }
        }
    }

    private void TryChangeFilePermissions(string filePath)
    {
        try
        {
            // On Unix-like systems, try to make file writable using chmod equivalent
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    // Try to set write permissions for owner
                    System.Diagnostics.Process process = new System.Diagnostics.Process()
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"u+w \"{filePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();

                    if (verboseLogging && process.ExitCode == 0)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] TryChangeFilePermissions: Changed permissions for '{filePath}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] TryChangeFilePermissions: Failed to change permissions for '{filePath}': {ex.Message}");
            }
        }
    }

    private void TryChangeDirectoryPermissions(string dirPath)
    {
        try
        {
            // On Unix-like systems, try to make directory writable using chmod equivalent
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if (dirInfo.Exists)
                {
                    // Try to set write permissions for owner
                    System.Diagnostics.Process process = new System.Diagnostics.Process()
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"u+w \"{dirPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();

                    if (verboseLogging && process.ExitCode == 0)
                    {
                        Console.WriteLine($"[MediaCacheCleanupWatchdog] TryChangeDirectoryPermissions: Changed permissions for '{dirPath}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheCleanupWatchdog] TryChangeDirectoryPermissions: Failed to change permissions for '{dirPath}': {ex.Message}");
            }
        }
    }
}
