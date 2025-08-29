# SASRip

## Docker Usage

### Using Docker Compose (Recommended)

```bash
docker-compose up --build
```

The application will be available at http://localhost:5000

### Using Docker directly

Build the image:
```bash
docker build -t sasrip .
```

Run the container:
```bash
docker run -p 5000:80 sasrip
```

**Optional volume mounts:**
```bash
docker run -p 5000:80 \
  -v $(pwd)/files:/app/SASRip/wwwroot/files \
  -v $(pwd)/logs:/var/logs \
  sasrip
```

### GitHub Container Registry

Automated Docker images are built and published to GitHub Container Registry on every push to main and tag release.

Pull the latest image:
```bash
docker pull ghcr.io/sas41/sasrip:latest
```

Run with the pre-built image:
```bash
docker run -p 5000:80 ghcr.io/sas41/sasrip:latest
```

**With volume mounts:**
```bash
docker run -p 5000:80 \
  -v $(pwd)/files:/app/SASRip/wwwroot/files \
  -v $(pwd)/logs:/var/logs \
  ghcr.io/sas41/sasrip:latest
```

## Configuration

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

**Examples:**

Update yt-dlp every 6 hours and ffmpeg daily at 4 AM:
```bash
docker run -e YTDLP_UPDATE_CRON="0 */6 * * *" -e FFMPEG_UPDATE_CRON="0 4 * * *" -p 5000:80 sasrip
```

Disable automatic updates and keep files:
```bash
docker run -e YTDLP_UPDATE_CRON="disabled" -e FFMPEG_UPDATE_CRON="disabled" -e KeepFiles=true -p 5000:80 sasrip
```

**Docker Compose:**
```yaml
services:
  sasrip:
    environment:
      # Update schedules
      - YTDLP_UPDATE_CRON="0 */6 * * *"
      - FFMPEG_UPDATE_CRON="0 4 * * *"
      - KeepFiles=true
      # Application settings
      - EnableVerboseLogging=true
      - VideoArguments=--no-check-certificate --no-playlist --format best[height<=720]
      - AudioArguments=--no-check-certificate --no-playlist --extract-audio --audio-format flac
      - CachedMediaLifeTimeSeconds=120
    volumes:
      - ./logs:/var/logs
      - ./files:/app/SASRip/wwwroot/files  # Optional: relocate files directory
```

### Custom Configuration File

You can also just provide your own `appsettings.json` file:

**1. Create your custom configuration:**
```bash
# Copy the sample file
curl -K https://raw.githubusercontent.com/sas41/SASRip/refs/heads/master/appsettings.sample.json -o appsettings.custom.json
# Edit with your preferred settings (nano for example, because we all know you can't quit vim)
nano appsettings.custom.json
```

**2. Mount the custom file:**
```bash
docker run -p 5000:80 -v $(pwd)/appsettings.custom.json:/app/SASRip/appsettings.json:ro sasrip
```

**3. Docker Compose with custom appsettings:**
```yaml
services:
  sasrip:
    volumes:
      - ./appsettings.custom.json:/app/SASRip/appsettings.json:ro
```

**Sample appsettings.json features:**
- Detailed comments and examples
- Multiple quality options for video/audio
- Various file naming templates
- Alternative cache timings
- Complete logging configuration

> **Note:** When using a custom appsettings.json file, environment variables will still override the file settings.

**Advanced Docker Example:**
```bash
docker run -p 5000:80 \
  -e EnableVerboseLogging=true \
  -e VideoArguments="--no-check-certificate --format best[height<=1080]" \
  -e AudioArguments="--no-check-certificate --extract-audio --audio-format flac" \
  -e CachedMediaLifeTimeSeconds=10800 \
  -e Logging__LogLevel__Default=Debug \
  -v $(pwd)/files:/app/SASRip/wwwroot/files \
  sasrip
```

### Update Schedule

- **yt-dlp**: Downloads latest version on container start, then updates based on configured cron schedule
- **ffmpeg**: Updates through Debian package manager based on configured cron schedule
- **Logs**: Update activities are logged to `/var/logs/ytdlp-update.log` and `/var/logs/ffmpeg-update.log`
- **Cron format**: `minute hour day month day-of-week` (e.g., "0 2 * * *" = daily at 2:00 AM)

## Dependencies

The Docker container automatically downloads and manages:
- yt-dlp (latest version from GitHub releases)
- ffmpeg (from Debian repositories)

You can technically mount a custom version of ffmpeg and disable updates, but I don't see why. If you really have a particular need, I assume you know how to do that.
If using a fork of yt-dlp, update appsettings.json to reflect that.