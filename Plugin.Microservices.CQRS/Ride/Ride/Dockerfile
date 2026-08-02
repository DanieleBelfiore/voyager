FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Ride/Ride/Ride.csproj", "Ride/Ride/"]
COPY ["Ride/Ride.API/Ride.API.csproj", "Ride/Ride.API/"]
COPY ["Ride/Ride.Core/Ride.Core.csproj", "Ride/Ride.Core/"]
COPY ["Ride/Ride.Handlers/Ride.Handlers.csproj", "Ride/Ride.Handlers/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Ride/Ride/Ride.csproj"

COPY . .

RUN dotnet publish "Ride/Ride/Ride.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ride.dll"]
