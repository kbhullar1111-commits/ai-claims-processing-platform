FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Use the full .NET SDK image to restore, build, and publish the app.
WORKDIR /src

# Copy project files first so dependency restore can be cached separately.
COPY ["services/fraud-service/FraudService.API/FraudService.API.csproj", "services/fraud-service/FraudService.API/"]
COPY ["building-blocks/contracts/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj", "building-blocks/contracts/BuildingBlocks.Contracts/"]

# Download NuGet packages and resolve project-to-project dependencies.
RUN dotnet restore "services/fraud-service/FraudService.API/FraudService.API.csproj"

# Copy the remaining allowed source files from the filtered build context.
COPY . .
WORKDIR "/src/services/fraud-service/FraudService.API"
# Build and publish the app into a deployable output folder.
RUN dotnet publish "FraudService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Use the smaller runtime image for the container that will actually run.
WORKDIR /app
# Copy only the published output from the build stage into the runtime stage.
COPY --from=build /app/publish .

EXPOSE 8080
# Start the API when the container launches.
ENTRYPOINT ["dotnet", "FraudService.API.dll"]