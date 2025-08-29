#!/usr/bin/bash
set -e

# Default cron expressions
YTDLP_UPDATE_CRON=${YTDLP_UPDATE_CRON:-"0 2 * * *"}
FFMPEG_UPDATE_CRON=${FFMPEG_UPDATE_CRON:-"0 3 * * 0"}

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

setup_cron() {
    log "Setting up cron jobs..."

    # Clear existing cron jobs
    crontab -r 2>/dev/null || true

    # Add cron jobs if expressions are provided
    if [ -n "$YTDLP_UPDATE_CRON" ] && [ "$YTDLP_UPDATE_CRON" != "disabled" ]; then
        (crontab -l 2>/dev/null; echo "$YTDLP_UPDATE_CRON /app/update_ytdlp.sh >> /var/logs/ytdlp-update.log 2>&1") | crontab -
        log "yt-dlp update scheduled: $YTDLP_UPDATE_CRON"
    else
        log "yt-dlp automatic updates disabled"
    fi

    if [ -n "$FFMPEG_UPDATE_CRON" ] && [ "$FFMPEG_UPDATE_CRON" != "disabled" ]; then
        (crontab -l 2>/dev/null; echo "$FFMPEG_UPDATE_CRON /app/update_ffmpeg.sh >> /var/logs/ffmpeg-update.log 2>&1") | crontab -
        log "ffmpeg update scheduled: $FFMPEG_UPDATE_CRON"
    else
        log "ffmpeg automatic updates disabled"
    fi
}

main() {
    log "Starting SASRip container..."

    # Log directory already created in Dockerfile
    mkdir -p /var/log/sasrip

    log "Update schedules - yt-dlp: '$YTDLP_UPDATE_CRON', ffmpeg: '$FFMPEG_UPDATE_CRON'"

    # Setup cron jobs
    setup_cron

    # Start cron daemon
    log "Starting cron daemon..."
    cron

    # Run initial updates
    log "Running initial yt-dlp update..."
    /app/update_ytdlp.sh >> /var/logs/ytdlp-update.log 2>&1 || log "Initial yt-dlp update failed"

    log "Running initial ffmpeg update..."
    /app/update_ffmpeg.sh >> /var/logs/ffmpeg-update.log 2>&1 || log "Initial ffmpeg update failed"

    # Start the ASP.NET application
    log "Starting ASP.NET application..."
    cd /app/SASRip
    exec dotnet /app/SASRip/SASRip.dll
}

main "$@"
