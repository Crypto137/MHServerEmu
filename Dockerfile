# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy dep DLLs and project files first to cache the restore layer
COPY dep/ dep/
COPY src/Gazillion/Gazillion.csproj src/Gazillion/
COPY src/MHServerEmu/MHServerEmu.csproj src/MHServerEmu/
COPY src/MHServerEmu.Core/MHServerEmu.Core.csproj src/MHServerEmu.Core/
COPY src/MHServerEmu.DatabaseAccess/MHServerEmu.DatabaseAccess.csproj src/MHServerEmu.DatabaseAccess/
COPY src/MHServerEmu.Frontend/MHServerEmu.Frontend.csproj src/MHServerEmu.Frontend/
COPY src/MHServerEmu.Games/MHServerEmu.Games.csproj src/MHServerEmu.Games/
COPY src/MHServerEmu.Grouping/MHServerEmu.Grouping.csproj src/MHServerEmu.Grouping/
COPY src/MHServerEmu.Leaderboards/MHServerEmu.Leaderboards.csproj src/MHServerEmu.Leaderboards/
COPY src/MHServerEmu.PlayerManagement/MHServerEmu.PlayerManagement.csproj src/MHServerEmu.PlayerManagement/
COPY src/MHServerEmu.WebFrontend/MHServerEmu.WebFrontend.csproj src/MHServerEmu.WebFrontend/

RUN dotnet restore src/MHServerEmu/MHServerEmu.csproj --runtime linux-x64

# Copy source and publish as self-contained linux-x64
COPY src/ src/

RUN dotnet publish src/MHServerEmu/MHServerEmu.csproj \
    --no-restore \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output /build/publish

# Runtime stage — runtime-deps is sufficient for self-contained binaries
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /build

COPY --from=build /build/publish /server

# Data directory holds the SQLite database, Config.ini overrides, and game assets.
# Mount a host directory here to persist player data across container restarts.
VOLUME ["/server"]

# 4306 — game client frontend
# 8080  — web frontend / dashboard
EXPOSE 4306
EXPOSE 8080

ENTRYPOINT ["./MHServerEmu"]
