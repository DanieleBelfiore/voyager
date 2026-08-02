FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Identity/Identity/Identity.csproj", "Identity/Identity/"]
COPY ["Identity/Identity.API/Identity.API.csproj", "Identity/Identity.API/"]
COPY ["Identity/Identity.Core/Identity.Core.csproj", "Identity/Identity.Core/"]
COPY ["Identity/Identity.Handlers/Identity.Handlers.csproj", "Identity/Identity.Handlers/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Identity/Identity/Identity.csproj"

COPY . .

RUN dotnet publish "Identity/Identity/Identity.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Identity.dll"]
