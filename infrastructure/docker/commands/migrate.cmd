@echo off
cd /d "%~dp0..\.."

echo Applying claims database migrations...
set ASPNETCORE_ENVIRONMENT=Development
set ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=claimsdb;Username=postgres;Password=postgres
dotnet ef database update ^
  --project services/claims-service/ClaimsService.Infrastructure/ClaimsService.Infrastructure.csproj ^
  --startup-project services/claims-service/ClaimsService.API/ClaimsService.API.csproj ^
  --context ClaimsDbContext
if %errorlevel% neq 0 (
    echo ERROR: Claims migration failed.
    exit /b %errorlevel%
)

echo Applying notification database migrations...
set ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=notificationdb;Username=postgres;Password=postgres
dotnet ef database update ^
  --project services/notification-service/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj ^
  --startup-project services/notification-service/NotificationService.API/NotificationService.API.csproj ^
  --context NotificationDbContext
if %errorlevel% neq 0 (
    echo ERROR: Notification migration failed.
    exit /b %errorlevel%
)

echo All migrations applied successfully.