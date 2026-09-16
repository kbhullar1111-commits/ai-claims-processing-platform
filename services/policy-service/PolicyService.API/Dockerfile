FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Use the full .NET SDK image to restore, build, and publish the app.
WORKDIR /src

# Copy project files first so dependency restore can be cached separately.
COPY ["services/policy-service/PolicyService.API/PolicyService.API.csproj", "services/policy-service/PolicyService.API/"]
COPY ["services/policy-service/PolicyService.Application/PolicyService.Application.csproj", "services/policy-service/PolicyService.Application/"]
COPY ["services/policy-service/PolicyService.Domain/PolicyService.Domain.csproj", "services/policy-service/PolicyService.Domain/"]
COPY ["services/policy-service/PolicyService.Infrastructure/PolicyService.Infrastructure.csproj", "services/policy-service/PolicyService.Infrastructure/"]

# Download NuGet packages and resolve project-to-project dependencies.
RUN dotnet restore "services/policy-service/PolicyService.API/PolicyService.API.csproj"

# Copy the remaining allowed source files from the filtered build context.
COPY . .
WORKDIR "/src/services/policy-service/PolicyService.API"
# Build and publish the app into a deployable output folder.
RUN dotnet publish "PolicyService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Use the smaller runtime image for the container that will actually run.
WORKDIR /app
RUN apt-get update \
	&& apt-get install -y --no-install-recommends libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*
# Copy only the published output from the build stage into the runtime stage.
COPY --from=build /app/publish .

EXPOSE 8080
# Start the API when the container launches.
ENTRYPOINT ["dotnet", "PolicyService.API.dll"]