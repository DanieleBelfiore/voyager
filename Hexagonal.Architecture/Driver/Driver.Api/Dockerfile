FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Hexagonal.Architecture/Driver/Driver.Api/Driver.Api.csproj", "Hexagonal.Architecture/Driver/Driver.Api/"]
COPY ["Hexagonal.Architecture/Driver/Driver.Core/Driver.Core.csproj", "Hexagonal.Architecture/Driver/Driver.Core/"]
COPY ["Hexagonal.Architecture/Driver/Driver.Adapters.Secondary/Driver.Adapters.Secondary.csproj", "Hexagonal.Architecture/Driver/Driver.Adapters.Secondary/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Hexagonal.Architecture/Driver/Driver.Api/Driver.Api.csproj"

COPY . .

RUN dotnet publish "Hexagonal.Architecture/Driver/Driver.Api/Driver.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Driver.Api.dll"]
