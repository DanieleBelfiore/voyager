FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Clean.Architecture/Driver/Driver.Api/Driver.Api.csproj", "Clean.Architecture/Driver/Driver.Api/"]
COPY ["Clean.Architecture/Driver/Driver.Application/Driver.Application.csproj", "Clean.Architecture/Driver/Driver.Application/"]
COPY ["Clean.Architecture/Driver/Driver.Domain/Driver.Domain.csproj", "Clean.Architecture/Driver/Driver.Domain/"]
COPY ["Clean.Architecture/Driver/Driver.Infrastructure/Driver.Infrastructure.csproj", "Clean.Architecture/Driver/Driver.Infrastructure/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Clean.Architecture/Driver/Driver.Api/Driver.Api.csproj"

COPY . .

RUN dotnet publish "Clean.Architecture/Driver/Driver.Api/Driver.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Driver.Api.dll"]
