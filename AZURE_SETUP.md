# Guia de Configuração - Azure Service Bus & Log Analytics

Este guia explica como provisionar e configurar os recursos Azure necessários para o PaymentsAPI.

---

## 📋 Índice

- [Opção 1: Azure Portal (Interface)](#opção-1-azure-portal-interface)
- [Opção 2: Azure CLI (Automático)](#opção-2-azure-cli-automático)
- [Configurar Permissões Mínimas](#configurar-permissões-mínimas)
- [Testar a Configuração](#testar-a-configuração)
- [Troubleshooting](#troubleshooting)

---

## Opção 1: Azure Portal (Interface)

### 1. Criar Resource Group

1. Acesse o [Azure Portal](https://portal.azure.com)
2. Pesquise por **"Resource groups"** e clique em **+ Create**
3. Preencha:
   - **Subscription**: Selecione sua assinatura
   - **Resource group**: `rg-cloudgames-prod`
   - **Region**: `Brazil South` (ou região desejada)
4. Clique em **Review + Create** → **Create**

---

### 2. Criar Log Analytics Workspace

1. Pesquise por **"Log Analytics workspaces"** e clique em **+ Create**
2. Preencha:
   - **Subscription**: Sua assinatura
   - **Resource group**: `rg-cloudgames-prod`
   - **Name**: `law-cloudgames-prod`
   - **Region**: `Brazil South` (mesma do Resource Group)
3. Clique em **Review + Create** → **Create**
4. Aguarde a criação (~1-2 minutos)

---

### 3. Criar Application Insights

1. Pesquise por **"Application Insights"** e clique em **+ Create**
2. Preencha:
   - **Subscription**: Sua assinatura
   - **Resource group**: `rg-cloudgames-prod`
   - **Name**: `appi-cloudgames-prod`
   - **Region**: `Brazil South`
   - **Resource Mode**: **Workspace-based**
   - **Log Analytics Workspace**: Selecione `law-cloudgames-prod`
3. Clique em **Review + Create** → **Create**
4. Após criação, acesse o recurso e vá em **Overview**
5. **Copie a Connection String** (formato: `InstrumentationKey=...;IngestionEndpoint=...`)
   - ⚠️ NÃO copiar apenas a Instrumentation Key, precisa da Connection String completa

---

### 4. Criar Service Bus Namespace

1. Pesquise por **"Service Bus"** e clique em **+ Create**
2. Preencha:
   - **Subscription**: Sua assinatura
   - **Resource group**: `rg-cloudgames-prod`
   - **Namespace name**: `cloudgames-prod` (deve ser único globalmente)
   - **Location**: `Brazil South`
   - **Pricing tier**: **Standard** (mínimo para tópicos)
3. Clique em **Review + Create** → **Create**
4. Aguarde a criação (~2-3 minutos)

---

### 5. Criar Tópicos no Service Bus

#### 5.1 Criar tópico `order-placed`

1. Acesse o Service Bus Namespace criado (`cloudgames-prod`)
2. No menu lateral, clique em **Topics**
3. Clique em **+ Topic**
4. Preencha:
   - **Name**: `order-placed`
   - Mantenha as configurações padrão
5. Clique em **Create**

#### 5.2 Criar assinatura `payments-api` no tópico `order-placed`

1. Clique no tópico **order-placed**
2. Clique em **+ Subscription**
3. Preencha:
   - **Name**: `payments-api`
   - **Max delivery count**: `10`
   - **Lock duration**: `30 seconds`
   - Mantenha outras configurações padrão
4. Clique em **Create**

#### 5.3 Criar tópico `payment-processed`

1. Volte para **Topics**
2. Clique em **+ Topic**
3. Preencha:
   - **Name**: `payment-processed`
   - Mantenha as configurações padrão
4. Clique em **Create**

---

### 6. Obter Connection String do Service Bus

> ⚠️ **IMPORTANTE**: Copie a connection string do **NAMESPACE**, não de um tópico individual!
>
> - ✅ Correto: `Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...`
> - ❌ Errado: `Endpoint=sb://...;EntityPath=order-placed;SharedAccessKeyName=...`
>
> Se tiver `EntityPath=` na string, você está copiando do tópico. Volte ao namespace!

#### Opção A: Usando RootManageSharedAccessKey (Desenvolvimento)

1. No **Service Bus Namespace** (não no tópico!), vá em **Settings** → **Shared access policies**
2. Clique em **RootManageSharedAccessKey**
3. **Copie a Primary Connection String**
   - Formato correto: `Endpoint=sb://cloudgames-prod.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...`
   - **NÃO deve conter** `EntityPath=`

⚠️ **Atenção**: Esta key tem permissões de gerenciamento. Para produção, veja a seção [Configurar Permissões Mínimas](#configurar-permissões-mínimas).

#### Opção B: Criar Policy com Permissões Mínimas (Produção - Recomendado)

1. No Service Bus Namespace, vá em **Settings** → **Shared access policies**
2. Clique em **+ Add**
3. Preencha:
   - **Policy name**: `payments-api-policy`
   - **Manage**: ❌ Desmarcar
   - **Send**: ✅ Marcar
   - **Listen**: ✅ Marcar
4. Clique em **Create**
5. Clique na policy recém-criada e **copie a Primary Connection String**

---

## Opção 2: Azure CLI (Automático)

Para provisionar tudo via linha de comando:

```bash
# Variáveis
RESOURCE_GROUP="rg-cloudgames-prod"
LOCATION="brazilsouth"
LAW_NAME="law-cloudgames-prod"
APPI_NAME="appi-cloudgames-prod"
SB_NAMESPACE="cloudgames-prod"
TOPIC_ORDER="order-placed"
TOPIC_PAYMENT="payment-processed"
SUBSCRIPTION_NAME="payments-api"

# 1. Login (se necessário)
az login

# 2. Criar Resource Group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# 3. Criar Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LAW_NAME \
  --location $LOCATION

# 4. Obter workspace ID para Application Insights
WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LAW_NAME \
  --query id -o tsv)

# 5. Criar Application Insights
az monitor app-insights component create \
  --app $APPI_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --workspace $WORKSPACE_ID

# 6. Obter Application Insights Connection String
APPI_CONN_STRING=$(az monitor app-insights component show \
  --app $APPI_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

echo "Application Insights Connection String:"
echo $APPI_CONN_STRING

# 7. Criar Service Bus Namespace (Standard tier)
az servicebus namespace create \
  --name $SB_NAMESPACE \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard

# 8. Criar tópico order-placed
az servicebus topic create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name $TOPIC_ORDER

# 9. Criar assinatura payments-api no tópico order-placed
az servicebus topic subscription create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --topic-name $TOPIC_ORDER \
  --name $SUBSCRIPTION_NAME \
  --max-delivery-count 10

# 10. Criar tópico payment-processed
az servicebus topic create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name $TOPIC_PAYMENT

# 11. Criar policy com permissões mínimas (Send + Listen)
az servicebus namespace authorization-rule create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name payments-api-policy \
  --rights Send Listen

# 12. Obter Connection String da policy
SB_CONN_STRING=$(az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name payments-api-policy \
  --query primaryConnectionString -o tsv)

echo ""
echo "Service Bus Connection String (Send + Listen only):"
echo $SB_CONN_STRING

# 13. Resumo
echo ""
echo "========================================="
echo "✅ Recursos criados com sucesso!"
echo "========================================="
echo ""
echo "📋 Configurações para copiar:"
echo ""
echo "APPLICATIONINSIGHTS_CONNECTION_STRING="
echo "$APPI_CONN_STRING"
echo ""
echo "ServiceBus__ConnectionString="
echo "$SB_CONN_STRING"
echo ""
echo "ServiceBus__OrderPlacedTopicName=order-placed"
echo "ServiceBus__OrderPlacedSubscriptionName=payments-api"
echo "ServiceBus__PaymentProcessedTopicName=payment-processed"
```

---

## Configurar Permissões Mínimas

Para seguir o princípio de **menor privilégio**, a aplicação deve usar apenas permissões `Send` e `Listen`:

### Por que não usar `Manage`?

- 🔒 **Segurança**: Evita que a aplicação crie/delete tópicos e assinaturas
- 🛡️ **Conformidade**: Alinhado com boas práticas de security
- 🏗️ **IaC**: Infraestrutura deve ser gerenciada via IaC, não pela aplicação

### Como configurar:

1. No Service Bus Namespace, vá em **Shared access policies**
2. Crie uma nova policy:
   - **Nome**: `payments-api-policy`
   - **Permissões**: ✅ Send, ✅ Listen
3. Use a connection string desta policy
4. **IMPORTANTE**: O código já está configurado com `ConfigureConsumeTopology = false` para não tentar criar entidades automaticamente

---

## Testar a Configuração

### 1. Configurar Variáveis Localmente

```bash
# Linux/Mac
export APPLICATIONINSIGHTS_CONNECTION_STRING="<sua-connection-string>"
export ServiceBus__ConnectionString="<sua-connection-string>"
export ServiceBus__OrderPlacedTopicName="order-placed"
export ServiceBus__OrderPlacedSubscriptionName="payments-api"
export ServiceBus__PaymentProcessedTopicName="payment-processed"

# Windows (PowerShell)
$env:APPLICATIONINSIGHTS_CONNECTION_STRING="<sua-connection-string>"
$env:ServiceBus__ConnectionString="<sua-connection-string>"
$env:ServiceBus__OrderPlacedTopicName="order-placed"
$env:ServiceBus__OrderPlacedSubscriptionName="payments-api"
$env:ServiceBus__PaymentProcessedTopicName="payment-processed"
```

### 2. Rodar a Aplicação

```bash
cd src/Payments.Api
dotnet run
```

### 3. Verificar Logs

Se conectou com sucesso, você verá:

```
info: MassTransit[0]
      Bus started: sb://cloudgames-prod.servicebus.windows.net/
```

### 4. Testar Processamento de Mensagens

#### 4.1 Enviar mensagem de teste via Azure Portal

1. No Service Bus, acesse o tópico **order-placed**
2. Clique em **Service Bus Explorer** (Preview)
3. Clique em **Send messages**
4. Cole o JSON:

```json
{
  "orderId": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "user-123",
  "gameId": "game-456",
  "emailUser": "teste@example.com",
  "price": 99.9
}
```

5. Clique em **Send**

#### 4.2 Verificar no Log Analytics

1. Acesse o Application Insights no Portal
2. Vá em **Logs**
3. Execute a query:

```kql
traces
| where timestamp > ago(30m)
| where message contains "OrderId"
| order by timestamp desc
```

Você deve ver logs do processamento do evento.

#### 4.3 Verificar mensagem publicada

1. No Service Bus, acesse o tópico **payment-processed**
2. Use o **Service Bus Explorer** para ver se a mensagem foi publicada

---

## Troubleshooting

### Erro: `Manage,EntityRead claims required`

**Causa**: Connection string não tem permissões `Send` e `Listen` OU o código está tentando criar entidades.

**Solução**:

1. Verifique se o código tem `ConfigureConsumeTopology = false` (já configurado nesta versão)
2. Use uma policy com `Send` e `Listen` (não `Manage`)
3. Garanta que tópicos e assinaturas já existem no Service Bus

### Erro: `The messaging entity could not be found`

**Causa**: Tópico ou assinatura não existe.

**Solução**:

1. Verifique se o tópico `order-placed` existe
2. Verifique se a assinatura `payments-api` existe dentro do tópico `order-placed`
3. Confirme os nomes nas variáveis de ambiente

### Erro: `ArgumentException: EntityPath in connection string`

**Causa**: Connection string copiada de um tópico específico, não do namespace.

**Solução**:

1. Vá para o **Service Bus Namespace** (nível acima dos tópicos)
2. **Settings** → **Shared access policies**
3. Copie a connection string **do namespace**
4. Verifique que **NÃO tem** `EntityPath=` na string

### Erro: `401 Unauthorized`

**Causa**: Connection string inválida ou expirada.

**Solução**:

1. Regenere a connection string no Portal
2. Certifique-se de copiar a string completa (inclui `Endpoint=`, `SharedAccessKeyName=`, `SharedAccessKey=`)
3. Verifique que copiou do **namespace**, não de um tópico individual

### Logs não aparecem no Log Analytics

**Causa**: Delay de ingestão (normal) ou connection string incorreta.

**Solução**:

1. Aguarde 2-5 minutos (lag de ingestão)
2. Verifique se `APPLICATIONINSIGHTS_CONNECTION_STRING` está definida
3. Confirme que é uma connection string, não uma instrumentation key

---

## 📚 Referências

- [Azure Service Bus Documentation](https://docs.microsoft.com/azure/service-bus-messaging/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [MassTransit Azure Service Bus](https://masstransit.io/documentation/transports/azure-service-bus)
- [Log Analytics Query Language (KQL)](https://docs.microsoft.com/azure/data-explorer/kusto/query/)

---

## 🔐 Segurança em Produção

### Checklist de Boas Práticas

- ✅ Usar Managed Identity em vez de connection strings (quando possível)
- ✅ Armazenar connection strings em Azure Key Vault
- ✅ Usar permissões mínimas (Send/Listen, não Manage)
- ✅ Rotacionar chaves periodicamente
- ✅ Habilitar diagnóstico e alertas no Service Bus
- ✅ Configurar retention policies no Log Analytics
- ✅ Usar Application Insights workspace-based (não classic)
- ✅ Implementar alertas para exceções e latência

### Exemplo: Usar Azure Key Vault

```bash
# Criar Key Vault
az keyvault create \
  --name kv-cloudgames-prod \
  --resource-group rg-cloudgames-prod \
  --location brazilsouth

# Armazenar secrets
az keyvault secret set \
  --vault-name kv-cloudgames-prod \
  --name ServiceBusConnectionString \
  --value "<connection-string>"

az keyvault secret set \
  --vault-name kv-cloudgames-prod \
  --name AppInsightsConnectionString \
  --value "<connection-string>"
```

No Kubernetes, use o [Azure Key Vault Provider for Secrets Store CSI Driver](https://azure.github.io/secrets-store-csi-driver-provider-azure/).

---

**🎉 Configuração concluída!** A aplicação agora está integrada com Azure Service Bus e Log Analytics.
