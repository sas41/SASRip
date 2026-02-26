#!/usr/bin/bash
set -e

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log "Updating ffmpeg..."

if apt-get update && apt-get install -y --only-upgrade ffmpeg; then
    VERSION=$(ffmpeg -version 2>/dev/null | head -n1 | cut -d' ' -f3 || echo "unknown")
    log "ffmpeg updated successfully to version: $VERSION"
else
    log "Failed to update ffmpeg or no updates available"
fi
