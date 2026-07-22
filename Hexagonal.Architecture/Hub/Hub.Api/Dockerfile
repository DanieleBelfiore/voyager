FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Hexagonal.Architecture/Hub/Hub.Api/Hub.Api.csproj", "Hexagonal.Architecture/Hub/Hub.Api/"]
COPY ["Hexagonal.Architecture/Hub/Hub.Core/Hub.Core.csproj", "Hexagonal.Architecture/Hub/Hub.Core/"]
COPY ["Hexagonal.Architecture/Hub/Hub.Adapters.Secondary/Hub.Adapters.Secondary.csproj", "Hexagonal.Architecture/Hub/Hub.Adapters.Secondary/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Hexagonal.Architecture/Hub/Hub.Api/Hub.Api.csproj"

COPY . .

RUN dotnet publish "Hexagonal.Architecture/Hub/Hub.Api/Hub.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hub.Api.dll"]
