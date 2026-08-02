FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Vertical.Slice.Architecture/Ride/Ride.Api/Ride.Api.csproj", "Vertical.Slice.Architecture/Ride/Ride.Api/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Vertical.Slice.Architecture/Ride/Ride.Api/Ride.Api.csproj"

COPY . .

RUN dotnet publish "Vertical.Slice.Architecture/Ride/Ride.Api/Ride.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ride.Api.dll"]
