#!/usr/bin/bash
set -e

YTDLP_PATH="/usr/local/bin/yt-dlp"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log "Updating yt-dlp..."

if curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux -o "$YTDLP_PATH.tmp"; then
    chmod +x "$YTDLP_PATH.tmp"
    mv "$YTDLP_PATH.tmp" "$YTDLP_PATH"
    VERSION=$("$YTDLP_PATH" --version 2>/dev/null || echo "unknown")
    log "yt-dlp updated successfully to version: $VERSION"
else
    log "Failed to update yt-dlp"
    rm -f "$YTDLP_PATH.tmp"
    exit 1
fi
