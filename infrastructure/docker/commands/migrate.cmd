@echo off
cd /d "%~dp0.."
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.observability.yml -f docker-compose.migrations.yml up --build claims-migrator notification-migrator 	
docker compose -f docker-compose.yml -f docker-compose.observability.yml -f docker-compose.migrations.yml rm -f claims-migrator notification-migrator