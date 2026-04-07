# Build .NET API stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution files
COPY ["BeautyCommerce.sln", "./"]
COPY ["src/Domain/BeautyCommerce.Domain.csproj", "src/Domain/"]
COPY ["src/Application/BeautyCommerce.Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/BeautyCommerce.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Api/BeautyCommerce.Api.csproj", "src/Api/"]

# Restore dependencies
RUN dotnet restore "src/Api/BeautyCommerce.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Api"
RUN dotnet build "BeautyCommerce.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BeautyCommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install PostgreSQL EF Core migrations bundle tool
RUN apt-get update && apt-get install -y wget && \
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-ef || true

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "/app/publish/BeautyCommerce.Api.dll"]
