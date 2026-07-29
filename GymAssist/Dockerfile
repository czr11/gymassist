FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar y restaurar dependencias desde la subcarpeta
COPY GymAssist/*.csproj ./GymAssist/
WORKDIR /src/GymAssist
RUN dotnet restore

# Copiar todo el código
COPY . .
WORKDIR /src/GymAssist
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymAssist.dll"]