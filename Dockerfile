# ==============================================================================
# Production-Grade, Secure Multi-Stage Dockerfile for RefitDemo (.NET 10)
#
# Features:
# - Multi-stage build for optimal layer caching & minimal image footprint
# - In-container automated testing step ensuring zero broken builds
# - Non-root execution using built-in $APP_UID (UID/GID 1654)
# - Hardened runtime with DOTNET_EnableDiagnostics=0
# - Healthcheck endpoint integration (/health)
# - Production environment configuration baked into image defaults
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Test Stage (SDK)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Step 1: Copy solution and project files only (maximizes Docker layer caching)
COPY RefitDemo.sln ./
COPY RefitDemo/RefitDemo.csproj RefitDemo/
COPY RefitDemo.Tests/RefitDemo.Tests.csproj RefitDemo.Tests/

# Step 2: Restore NuGet dependencies as an isolated layer
RUN dotnet restore RefitDemo.sln

# Step 3: Copy all source files
COPY . .

# Step 4: Run unit & integration tests inside the build container
# This ensures that an image is never produced if tests fail!
RUN dotnet test RefitDemo.Tests/RefitDemo.Tests.csproj \
    -c Release \
    --no-restore \
    --verbosity normal

# Step 5: Publish production release artifacts
RUN dotnet publish RefitDemo/RefitDemo.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Final Production Runtime Stage (ASP.NET Core)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Standard OCI Container Labels
LABEL org.opencontainers.image.title="RefitDemo API" \
      org.opencontainers.image.description="Production-grade ASP.NET Core 10 Web API using Refit and HybridCache" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.vendor="RefitDemo Team" \
      org.opencontainers.image.licenses="MIT" \
      MAINTAINER="dheeraj.89@live.com"

WORKDIR /app

# Install curl for container health check probe, then clean up apt cache
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application from build stage with non-root ownership
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# ==============================================================================
# Application & Container Configuration
# ==============================================================================

# ASP.NET Core & .NET Runtime Settings
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Application-specific TMDB Configuration Defaults
# Overridable at runtime via `-e Tmdb__ApiReadAccessToken=...` or Kubernetes/Docker secrets
ENV Tmdb__BaseUrl=https://api.themoviedb.org/3 \
    Tmdb__ApiReadAccessToken=""

# Security: Switch to official non-root user ($APP_UID = 1654)
USER $APP_UID

# Expose standard non-root port
EXPOSE 8080

# Production Health Check Directive
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "RefitDemo.dll"]
