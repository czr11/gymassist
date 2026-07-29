FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN if [ -f "./GymAssist/GymAssist.csproj" ]; then \
        PROJECT_PATH="./GymAssist/GymAssist.csproj"; \
    elif [ -f "./GymAssist.csproj" ]; then \
        PROJECT_PATH="./GymAssist.csproj"; \
    else \
        echo "Project file not found"; exit 1; \
    fi && \
    dotnet restore "$PROJECT_PATH" && \
    dotnet publish "$PROJECT_PATH" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymAssist.dll"]