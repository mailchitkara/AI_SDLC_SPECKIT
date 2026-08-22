# syntax=docker/dockerfile:1
#
# Build context is backend/ (see render.yaml's dockerContext) so AgentGuard.Core and
# AgentGuard.Api can be copied as siblings. Test projects are excluded via .dockerignore.

## Build stage — compiles and publishes AgentGuard.Api (+ its AgentGuard.Core dependency) ##
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached unless a .csproj actually changes.
COPY AgentGuard.Core/AgentGuard.Core.csproj AgentGuard.Core/
COPY AgentGuard.Api/AgentGuard.Api.csproj AgentGuard.Api/
RUN dotnet restore AgentGuard.Api/AgentGuard.Api.csproj

COPY AgentGuard.Core/ AgentGuard.Core/
COPY AgentGuard.Api/ AgentGuard.Api/
RUN dotnet publish AgentGuard.Api/AgentGuard.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

## Runtime stage — ASP.NET Core runtime only, no SDK, no source, non-root user ##
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --home /app --gecos "" appuser \
    && chown -R appuser:appuser /app
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

# Documentation only — the actual bound port is read from PORT at runtime (Program.cs).
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgentGuard.Api.dll"]
