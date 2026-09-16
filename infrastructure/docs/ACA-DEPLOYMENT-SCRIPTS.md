# ACA Deployment Scripts

This file contains Azure Container Apps deployment commands for the services in this repository, plus a serverless Function App deployment example for the document processor.

## 1) Shared variables

```cmd
set RG=rg-name
set ACR_NAME=acrname
set ACR_SERVER=acrservername
set ACA_ENV=aca-env
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

### Policy service

```cmd
set SERVICE_NAME=policy-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

docker build -f services/policy-service/PolicyService.API/Dockerfile -t %SERVICE_NAME% .
docker tag %SERVICE_NAME% %IMAGE%
docker push %IMAGE%
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

If the Container App was created but its revision shows `Activation failed` with
`unable to pull image using Managed Identity`, configure the registry pull identity
before updating the image:

```cmd
set SERVICE_NAME=policy-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%

az containerapp identity assign --name %SERVICE_NAME% --resource-group %RG% --system-assigned
for /f "delims=" %i in ('az containerapp show --name %SERVICE_NAME% --resource-group %RG% --query identity.principalId -o tsv') do set ACA_PRINCIPAL_ID=%i
for /f "delims=" %i in ('az acr show --name %ACR_NAME% --query id -o tsv') do set ACR_ID=%i
az role assignment create --assignee-object-id %ACA_PRINCIPAL_ID% --assignee-principal-type ServicePrincipal --role AcrPull --scope %ACR_ID%
az containerapp registry set --name %SERVICE_NAME% --resource-group %RG% --server %ACR_SERVER% --identity system
az containerapp update --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE%
```

For a new Container App, include `--system-assigned --registry-identity system` in
the create command, then grant the identity the `AcrPull` role and run the update
commands above. The app resource can exist even while its first revision is unable
to activate, so creation success alone does not confirm that the image was pulled.

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

```cmd
set SERVICE_NAME=policy-service
set IMAGE=%ACR_SERVER%/%SERVICE_NAME%:%TAG%
az containerapp create --name %SERVICE_NAME% --resource-group %RG% --image %IMAGE% --environment %ACA_ENV% --ingress external --registry-server %ACR_SERVER% --registry-identity system --system-assigned --target-port 8080 --env-vars ASPNETCORE_URLS=http://+:8080
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
