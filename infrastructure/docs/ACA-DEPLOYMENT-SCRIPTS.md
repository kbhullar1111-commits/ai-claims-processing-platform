# ACA Deployment Scripts

This file contains Azure Container Apps deployment commands for the services in this repository, plus a serverless Function App deployment example for the document processor.

## 1) Shared variables

```cmd
set RG=rg-ai-claims-dev
set ACR_NAME=aiclaimsacr
set ACR_SERVER=aiclaimsacr.azurecr.io
set ACA_ENV=aiclaims-aca-env
set TAG=v1
set CLAIMS_DB_CONNECTION_STRING=Host=...;Database=claimsdb;Username=...;Password=...
set NOTIFICATION_DB_CONNECTION_STRING=Host=...;Database=notificationdb;Username=...;Password=...
set CUSTOMER_DB_CONNECTION_STRING=Host=...;Database=customerdb;Username=...;Password=...
```

## 2) Optional login to ACR

```cmd
az acr login --name %ACR_NAME%
```

## 3) Generic pattern for each service

```cmd
set SERVICE_NAME=document-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/%SERVICE_NAME%/DocumentService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

## 4) Service-specific deployment commands

### Document service

```cmd
set SERVICE_NAME=document-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/document-service/DocumentService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Claims service

```cmd
set SERVICE_NAME=claims-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/claims-service/ClaimsService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Notification service

```cmd
set SERVICE_NAME=notification-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/notification-service/NotificationService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Fraud service

```cmd
set SERVICE_NAME=fraud-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/fraud-service/FraudService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Payment service

```cmd
set SERVICE_NAME=payment-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/payment-service/PaymentService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Gateway service

```cmd
set SERVICE_NAME=gateway-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/gateway-service/GatewayService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

### Customer service

```cmd
set SERVICE_NAME=customer-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/customer-service/CustomerService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

## 5) One-time Container App create commands

```cmd
set SERVICE_NAME=document-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=claims-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=notification-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=fraud-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=payment-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=gateway-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

```cmd
set SERVICE_NAME=customer-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
```

## 6) Serverless / Function App deployment example

The repository also contains a serverless document processor under [serverless/document-processor-function](../../serverless/document-processor-function).

```cmd
set FUNCTION_APP=document-processor-function
set FUNCTION_RUNTIME=dotnet
set FUNCTION_PLAN=Consumption

az functionapp create --name %FUNCTION_APP% --resource-group %RG% --consumption-plan-location eastus --runtime %FUNCTION_RUNTIME% --functions-version 4 --os-type Linux --deployment-container-image-name %ACR_SERVER%/%FUNCTION_APP%:%TAG%
```

If you already have the Function App created and want to update it with a container image:

```cmd
az functionapp config container set --name %FUNCTION_APP% --resource-group %RG% --docker-custom-image-name %ACR_SERVER%/%FUNCTION_APP%:%TAG%
```

## 6) Useful operational commands

```cmd
az containerapp revision list --name document-service --resource-group %RG% -o table
az containerapp logs show --name document-service --resource-group %RG% --tail 200
az containerapp restart --name document-service --resource-group %RG%
```
