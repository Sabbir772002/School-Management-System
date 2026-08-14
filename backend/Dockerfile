# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AssignmentBackend.csproj", "./"]
RUN dotnet restore "AssignmentBackend.csproj"

COPY . .
RUN dotnet build "AssignmentBackend.csproj" -c Release -o /app/build

# ── Publish stage ─────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "AssignmentBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for the docker-compose healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=publish /app/publish .

# Persistent volume mount point for file uploads
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "AssignmentBackend.dll"]
