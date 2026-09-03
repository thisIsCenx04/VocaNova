FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["src/VocaNova.API/VocaNova.API.csproj", "src/VocaNova.API/"]
RUN dotnet restore "src/VocaNova.API/VocaNova.API.csproj"

FROM restore AS publish
COPY . .
WORKDIR /src/src/VocaNova.API
RUN dotnet publish "VocaNova.API.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "VocaNova.API.dll"]
