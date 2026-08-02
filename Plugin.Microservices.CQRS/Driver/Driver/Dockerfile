FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Driver/Driver/Driver.csproj", "Driver/Driver/"]
COPY ["Driver/Driver.API/Driver.API.csproj", "Driver/Driver.API/"]
COPY ["Driver/Driver.Core/Driver.Core.csproj", "Driver/Driver.Core/"]
COPY ["Driver/Driver.Handlers/Driver.Handlers.csproj", "Driver/Driver.Handlers/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Driver/Driver/Driver.csproj"

COPY . .

RUN dotnet publish "Driver/Driver/Driver.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# curl is what the docker-compose healthcheck below (and any orchestrator) polls
# /health with — the aspnet base image doesn't ship it.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Driver.dll"]
