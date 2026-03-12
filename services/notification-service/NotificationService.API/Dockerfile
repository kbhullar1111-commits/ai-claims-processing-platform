FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["services/notification-service/NotificationService.API/NotificationService.API.csproj", "services/notification-service/NotificationService.API/"]
COPY ["services/notification-service/NotificationService.Application/NotificationService.Application.csproj", "services/notification-service/NotificationService.Application/"]
COPY ["services/notification-service/NotificationService.Domain/NotificationService.Domain.csproj", "services/notification-service/NotificationService.Domain/"]
COPY ["services/notification-service/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "services/notification-service/NotificationService.Infrastructure/"]
COPY ["building-blocks/contracts/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj", "building-blocks/contracts/BuildingBlocks.Contracts/"]

RUN dotnet restore "services/notification-service/NotificationService.API/NotificationService.API.csproj"

COPY . .
WORKDIR "/src/services/notification-service/NotificationService.API"
RUN dotnet publish "NotificationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]