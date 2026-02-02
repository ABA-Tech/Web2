
# Multi-stage Dockerfile placed at repository root
# This allows running `docker build .` from the repo root in production

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy only the project file first to leverage Docker layer caching
COPY Web/Web.csproj ./Web/
RUN dotnet restore ./Web/Web.csproj

# Copy entire repository
COPY . ./

# Publish the Web project
RUN dotnet publish ./Web/Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# If an SQLite file was created or committed in Web/, copy it into the publish output
RUN if [ -f "Web/app.db" ]; then cp "Web/app.db" /app/publish/; fi

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Web.dll"]
