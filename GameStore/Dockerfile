# See https://aka.ms/customizecontainer to learn how to customize your debug container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GameStore/GameStore.csproj", "GameStore/"]
RUN dotnet restore "./GameStore/GameStore.csproj"
COPY . .
WORKDIR "/src/GameStore"
RUN dotnet build "./GameStore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GameStore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Switch to root to copy files and set permissions
USER root

# Copy published files
COPY --from=publish /app/publish .

# Create directory for uploaded images if it doesn't exist
RUN mkdir -p wwwroot/images/uploads && \
    chmod -R 755 wwwroot && \
    chmod -R 777 wwwroot/images && \
    chmod -R 777 wwwroot/images/uploads || true

# Switch back to app user for security
USER app

ENTRYPOINT ["dotnet", "GameStore.dll"]