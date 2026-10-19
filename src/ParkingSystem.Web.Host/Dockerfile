# Stage 1: Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy NuGet.Config for package restore
COPY ["NuGet.Config", "./"]

# Copy project files to leverage layer caching during dotnet restore
COPY ["ParkingSystem.Web.Host/ParkingSystem.Web.Host.csproj", "ParkingSystem.Web.Host/"]
COPY ["ParkingSystem.Web.Core/ParkingSystem.Web.Core.csproj", "ParkingSystem.Web.Core/"]
COPY ["ParkingSystem.Application/ParkingSystem.Application.csproj", "ParkingSystem.Application/"]
COPY ["ParkingSystem.Core/ParkingSystem.Core.csproj", "ParkingSystem.Core/"]
COPY ["ParkingSystem.EntityFrameworkCore/ParkingSystem.EntityFrameworkCore.csproj", "ParkingSystem.EntityFrameworkCore/"]

# Restore dependencies
RUN dotnet restore "ParkingSystem.Web.Host/ParkingSystem.Web.Host.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/ParkingSystem.Web.Host"
RUN dotnet publish "ParkingSystem.Web.Host.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Explicitly ensure App_Data/Logs directory exists and configure write permissions for logs
RUN mkdir -p /app/App_Data/Logs \
    && chmod -R 777 /app/App_Data

# Set container user explicitly (non-root $APP_UID)
USER $APP_UID

EXPOSE 44337
EXPOSE 80

ENTRYPOINT ["dotnet", "ParkingSystem.Web.Host.dll"]
