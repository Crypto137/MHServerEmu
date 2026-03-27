# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MHServerEmu.sln ./
COPY src/ src/
COPY dep/ dep/

RUN dotnet publish MHServerEmu.sln \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Patch Config.ini for Docker: HttpListener needs "+" (wildcard) instead of
# "localhost" on Linux, otherwise it crashes with "The request is not supported"
RUN sed -i 's/^Address=localhost$/Address=+/' /app/Config.ini

# Create persistent data directories
RUN mkdir -p /app/Data /app/Data/Leaderboards

# Game server (TCP) and web frontend (HTTP)
EXPOSE 4306 8080

VOLUME ["/app/Data"]

ENTRYPOINT ["./MHServerEmu"]