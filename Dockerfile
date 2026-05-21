# 1. Etap budowania
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiowanie pliku projektu i przywracanie zależności (NuGet)
COPY ["src/ClinicManager.Web/ClinicManager.Web.csproj", "src/ClinicManager.Web/"]
RUN dotnet restore "src/ClinicManager.Web/ClinicManager.Web.csproj"

# Kopiowanie reszty kodu i budowanie aplikacji
COPY . .
WORKDIR "/src/src/ClinicManager.Web"
RUN dotnet build "ClinicManager.Web.csproj" -c Release -o /app/build

# 2. Etap publikacji
FROM build AS publish
RUN dotnet publish "ClinicManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etap uruchomieniowy
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Port, na którym nasłuchuje domyślnie .NET 8/9/10
EXPOSE 8080

# Kopiowanie skompilowanych plików z etapu publikacji
COPY --from=publish /app/publish .

# Uruchamianie aplikacji
ENTRYPOINT ["dotnet", "ClinicManager.Web.dll"]