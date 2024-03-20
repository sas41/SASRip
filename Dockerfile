#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SASRip.csproj", "."]
RUN dotnet restore "./SASRip.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./SASRip.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SASRip.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ADD https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux /bin/yt-dlp
RUN chmod +x /bin/yt-dlp
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SASRip.dll"]