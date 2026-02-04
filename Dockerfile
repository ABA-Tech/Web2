# Multi-stage Dockerfile for ASP.NET Core 7.0

# ----------------------
# BUILD STAGE
# ----------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy project file first for caching
COPY Web/Web.csproj ./Web/
RUN dotnet restore ./Web/Web.csproj

# Copy everything
COPY . ./

# Publish the Web project
RUN dotnet publish ./Web/Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Copy SQLite database if present
RUN if [ -f "Web/app.db" ]; then cp "Web/app.db" /app/publish/; fi

# ----------------------
# RUNTIME STAGE
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Install CA certificates for SSL/TLS
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

# Expose port for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Pass Brevo API key from Render environment
# (you can also set this in Render UI under Environment Variables)
ENV BREVO_API_KEY=${BREVO_API_KEY}

# Copy published app
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "Web.dll"]
