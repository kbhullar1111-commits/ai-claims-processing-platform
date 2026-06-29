@echo off
cd /d "%~dp0.."

echo === documentdb ===
(
echo select count(*) as documents_count from "Documents";
echo select count(*) as outbox_message_count from "OutboxMessage";
echo select count(*) as outbox_state_count from "OutboxState";
echo select count(*) as inbox_state_count from "InboxState";
) | docker compose exec -T postgres psql -U postgres -d documentdb
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo === claimsdb ===
(
echo select count(*) as claims_count from claims;
echo select count(*) as saga_count from "ClaimProcessingSagaState";
echo select count(*) as outbox_message_count from "OutboxMessage";
echo select count(*) as outbox_state_count from "OutboxState";
echo select count(*) as inbox_state_count from "InboxState";
) | docker compose exec -T postgres psql -U postgres -d claimsdb
if %errorlevel% neq 0 exit /b %errorlevel%