using Microsoft.Extensions.Configuration;
using SASRip.Interfaces;
using System;

namespace SASRip.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    // Cached configuration values
    private readonly string _fileOutputPath;
    private readonly bool _useEnvironmentVariables = true;
    private readonly bool _enableVerboseLogging;
    private readonly string _logPath;
    private readonly string _youtubeDLPath;
    private readonly string _videoArguments;
    private readonly string _audioArguments;
    private readonly string _videoName;
    private readonly string _audioName;
    private readonly int _cachedMediaCheckupTimeSeconds;
    private readonly int _cachedMediaLifeTimeSeconds;
    private readonly bool _keepFiles;

    // Public properties exposing cached values
    public string FileOutputPath => _fileOutputPath;
    public bool EnableVerboseLogging => _enableVerboseLogging;
    public string LogPath => _logPath;
    public string YoutubeDLPath => _youtubeDLPath;
    public string VideoArguments => _videoArguments;
    public string AudioArguments => _audioArguments;
    public string VideoName => _videoName;
    public string AudioName => _audioName;
    public int CachedMediaCheckupTimeSeconds => _cachedMediaCheckupTimeSeconds;
    public int CachedMediaLifeTimeSeconds => _cachedMediaLifeTimeSeconds;
    public bool KeepFiles => _keepFiles;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        try
        {
            // This should not change in order to let ASP.Net serve files without additional configuration
            _fileOutputPath = "./wwwroot/files";

            // First, determine if we should use environment variables (always from appsettings.json)
            _useEnvironmentVariables = GetConfigValueAsBool("UseEnvironmentVariables", true);

            // Load and cache all configuration values at startup
            _keepFiles = GetConfigValueAsBool("KeepFiles", false);
            _enableVerboseLogging = GetConfigValueAsBool("EnableVerboseLogging", false);
            _logPath = GetRequiredConfigValue("LogPath");
            _youtubeDLPath = GetRequiredConfigValue("YoutubeDLPath");
            _videoArguments = GetRequiredConfigValue("VideoArguments");
            _audioArguments = GetRequiredConfigValue("AudioArguments");
            _videoName = GetRequiredConfigValue("VideoName");
            _audioName = GetRequiredConfigValue("AudioName");
            _cachedMediaCheckupTimeSeconds = GetConfigValueAsInt("CachedMediaCheckupTimeSeconds", 300);
            _cachedMediaLifeTimeSeconds = GetConfigValueAsInt("CachedMediaLifeTimeSeconds", 3600);

            // Validate cached timings
            if (_cachedMediaCheckupTimeSeconds <= 0)
            {
                Console.WriteLine("CachedMediaCheckupTimeSeconds should be greater than 0, using default: 300");
            }

            if (_cachedMediaLifeTimeSeconds <= 0)
            {
                Console.WriteLine("CachedMediaLifeTimeSeconds should be greater than 0, using default: 3600");
            }

            // Log configuration if verbose logging is enabled
            if (_enableVerboseLogging)
            {
                PrintConfiguration();
            }

            Console.WriteLine("Configuration loaded and cached successfully");
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to load required configuration. Application will terminate.");
            Environment.Exit(1);
        }
    }

    // Helper method to get required configuration values (terminates app if missing)
    private string GetRequiredConfigValue(string key)
    {
        // First try environment variables (if enabled), then appsettings.json
        string value = _useEnvironmentVariables ? Environment.GetEnvironmentVariable(key) : null;
        value ??= _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine("Required configuration value '{Key}' is missing from both environment variables and appsettings.json", key);
            throw new InvalidOperationException($"Required configuration value '{key}' is missing");
        }

        return value;
    }

    private bool GetConfigValueAsBool(string key, bool defaultValue)
    {
        string value = _useEnvironmentVariables ? Environment.GetEnvironmentVariable(key) : null;

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    private int GetConfigValueAsInt(string key, int defaultValue = 0)
    {
        string value = _useEnvironmentVariables ? Environment.GetEnvironmentVariable(key) : null;
        value ??= _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private void PrintConfiguration()
    {
        Console.WriteLine("=== SASRip Configuration ===");
        Console.WriteLine($"Use Environment Variables: {_useEnvironmentVariables}");
        Console.WriteLine($"Verbose Logging: {_enableVerboseLogging}");
        Console.WriteLine($"YouTube-DL Path: {_youtubeDLPath}");
        Console.WriteLine($"Log Path: {_logPath}");
        Console.WriteLine($"Keep Files: {_keepFiles}");
        Console.WriteLine($"Cache Checkup Time: {_cachedMediaCheckupTimeSeconds}s");
        Console.WriteLine($"Cache Lifetime: {_cachedMediaLifeTimeSeconds}s");
        Console.WriteLine($"Video Name Template: {VideoName}");
        Console.WriteLine($"Audio Name Template: {AudioName}");
        Console.WriteLine($"Video Arguments: {_videoArguments}");
        Console.WriteLine($"Audio Arguments: {_audioArguments}");
        Console.WriteLine("===============================");
    }
}
