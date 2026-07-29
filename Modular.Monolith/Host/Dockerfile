FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Modular.Monolith/Host/Host.csproj", "Modular.Monolith/Host/"]
COPY ["Modular.Monolith/Modules/Identity/Identity.Module/Identity.Module.csproj", "Modular.Monolith/Modules/Identity/Identity.Module/"]
COPY ["Modular.Monolith/Modules/Driver/Driver.Module/Driver.Module.csproj", "Modular.Monolith/Modules/Driver/Driver.Module/"]
COPY ["Modular.Monolith/Modules/Ride/Ride.Module/Ride.Module.csproj", "Modular.Monolith/Modules/Ride/Ride.Module/"]
COPY ["Modular.Monolith/Modules/Hub/Hub.Module/Hub.Module.csproj", "Modular.Monolith/Modules/Hub/Hub.Module/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Modular.Monolith/Host/Host.csproj"

COPY . .

RUN dotnet publish "Modular.Monolith/Host/Host.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Host.dll"]
