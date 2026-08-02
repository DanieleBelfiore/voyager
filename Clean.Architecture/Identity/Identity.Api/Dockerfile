FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Clean.Architecture/Identity/Identity.Api/Identity.Api.csproj", "Clean.Architecture/Identity/Identity.Api/"]
COPY ["Clean.Architecture/Identity/Identity.Application/Identity.Application.csproj", "Clean.Architecture/Identity/Identity.Application/"]
COPY ["Clean.Architecture/Identity/Identity.Domain/Identity.Domain.csproj", "Clean.Architecture/Identity/Identity.Domain/"]
COPY ["Clean.Architecture/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "Clean.Architecture/Identity/Identity.Infrastructure/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Clean.Architecture/Identity/Identity.Api/Identity.Api.csproj"

COPY . .

RUN dotnet publish "Clean.Architecture/Identity/Identity.Api/Identity.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
