FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GymAssist.csproj", "./"]
RUN dotnet restore "./GymAssist.csproj"

COPY . .
RUN dotnet build "GymAssist.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GymAssist.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "GymAssist.dll"]