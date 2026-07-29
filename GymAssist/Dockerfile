FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "./GymAssist/GymAssist.csproj"
RUN dotnet publish "./GymAssist/GymAssist.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymAssist.dll"]