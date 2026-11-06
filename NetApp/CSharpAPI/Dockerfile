# =============================================================================
#  Multi-stage Docker build for SharpAPI
#  Stage 1 (build): full SDK image, ~800 MB, discarded after the build
#  Stage 2 (final): runtime-only "chiseled" image, ~110 MB, which is what ships
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Copy ONLY the project/props files and restore. Docker caches this layer, so
#    NuGet packages are re-downloaded only when a .csproj changes, not on every code edit.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/SharpAPI.Domain/SharpAPI.Domain.csproj                 src/SharpAPI.Domain/
COPY src/SharpAPI.Application/SharpAPI.Application.csproj       src/SharpAPI.Application/
COPY src/SharpAPI.Infrastructure/SharpAPI.Infrastructure.csproj src/SharpAPI.Infrastructure/
COPY src/SharpAPI.Api/SharpAPI.Api.csproj                       src/SharpAPI.Api/
RUN dotnet restore src/SharpAPI.Api/SharpAPI.Api.csproj

# 2) Copy the source and publish a Release build.
COPY src/ src/
RUN dotnet publish src/SharpAPI.Api/SharpAPI.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# -----------------------------------------------------------------------------
# "Chiseled" = Ubuntu with only the files .NET needs: no shell, no package manager,
# and a NON-ROOT user by default. Far smaller attack surface, far fewer CVEs to patch.
# "-extra" adds ICU + tzdata (needed for culture-aware formatting and SqlClient).
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "SharpAPI.Api.dll"]
