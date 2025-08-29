namespace SASRip.Interfaces;

public interface IConfigurationService
{
    string FileOutputPath { get; }

    // Logging Configuration
    bool EnableVerboseLogging { get; }
    string LogPath { get; }

    // Application Paths
    string YoutubeDLPath { get; }

    // Download Configuration
    string VideoArguments { get; }
    string AudioArguments { get; }

    // File Naming Templates
    string VideoName { get; }
    string AudioName { get; }

    // Cache Configuration
    int CachedMediaCheckupTimeSeconds { get; }
    int CachedMediaLifeTimeSeconds { get; }

    // File Management
    bool KeepFiles { get; }
}
