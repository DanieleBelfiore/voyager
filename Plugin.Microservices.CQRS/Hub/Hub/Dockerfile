FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Hub/Hub/Hub.csproj", "Hub/Hub/"]
COPY ["Hub/Hub.API/Hub.API.csproj", "Hub/Hub.API/"]
COPY ["Hub/Hub.Core/Hub.Core.csproj", "Hub/Hub.Core/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Hub/Hub/Hub.csproj"

COPY . .

RUN dotnet publish "Hub/Hub/Hub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hub.dll"]
