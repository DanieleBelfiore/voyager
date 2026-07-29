FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Hexagonal.Architecture/Ride/Ride.Api/Ride.Api.csproj", "Hexagonal.Architecture/Ride/Ride.Api/"]
COPY ["Hexagonal.Architecture/Ride/Ride.Core/Ride.Core.csproj", "Hexagonal.Architecture/Ride/Ride.Core/"]
COPY ["Hexagonal.Architecture/Ride/Ride.Adapters.Secondary/Ride.Adapters.Secondary.csproj", "Hexagonal.Architecture/Ride/Ride.Adapters.Secondary/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Hexagonal.Architecture/Ride/Ride.Api/Ride.Api.csproj"

COPY . .

RUN dotnet publish "Hexagonal.Architecture/Ride/Ride.Api/Ride.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ride.Api.dll"]
