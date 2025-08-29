# SASRip

## Docker Usage

### Using Docker Compose (Recommended)

Assuming you have docker installed...

**docker-compose.yaml**
```yaml
services:
  sasrip:
    image: ghcr.io/sas41/sasrip:latest
    restart: unless-stopped
    environment:
      # Update schedules
      - YTDLP_UPDATE_CRON=0 2 * * *
      - FFMPEG_UPDATE_CRON=0 3 * * 6
      # Logging Settings:
      #- EnableVerboseLogging=true
      #- LogPath=/var/logs/sasrip# Not recommended to change this, it's mostly for debugging
      # File Name and Quality Settings:
      #- VideoArguments=--no-check-certificate --no-playlist --format bestvideo[ext=webm]+bestaudio[ext=webm]/bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=webm]/best[ext=mp4]/bestvideo+bestaudio/best
      #- AudioArguments=--no-check-certificate --no-playlist --extract-audio --audio-format mp3
      #- VideoName=%(title).100s - (%(resolution)s).%(ext)s
      #- AudioName=%(title).100s.%(ext)s
      # Cache Settings:
      #- KeepFiles=false
      #- CachedMediaCheckupTimeSeconds=300
      #- CachedMediaLifeTimeSeconds=3600
    ports:
     # HTTP Port (it's recommended to run this behind a reverse proxy)
     - "8080:54100"
     # Optional: HTTPS Port
     #- "8081:54101"
    #volumes:
      # Optional: To keep your logs
      #- ./logs:/var/logs
      # Optional: relocate files directory, use if you wish to keep files or a different drive
      #- ./files:/app/SASRip/wwwroot/files
      # Optional: Full Custom Config
      #- ./appsettings.custom.json:/app/SASRip/appsettings.json:ro
```

**Command to run:**
```bash
docker compose up -d
```

## Configuration

You can see more examples in the `appsettings.sample.json` file.

### Binary Update Schedules

You can configure when yt-dlp and ffmpeg are updated using cron expressions:

- `YTDLP_UPDATE_CRON` - Cron expression for yt-dlp updates (default: "0 2 * * *" - daily at 2 AM)
- `FFMPEG_UPDATE_CRON` - Cron expression for ffmpeg updates (default: "0 3 * * 0" - weekly on Sunday at 3 AM)

### Application Settings

Most appsettings.json values can be overridden using environment variables:

**Core Settings:**
- `EnableVerboseLogging` - This will enable logging on every API hit and add more details to Docker/service logs (default: "false")
- `YoutubeDLPath` - Path to yt-dlp binary (default: "yt-dlp")
- `LogPath` - Application log directory (default: "/var/logs/sasrip")

**Download Arguments:**
- `VideoArguments` - yt-dlp arguments for video downloads
- `AudioArguments` - yt-dlp arguments for audio downloads

**File Naming:**
- `VideoName` - Template for video file names (default: "%(title).100s - (%(resolution)s).%(ext)s")
- `AudioName` - Template for audio file names (default: "%(title).100s.%(ext)s")

**Cache Settings:**
- `KeepFiles` - Whether to keep downloaded files after processing, disables caching (default: false)
- `CachedMediaCheckupTimeSeconds` - Cache check interval in seconds (default: "300")
- `CachedMediaLifeTimeSeconds` - Cache lifetime in seconds (default: "3600")

### Custom Configuration File and Disabling Environment Variables

You can also just provide your own `appsettings.json` file where you can even disable Environment Variables:

**1. Create your custom configuration:**
```bash
# Copy the sample file
curl -K https://raw.githubusercontent.com/sas41/SASRip/refs/heads/master/appsettings.sample.json -o appsettings.custom.json
# Edit with your preferred settings (nano for example, because we all know you can't quit vim)
nano appsettings.custom.json
# Don't forget to enable the volume mount in your docker-compose.yaml file!
```
> **Note:** When using a custom appsettings.json file, environment variables will still override the file settings unless they have been explicitly disabled.

## Dependencies

The Docker container automatically downloads and manages:
- yt-dlp (latest version from GitHub releases)
- ffmpeg (from Debian repositories)

You can technically mount a custom version of ffmpeg and disable updates, but I don't see why. If you really have a particular need, I assume you know how to do that.
If using a fork of yt-dlp, update appsettings.json to reflect that.
