FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Hexagonal.Architecture/Identity/Identity.Api/Identity.Api.csproj", "Hexagonal.Architecture/Identity/Identity.Api/"]
COPY ["Hexagonal.Architecture/Identity/Identity.Core/Identity.Core.csproj", "Hexagonal.Architecture/Identity/Identity.Core/"]
COPY ["Hexagonal.Architecture/Identity/Identity.Adapters.Secondary/Identity.Adapters.Secondary.csproj", "Hexagonal.Architecture/Identity/Identity.Adapters.Secondary/"]
COPY ["Commons/Voyager.Shared/Voyager.Shared.csproj", "Commons/Voyager.Shared/"]
COPY ["Commons/Voyager.Contracts/Voyager.Contracts.csproj", "Commons/Voyager.Contracts/"]

RUN dotnet restore "Hexagonal.Architecture/Identity/Identity.Api/Identity.Api.csproj"

COPY . .

RUN dotnet publish "Hexagonal.Architecture/Identity/Identity.Api/Identity.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
