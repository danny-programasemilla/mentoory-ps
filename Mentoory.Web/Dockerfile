# Multi-stage build for the Mentoory web app (feature 022-vm-deployment).
#
# Build context is the REPOSITORY ROOT (see deploy/vm/docker-compose.yml: context ../..).
# Mentoory.Web transitively references ~19 module projects + ServiceDefaults; publishing
# the Web csproj restores and compiles exactly that closure. `dotnet publish` (not build)
# is required so MapStaticAssets()'s static-web-assets manifest + wwwroot are emitted.
#
# Central Package Management (Directory.Packages.props) + global.json (SDK 10.0.0,
# allowPrerelease) must be present in the context. No nuget.config exists -> default
# nuget.org feed, which hosts the .NET 10 prerelease packages.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide build inputs first (better restore-layer caching).
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./

# Copy the full source tree (the .dockerignore at the repo root trims bin/obj/.git/etc).
COPY . .

# Restore + publish the web project and its transitive project graph.
RUN dotnet publish Mentoory.Web/Mentoory.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Listen on plain HTTP; Caddy terminates TLS and reverse-proxies here.
ENV ASPNETCORE_ENVIRONMENT=Production \
    HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# NOTE: ASP.NET DataProtection keys are persisted via a named volume mounted by
# docker-compose.yml. The aspnet:10.0 image runs as root by default, so the keyring
# path is /root/.aspnet/DataProtection-Keys. If a non-root image variant is adopted,
# update the volume target in docker-compose.yml to /home/app/.aspnet/DataProtection-Keys.
ENTRYPOINT ["dotnet", "Mentoory.Web.dll"]
