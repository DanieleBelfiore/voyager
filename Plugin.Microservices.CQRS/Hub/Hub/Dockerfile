FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Hub/Hub/Hub.csproj", "Hub/Hub/"]
COPY ["Hub/Hub.API/Hub.API.csproj", "Hub/Hub.API/"]
COPY ["Hub/Hub.Core/Hub.Core.csproj", "Hub/Hub.Core/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Hub/Hub/Hub.csproj"

COPY . .

RUN dotnet publish "Hub/Hub/Hub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hub.dll"]
