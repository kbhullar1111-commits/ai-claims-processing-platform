FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["services/claims-service/ClaimsService.API/ClaimsService.API.csproj", "services/claims-service/ClaimsService.API/"]
COPY ["services/claims-service/ClaimsService.Application/ClaimsService.Application.csproj", "services/claims-service/ClaimsService.Application/"]
COPY ["services/claims-service/ClaimsService.Domain/ClaimsService.Domain.csproj", "services/claims-service/ClaimsService.Domain/"]
COPY ["services/claims-service/ClaimsService.Infrastructure/ClaimsService.Infrastructure.csproj", "services/claims-service/ClaimsService.Infrastructure/"]
COPY ["building-blocks/contracts/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj", "building-blocks/contracts/BuildingBlocks.Contracts/"]

RUN dotnet restore "services/claims-service/ClaimsService.API/ClaimsService.API.csproj"

COPY . .
WORKDIR "/src/services/claims-service/ClaimsService.API"
RUN dotnet publish "ClaimsService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClaimsService.API.dll"]