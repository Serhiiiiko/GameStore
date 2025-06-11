# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG http_proxy
ARG https_proxy
WORKDIR /src

# Set proxy for NuGet if provided
RUN if [ ! -z "$http_proxy" ]; then \
    dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org --configfile NuGet.Config || true; \
    fi

# Copy csproj and restore dependencies
COPY ["GameStore/GameStore.csproj", "GameStore/"]
RUN dotnet restore "./GameStore/GameStore.csproj" --disable-parallel --force

# Copy everything else and build
COPY . .
WORKDIR "/src/GameStore"
RUN dotnet build "./GameStore.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GameStore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install dependencies for image processing (if needed)
RUN apt-get update && apt-get install -y \
    libc6-dev \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Create directory for uploaded images with proper permissions
RUN mkdir -p wwwroot/images/uploads && \
    chmod -R 755 wwwroot && \
    chmod -R 777 wwwroot/images && \
    chmod -R 777 wwwroot/images/uploads || true

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "GameStore.dll"]