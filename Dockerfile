# BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY Web/Web.csproj ./Web/
RUN dotnet restore ./Web/Web.csproj

COPY . ./
RUN dotnet publish ./Web/Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# ⚠️ NE METTEZ RIEN ICI POUR BREVO_API_KEY

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Web.dll"]