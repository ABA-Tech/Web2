# ====== BUILD ======
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY Web.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ====== RUNTIME ======
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copier l'app
COPY --from=build /app/publish .

# Copier explicitement la base SQLite
COPY app.db ./app.db
# ou wedding.db si c'est celle-là
# COPY wedding.db ./wedding.db

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Web.dll"]
