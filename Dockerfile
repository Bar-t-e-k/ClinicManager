# 1. Etap budowania
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiowanie pliku projektu i przywracanie zależności (NuGet)
COPY ["src/ClinicManager.Web/ClinicManager.Web.csproj", "src/ClinicManager.Web/"]
RUN dotnet restore "src/ClinicManager.Web/ClinicManager.Web.csproj"

# Kopiowanie reszty kodu i budowanie aplikacji
COPY . .
WORKDIR "/src/src/ClinicManager.Web"

# 2. Etap publikacji
RUN dotnet publish "ClinicManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etap uruchomieniowy
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER $APP_UID

# Port, na którym nasłuchuje domyślnie .NET 8/9/10
EXPOSE 8080

# Kopiowanie skompilowanych plików z etapu publikacji
COPY --from=build /app/publish .

# Uruchamianie aplikacji
ENTRYPOINT ["dotnet", "ClinicManager.Web.dll"]
