FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Ride/Ride/Ride.csproj", "Ride/Ride/"]
COPY ["Ride/Ride.API/Ride.API.csproj", "Ride/Ride.API/"]
COPY ["Ride/Ride.Core/Ride.Core.csproj", "Ride/Ride.Core/"]
COPY ["Ride/Ride.Handlers/Ride.Handlers.csproj", "Ride/Ride.Handlers/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Ride/Ride/Ride.csproj"

COPY . .

RUN dotnet publish "Ride/Ride/Ride.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ride.dll"]
