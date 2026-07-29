FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Vertical.Slice.Architecture/Hub/Hub.Api/Hub.Api.csproj", "Vertical.Slice.Architecture/Hub/Hub.Api/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Vertical.Slice.Architecture/Hub/Hub.Api/Hub.Api.csproj"

COPY . .

RUN dotnet publish "Vertical.Slice.Architecture/Hub/Hub.Api/Hub.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hub.Api.dll"]
