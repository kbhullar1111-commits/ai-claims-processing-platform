FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Use the full .NET SDK image to restore, build, and publish the app.
WORKDIR /src

# Copy project files first so dependency restore can be cached separately.
COPY ["services/customer-service/CustomerService.API/CustomerService.API.csproj", "services/customer-service/CustomerService.API/"]
COPY ["services/customer-service/CustomerService.Application/CustomerService.Application.csproj", "services/customer-service/CustomerService.Application/"]
COPY ["services/customer-service/CustomerService.Domain/CustomerService.Domain.csproj", "services/customer-service/CustomerService.Domain/"]
COPY ["services/customer-service/CustomerService.Infrastructure/CustomerService.Infrastructure.csproj", "services/customer-service/CustomerService.Infrastructure/"]

# Download NuGet packages and resolve project-to-project dependencies.
RUN dotnet restore "services/customer-service/CustomerService.API/CustomerService.API.csproj"

# Copy the remaining allowed source files from the filtered build context.
COPY . .
WORKDIR "/src/services/customer-service/CustomerService.API"
# Build and publish the app into a deployable output folder.
RUN dotnet publish "CustomerService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "CustomerService.API.dll"]