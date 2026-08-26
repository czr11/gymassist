FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install LibMan CLI for restoring client-side libraries
RUN dotnet tool install -g microsoft.web.librarymanager.cli

COPY GymAssist/GymAssist.csproj ./GymAssist/
COPY GymAssist/libman.json ./GymAssist/
RUN dotnet restore "./GymAssist/GymAssist.csproj"

# Restore client-side libraries
RUN cd ./GymAssist && libman restore

COPY GymAssist/. ./GymAssist/
RUN dotnet publish "./GymAssist/GymAssist.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymAssist.dll"]