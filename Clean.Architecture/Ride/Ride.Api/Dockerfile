FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Clean.Architecture/Ride/Ride.Api/Ride.Api.csproj", "Clean.Architecture/Ride/Ride.Api/"]
COPY ["Clean.Architecture/Ride/Ride.Application/Ride.Application.csproj", "Clean.Architecture/Ride/Ride.Application/"]
COPY ["Clean.Architecture/Ride/Ride.Domain/Ride.Domain.csproj", "Clean.Architecture/Ride/Ride.Domain/"]
COPY ["Clean.Architecture/Ride/Ride.Infrastructure/Ride.Infrastructure.csproj", "Clean.Architecture/Ride/Ride.Infrastructure/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Clean.Architecture/Ride/Ride.Api/Ride.Api.csproj"

COPY . .

RUN dotnet publish "Clean.Architecture/Ride/Ride.Api/Ride.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ride.Api.dll"]
