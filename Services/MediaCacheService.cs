using SASRip.Data;
using SASRip.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace SASRip.Services;

public class MediaCacheService : IMediaCache
{
    public Dictionary<string, CacheInfo> MediaCacheStatus { get; set; }
    private readonly bool verboseLogging;

    public MediaCacheService(IConfigurationService cfg)
    {
        verboseLogging = cfg.EnableVerboseLogging;
        MediaCacheStatus = new Dictionary<string, CacheInfo>();

        Console.WriteLine("[MediaCacheService] Initializing media cache service");
        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] Verbose logging enabled");
            Console.WriteLine($"[MediaCacheService] Cache checkup time: {cfg.CachedMediaCheckupTimeSeconds}s");
            Console.WriteLine($"[MediaCacheService] Cache lifetime: {cfg.CachedMediaLifeTimeSeconds}s");
            Console.WriteLine($"[MediaCacheService] File output path: {cfg.FileOutputPath}");
        }

        // Start Background Cleanup Service.
        MediaCacheCleanupWatchdog clearingService = new MediaCacheCleanupWatchdog(cfg.CachedMediaCheckupTimeSeconds, cfg.CachedMediaLifeTimeSeconds, cfg.FileOutputPath, this, verboseLogging);

        Console.WriteLine("[MediaCacheService] Media cache service initialized with background cleanup");
    }

    // Chaching Helper Methods
    public void MarkAsQueued(string key, string path)
    {
        // Sanitize inputs for cross-platform compatibility
        string sanitizedKey = SanitizeKey(key);
        string validatedPath = ValidateAndSanitizePath(path);

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] MarkAsQueued: Key='{key}' (sanitized: '{sanitizedKey}'), Path='{path}' (validated: '{validatedPath}')");
        }

        if (!MediaCacheStatus.ContainsKey(sanitizedKey))
        {
            MediaCacheStatus.Add(sanitizedKey, new CacheInfo(CacheInfo.Statuses.Processing, DateTime.Now, validatedPath));
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsQueued: Added new cache entry for key '{sanitizedKey}'");
            }
        }
        else
        {
            CacheInfo.Statuses oldStatus = MediaCacheStatus[sanitizedKey].Status;
            MediaCacheStatus[sanitizedKey] = new CacheInfo(CacheInfo.Statuses.Processing, DateTime.Now, validatedPath);
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsQueued: Updated existing cache entry for key '{sanitizedKey}' (was {oldStatus})");
            }
        }

        Console.WriteLine($"[MediaCacheService] Cache entry '{sanitizedKey}' marked as queued for processing");
    }

    public void MarkAsDone(string key, string path)
    {
        // Sanitize inputs for cross-platform compatibility
        string sanitizedKey = SanitizeKey(key);
        string validatedPath = ValidateAndSanitizePath(path);

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] MarkAsDone: Key='{key}' (sanitized: '{sanitizedKey}'), Path='{path}' (validated: '{validatedPath}')");
        }

        if (MediaCacheStatus.ContainsKey(sanitizedKey))
        {
            CacheInfo.Statuses oldStatus = MediaCacheStatus[sanitizedKey].Status;
            TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[sanitizedKey].TimeOfCreation);
            MediaCacheStatus[sanitizedKey].Status = CacheInfo.Statuses.Ready;
            MediaCacheStatus[sanitizedKey].AbsolutePath = validatedPath;
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsDone: Updated existing cache entry for key '{sanitizedKey}' (was {oldStatus}, processing time: {age.TotalSeconds:F1}s)");
            }
        }
        else
        {
            MediaCacheStatus.Add(sanitizedKey, new CacheInfo(CacheInfo.Statuses.Ready, DateTime.Now, validatedPath));
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsDone: Added new ready cache entry for key '{sanitizedKey}'");
            }
        }

        Console.WriteLine($"[MediaCacheService] Cache entry '{sanitizedKey}' marked as ready");
    }

    public void MarkAsFailed(string key, string path)
    {
        // Sanitize inputs for cross-platform compatibility
        string sanitizedKey = SanitizeKey(key);
        string validatedPath = ValidateAndSanitizePath(path);

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] MarkAsFailed: Key='{key}' (sanitized: '{sanitizedKey}'), Path='{path}' (validated: '{validatedPath}')");
        }

        if (!MediaCacheStatus.ContainsKey(sanitizedKey))
        {
            MediaCacheStatus.Add(sanitizedKey, new CacheInfo(CacheInfo.Statuses.Failed, DateTime.Now, validatedPath));
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsFailed: Added new failed cache entry for key '{sanitizedKey}'");
            }
        }
        else
        {
            CacheInfo.Statuses oldStatus = MediaCacheStatus[sanitizedKey].Status;
            TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[sanitizedKey].TimeOfCreation);
            MediaCacheStatus[sanitizedKey].Status = CacheInfo.Statuses.Failed;
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] MarkAsFailed: Updated existing cache entry for key '{sanitizedKey}' (was {oldStatus}, processing time: {age.TotalSeconds:F1}s)");
            }
        }

        Console.WriteLine($"[MediaCacheService] Cache entry '{sanitizedKey}' marked as failed");
    }

    public void ExtendCacheTime(string key)
    {
        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] ExtendCacheTime: Key='{key}'");
        }

        if (MediaCacheStatus.ContainsKey(key) && MediaCacheStatus[key].Status == CacheInfo.Statuses.Ready)
        {
            DateTime oldTime = MediaCacheStatus[key].TimeOfCreation;
            MediaCacheStatus[key].TimeOfCreation = DateTime.Now;
            if (verboseLogging)
            {
                TimeSpan oldAge = DateTime.Now.Subtract(oldTime);
                Console.WriteLine($"[MediaCacheService] ExtendCacheTime: Extended cache time for key '{key}' (was {oldAge.TotalSeconds:F1}s old)");
            }
            else
            {
                Console.WriteLine($"[MediaCacheService] Extended cache time for key '{key}'");
            }
        }
        else if (verboseLogging)
        {
            if (!MediaCacheStatus.ContainsKey(key))
            {
                Console.WriteLine($"[MediaCacheService] ExtendCacheTime: Key '{key}' not found in cache");
            }
            else
            {
                Console.WriteLine($"[MediaCacheService] ExtendCacheTime: Key '{key}' not ready (status: {MediaCacheStatus[key].Status})");
            }
        }
    }

    public void RemoveFromCache(string key)
    {
        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] RemoveFromCache: Key='{key}'");
        }

        if (MediaCacheStatus.ContainsKey(key))
        {
            CacheInfo cacheEntry = MediaCacheStatus[key];
            TimeSpan age = DateTime.Now.Subtract(cacheEntry.TimeOfCreation);
            MediaCacheStatus.Remove(key);
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] RemoveFromCache: Removed cache entry for key '{key}' (status: {cacheEntry.Status}, age: {age.TotalSeconds:F1}s)");
            }
            else
            {
                Console.WriteLine($"[MediaCacheService] Removed cache entry for key '{key}'");
            }
        }
        else if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] RemoveFromCache: Key '{key}' not found in cache");
        }
    }

    public bool IsInQueue(string key)
    {
        if (MediaCacheStatus.ContainsKey(key))
        {
            bool isInQueue = MediaCacheStatus[key].Status == CacheInfo.Statuses.Processing;
            if (verboseLogging)
            {
                TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[key].TimeOfCreation);
                Console.WriteLine($"[MediaCacheService] IsInQueue: Key='{key}' -> {isInQueue} (status: {MediaCacheStatus[key].Status}, age: {age.TotalSeconds:F1}s)");
            }
            return isInQueue;
        }
        else
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] IsInQueue: Key='{key}' -> false (not found)");
            }
            return false;
        }
    }

    public bool IsDone(string key)
    {
        if (MediaCacheStatus.ContainsKey(key))
        {
            bool isDone = MediaCacheStatus[key].Status == CacheInfo.Statuses.Ready;
            if (verboseLogging)
            {
                TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[key].TimeOfCreation);
                Console.WriteLine($"[MediaCacheService] IsDone: Key='{key}' -> {isDone} (status: {MediaCacheStatus[key].Status}, age: {age.TotalSeconds:F1}s)");
            }
            return isDone;
        }
        else
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] IsDone: Key='{key}' -> false (not found)");
            }
            return false;
        }
    }

    public bool IsFailed(string key)
    {
        if (MediaCacheStatus.ContainsKey(key))
        {
            bool isFailed = MediaCacheStatus[key].Status == CacheInfo.Statuses.Failed;
            if (verboseLogging)
            {
                TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[key].TimeOfCreation);
                Console.WriteLine($"[MediaCacheService] IsFailed: Key='{key}' -> {isFailed} (status: {MediaCacheStatus[key].Status}, age: {age.TotalSeconds:F1}s)");
            }
            return isFailed;
        }
        else
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] IsFailed: Key='{key}' -> false (not found)");
            }
            return false;
        }
    }

    TimeSpan IMediaCache.GetAge(string key)
    {
        if (MediaCacheStatus.ContainsKey(key))
        {
            TimeSpan age = DateTime.Now.Subtract(MediaCacheStatus[key].TimeOfCreation);
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] GetAge: Key='{key}' -> {age.TotalSeconds:F1}s (status: {MediaCacheStatus[key].Status})");
            }
            return age;
        }

        if (verboseLogging)
        {
            Console.WriteLine($"[MediaCacheService] GetAge: Key='{key}' -> 0s (not found)");
        }
        return TimeSpan.Zero;
    }

    private string ValidateAndSanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            // Normalize path separators for current platform
            string normalizedPath = Path.GetFullPath(path);

            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidPathChars();
            if (normalizedPath.IndexOfAny(invalidChars) != -1)
            {
                if (verboseLogging)
                {
                    Console.WriteLine($"[MediaCacheService] ValidateAndSanitizePath: Path contains invalid characters: '{path}'");
                }
                return null;
            }

            // Check path length limits (Windows has 260 char limit, Linux typically 4096)
            if (normalizedPath.Length > 255) // Conservative limit for cross-platform compatibility
            {
                Console.WriteLine($"[MediaCacheService] ValidateAndSanitizePath: Path too long ({normalizedPath.Length} chars): '{path}'");
                return null;
            }

            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] ValidateAndSanitizePath: '{path}' -> '{normalizedPath}'");
            }

            return normalizedPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MediaCacheService] ValidateAndSanitizePath: Error validating path '{path}': {ex.Message}");
            return null;
        }
    }

    private string SanitizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        try
        {
            // Remove or replace characters that might cause issues in cross-platform scenarios
            string sanitizedKey = key.Trim();

            // Normalize case for consistent lookup (important for Linux case sensitivity)
            sanitizedKey = sanitizedKey.ToLowerInvariant();

            // Replace problematic characters
            char[] problematicChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            foreach (char c in problematicChars)
            {
                sanitizedKey = sanitizedKey.Replace(c, '_');
            }

            if (verboseLogging && !key.Equals(sanitizedKey, StringComparison.Ordinal))
            {
                Console.WriteLine($"[MediaCacheService] SanitizeKey: '{key}' -> '{sanitizedKey}'");
            }

            return sanitizedKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MediaCacheService] SanitizeKey: Error sanitizing key '{key}': {ex.Message}");
            return key; // Return original if sanitization fails
        }
    }

    private bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            // Try to get the full path - this will throw if invalid
            string fullPath = Path.GetFullPath(path);

            // Check if path exists or if parent directory exists (for new files)
            string directory = Path.GetDirectoryName(fullPath);
            bool isValid = !string.IsNullOrEmpty(directory) && (Directory.Exists(directory) || Directory.Exists(Path.GetDirectoryName(directory)));

            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] IsValidPath: '{path}' -> {isValid}");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Console.WriteLine($"[MediaCacheService] IsValidPath: Path '{path}' is invalid: {ex.Message}");
            }
            return false;
        }
    }

}
