FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["Identity/Identity/Identity.csproj", "Identity/Identity/"]
COPY ["Identity/Identity.API/Identity.API.csproj", "Identity/Identity.API/"]
COPY ["Identity/Identity.Core/Identity.Core.csproj", "Identity/Identity.Core/"]
COPY ["Identity/Identity.Handlers/Identity.Handlers.csproj", "Identity/Identity.Handlers/"]
COPY ["Common/Common.Core/Common.Core.csproj", "Common/Common.Core/"]

RUN dotnet restore "Identity/Identity/Identity.csproj"

COPY . .

RUN dotnet publish "Identity/Identity/Identity.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Identity.dll"]
