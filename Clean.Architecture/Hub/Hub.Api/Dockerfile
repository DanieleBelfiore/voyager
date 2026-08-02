FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Clean.Architecture/Hub/Hub.Api/Hub.Api.csproj", "Clean.Architecture/Hub/Hub.Api/"]
COPY ["Clean.Architecture/Hub/Hub.Application/Hub.Application.csproj", "Clean.Architecture/Hub/Hub.Application/"]
COPY ["Clean.Architecture/Hub/Hub.Infrastructure/Hub.Infrastructure.csproj", "Clean.Architecture/Hub/Hub.Infrastructure/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Clean.Architecture/Hub/Hub.Api/Hub.Api.csproj"

COPY . .

RUN dotnet publish "Clean.Architecture/Hub/Hub.Api/Hub.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hub.Api.dll"]
