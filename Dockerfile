FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["SASRip.csproj", "."]
RUN dotnet restore

# Copy source code and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /

# Install dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    ffmpeg \
    yt-dlp \
    cron \
    && rm -rf /var/lib/apt/lists/*

# Create directories
RUN mkdir -p /var/logs /app/SASRip

# Copy published application
COPY --from=build /app/publish /app/SASRip

# Create diroctory for files
RUN mkdir -p /app/SASRip/wwwroot/files

# Copy startup script and update scripts
COPY ./ContainerScripts/startup.sh /startup.sh
COPY ./ContainerScripts/update_ytdlp.sh /app/update_ytdlp.sh
COPY ./ContainerScripts/update_ffmpeg.sh /app/update_ffmpeg.sh
RUN chmod +x /startup.sh /app/update_ytdlp.sh /app/update_ffmpeg.sh

# Set it to Production
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_URLS="http://0.0.0.0:54101;http://0.0.0.0:54100"
ENV ASPNETCORE_HTTPS_PORT="54101"
ENV ASPNETCORE_HTTP_PORT="54100"

# Environment variables for update schedules (cron expressions)
ENV YTDLP_UPDATE_CRON="0 2 * * *"
ENV FFMPEG_UPDATE_CRON="0 3 * * 0"

# Environment variables for configuration
#ENV KeepFiles="false"
#ENV EnableVerboseLogging="false"
#ENV YoutubeDLPath="yt-dlp"
#ENV RootOutputPath="/var/files"
#ENV LogPath="/var/logs/sasrip"
#ENV VideoArguments="--no-check-certificate --no-playlist --format bestvideo[ext=webm]+bestaudio[ext=webm]/bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=webm]/best[ext=mp4]/bestvideo+bestaudio/best"
#ENV AudioArguments="--no-check-certificate --no-playlist --extract-audio --audio-format mp3"
#ENV VideoName="%(title).100s - (%(resolution)s).%(ext)s"
#ENV AudioName="%(title).100s.%(ext)s"
#ENV CachedMediaCheckupTimeSeconds="300"
#ENV CachedMediaLifeTimeSeconds="60"

ENV ASPNETCORE_HTTPS_PORT="54101",
ENV ASPNETCORE_HTTP_PORT="54100",
EXPOSE 54101
EXPOSE 54100

ENTRYPOINT ["/startup.sh"]
