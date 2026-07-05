FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["mecira-microservices.sln", "."]
COPY ["Shared/SharedModels/SharedModels.csproj", "Shared/SharedModels/"]
COPY ["Services/NotificationService/NotificationService.csproj", "Services/NotificationService/"]

# Restore dependencies
RUN dotnet restore "Services/NotificationService/NotificationService.csproj"
# Copy source code
COPY . .

# Build
WORKDIR "/src/Services/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

EXPOSE 5005
ENTRYPOINT ["dotnet", "NotificationService.dll"]
