# 🔧 Troubleshooting - Erros Comuns

## ❌ Erro: `EntityPath in connection string`

### Mensagem completa:

```
System.ArgumentException: The queue or topic name provided does not match
the EntityPath in the connection string passed to the ServiceBusClient constructor.
```

### Causa:

Você copiou a connection string de um **tópico individual** (ex: `order-placed`) em vez do **namespace**.

### Como identificar:

Sua connection string contém `EntityPath=` no final:

```
❌ ERRADO:
Endpoint=sb://cloudgames-prod.servicebus.windows.net/;EntityPath=order-placed;SharedAccessKeyName=...
```

### Solução:

#### Passo 1: Ir para o Namespace (não o tópico)

```
Portal Azure → Service Bus → Seu namespace (ex: cloudgames-prod)
                               ^^^^^^^^^^^^^^
                               Este é o namespace!
```

**NÃO clique no tópico!** Fique no nível do namespace.

#### Passo 2: Acessar Shared Access Policies

```
Namespace (cloudgames-prod)
├── Overview
├── Activity log
├── ...
└── Settings
    ├── Networking
    ├── Shared access policies  ← Clique aqui
    └── ...
```

#### Passo 3: Copiar Connection String

```
Shared access policies
├── RootManageSharedAccessKey  ← Clique aqui
└── (ou sua policy custom)
```

Copie a **Primary Connection String**.

### Connection String Correta:

```
✅ CORRETO:
Endpoint=sb://cloudgames-prod.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...
```

**Características da string correta:**

- ✅ Começa com `Endpoint=sb://`
- ✅ Tem `SharedAccessKeyName=`
- ✅ Tem `SharedAccessKey=`
- ✅ **NÃO tem** `EntityPath=`

---

## ❌ Erro: `InvalidSignature: The token has an invalid signature`

### Mensagem completa:

```
InvalidSignature: The token has an invalid signature.
Status: 401 (Unauthorized)
```

### Causa:

A connection string está **corrompida**, **incompleta** ou **mal formatada**.

### Causas comuns:

#### 1. Quebra de linha no meio da string

**❌ Errado (docker-compose.yaml):**

```yaml
ServiceBus__ConnectionString:
  "Endpoint=sb://cloudgames-prod.servicebus.windows.net/;
  SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
```

**✅ Correto:**

```yaml
ServiceBus__ConnectionString: "Endpoint=sb://cloudgames-prod.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
```

#### 2. Espaços extras ao copiar/colar

**❌ Errado:**

```bash
export ServiceBus__ConnectionString=" Endpoint=sb://..."
#                                   ^ espaço extra
```

**✅ Correto:**

```bash
export ServiceBus__ConnectionString="Endpoint=sb://..."
```

#### 3. String incompleta (cortada)

A `SharedAccessKey` deve estar completa. Compare tamanho:

```
✅ Correto (key completa - geralmente ~44 caracteres):
SharedAccessKey=abc123XYZ789def456GHI012jkl345MNO678pqr901==

❌ Errado (key cortada):
SharedAccessKey=abc123XYZ789def456GHI012jkl
```

#### 4. Caracteres especiais mal escapados

Se sua key tem `&`, `=` ou outros caracteres especiais, verifique:

**Docker Compose:**

```yaml
# Sempre entre aspas duplas
ServiceBus__ConnectionString: "Endpoint=sb://...;SharedAccessKey=ABC+123/xyz=="
```

**Bash/Terminal:**

```bash
# Use aspas simples para evitar interpretação
export ServiceBus__ConnectionString='Endpoint=sb://...;SharedAccessKey=ABC+123/xyz=='
```

**Kubernetes (secret.yaml):**

```yaml
stringData:
  ServiceBus__ConnectionString: "Endpoint=sb://...;SharedAccessKey=ABC+123/xyz=="
```

### Solução: Copiar novamente do Portal

1. Portal Azure → Service Bus Namespace → Shared access policies
2. Clique em **RootManageSharedAccessKey**
3. Clique no ícone de **copiar** ao lado de "Primary Connection String" (não digite manualmente!)
4. Cole em um editor de texto primeiro para verificar se está íntegra
5. Verifique que termina com `SharedAccessKey=...` (pode terminar com `=` ou `==`)

### Validar formato manualmente:

```bash
# Salve sua connection string em uma variável
CONN_STRING="<cole-aqui>"

# Verificar se tem todos os componentes
echo $CONN_STRING | grep -o "Endpoint=sb://" && echo "✅ Endpoint OK"
echo $CONN_STRING | grep -o "SharedAccessKeyName=" && echo "✅ KeyName OK"
echo $CONN_STRING | grep -o "SharedAccessKey=" && echo "✅ Key OK"

# Verificar se NÃO tem EntityPath
echo $CONN_STRING | grep -o "EntityPath=" && echo "❌ Remover EntityPath!" || echo "✅ Sem EntityPath"
```

### Se o erro persistir:

1. **Regenere a key** no Portal:
   - Settings → Shared access policies → RootManageSharedAccessKey
   - Clique em **Regenerate Primary Key**
   - Copie a **nova** Primary Connection String

2. **Teste com curl** (para validar a key):

   ```bash
   # Extrair componentes
   NAMESPACE="cloudgames-prod.servicebus.windows.net"

   # Tentar listar tópicos (requer Manage permission)
   curl -X GET "https://$NAMESPACE/\$Resources/topics?api-version=2017-04" \
     -H "Authorization: Bearer <token>"
   ```

---

## ❌ Erro: `401 Unauthorized` ou `Manage,EntityRead claims required`

### Mensagem completa:

```
Status: 401 (Unauthorized)
Manage,EntityRead claims required for this operation.
```

### Causa 1: Permission Insuficiente

#### Solução:

Use uma policy com **Send** + **Listen**:

1. Namespace → Settings → Shared access policies
2. Clique em **+ Add**
3. Nome: `payments-api-policy`
4. Permissões:
   - ❌ Manage (desmarcar)
   - ✅ Send (marcar)
   - ✅ Listen (marcar)
5. Copie a connection string desta policy

### Causa 2: Código tentando criar entidades

#### Verificar:

O código em `MassTransitConfig.cs` deve ter:

```csharp
e.ConfigureConsumeTopology = false;
```

Esta linha já está no código migrado. Se você editou manualmente, verifique.

---

## ❌ Erro: `The messaging entity could not be found`

### Mensagem completa:

```
MessagingEntityNotFoundException: The messaging entity 'order-placed' could not be found.
```

### Causa:

Tópico ou assinatura não existe no Service Bus.

### Solução:

#### Verificar Tópicos:

```bash
# Azure CLI
az servicebus topic list \
  --resource-group rg-cloudgames-prod \
  --namespace-name cloudgames-prod \
  --query "[].name" -o table
```

Deve retornar:

```
order-placed
payment-processed
```

#### Verificar Assinaturas:

```bash
az servicebus topic subscription list \
  --resource-group rg-cloudgames-prod \
  --namespace-name cloudgames-prod \
  --topic-name order-placed \
  --query "[].name" -o table
```

Deve retornar:

```
payments-api
```

#### Criar se não existir:

```bash
# Criar tópico
az servicebus topic create \
  --resource-group rg-cloudgames-prod \
  --namespace-name cloudgames-prod \
  --name order-placed

# Criar assinatura
az servicebus topic subscription create \
  --resource-group rg-cloudgames-prod \
  --namespace-name cloudgames-prod \
  --topic-name order-placed \
  --name payments-api
```

---

## ❌ Logs não aparecem no Log Analytics

### Causa 1: Delay de Ingestão (Normal)

**Solução**: Aguarde 2-5 minutos. É normal ter delay.

### Causa 2: Connection String Incorreta

#### Verificar:

```bash
# Deve mostrar a connection string
echo $APPLICATIONINSIGHTS_CONNECTION_STRING
```

Formato correto:

```
InstrumentationKey=xxx-xxx-xxx;IngestionEndpoint=https://brazilsouth-1.in.applicationinsights.azure.com/;LiveEndpoint=https://...
```

#### Se estiver vazia ou incorreta:

1. Portal Azure → Application Insights → seu recurso
2. **Overview** → copie **Connection String** (não Instrumentation Key)
3. Exporte novamente:
   ```bash
   export APPLICATIONINSIGHTS_CONNECTION_STRING="<cole-aqui>"
   ```

### Causa 3: Workspace não configurado

O Application Insights **deve ser workspace-based**.

#### Verificar:

Portal → Application Insights → Properties

```
Workspace: law-cloudgames-prod  ← Deve estar preenchido
```

Se estiver vazio (classic mode), recrie o Application Insights como workspace-based.

---

## ❌ Build Error: `No overload for SubscriptionEndpoint`

### Mensagem:

```
error CS1501: No overload for method 'SubscriptionEndpoint' takes 3 arguments
```

### Causa:

Versão incorreta do MassTransit ou sintaxe errada.

### Solução:

#### Verificar versão:

```bash
cd src/Payments.Api
dotnet list package | grep MassTransit
```

Deve mostrar:

```
MassTransit                           8.5.7
MassTransit.Azure.ServiceBus.Core     8.5.7
```

#### Sintaxe correta:

```csharp
cfg.SubscriptionEndpoint<OrderPlacedEvent>(settings.OrderPlacedSubscriptionName, e =>
{
    e.ConfigureConsumeTopology = false;
    e.ConfigureConsumer<OrderPlacedConsumer>(context);
});
```

**Não usar** 3 argumentos (subscriptionName, topicName, configurator).

---

## ❌ Container não inicia (Docker)

### Verificar logs:

```bash
docker-compose logs payments-api
```

### Causa 1: Variáveis não configuradas

#### Solução:

Edite `docker-compose.yaml` e preencha:

```yaml
ServiceBus__ConnectionString: "Endpoint=sb://..."
APPLICATIONINSIGHTS_CONNECTION_STRING: "InstrumentationKey=..."
```

**Não deixe vazio** (`""`).

### Causa 2: Connection string com aspas

#### ❌ Errado:

```yaml
ServiceBus__ConnectionString: ""Endpoint=sb://...""
```

#### ✅ Correto:

```yaml
ServiceBus__ConnectionString: "Endpoint=sb://..."
```

---

## ❌ Kubernetes Pod em CrashLoopBackOff

### Verificar logs:

```bash
kubectl logs deployment/payments-api
```

### Causa: Secrets não configurados

#### Verificar:

```bash
kubectl get secret payments-api-secret -o yaml
```

Deve ter:

```yaml
stringData:
  ServiceBus__ConnectionString: "Endpoint=sb://..."
  APPLICATIONINSIGHTS_CONNECTION_STRING: "InstrumentationKey=..."
```

#### Solução:

1. Edite `k8s/secret.yaml`
2. Preencha os valores em `stringData`
3. Aplique:
   ```bash
   kubectl apply -f k8s/secret.yaml
   kubectl rollout restart deployment/payments-api
   ```

---

## 🧪 Testar Connection Strings

### Teste 1: Service Bus

```bash
# Instalar ferramenta
dotnet tool install -g ServiceBusExplorer

# Testar conexão (substitua com sua connection string)
sbexplorer test "Endpoint=sb://cloudgames-prod.servicebus.windows.net/;SharedAccessKeyName=..."
```

### Teste 2: Application Insights

```bash
# Enviar telemetria de teste
curl -X POST https://brazilsouth-1.in.applicationinsights.azure.com/v2.1/track \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Microsoft.ApplicationInsights.Event",
    "time": "'$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)'",
    "iKey": "SEU-INSTRUMENTATION-KEY",
    "data": {
      "baseType": "EventData",
      "baseData": {
        "name": "TestEvent"
      }
    }
  }'
```

Aguarde 2-5 minutos e verifique no Log Analytics:

```kql
customEvents
| where name == "TestEvent"
| order by timestamp desc
```

---

## 📞 Onde Obter Ajuda

1. **Documentação oficial:**
   - [Azure Service Bus](https://docs.microsoft.com/azure/service-bus-messaging/)
   - [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
   - [MassTransit Azure Service Bus](https://masstransit.io/documentation/transports/azure-service-bus)

2. **Verificar status do Azure:**
   - https://status.azure.com/

3. **Issues do projeto:**
   - Crie uma issue no repositório com logs completos

---

## ✅ Checklist de Validação

Antes de abrir um ticket, verifique:

- [ ] Connection string do **namespace** (sem `EntityPath=`)
- [ ] Tópicos `order-placed` e `payment-processed` existem
- [ ] Assinatura `payments-api` existe em `order-placed`
- [ ] Policy tem permissões Send + Listen
- [ ] Application Insights é workspace-based
- [ ] Variáveis de ambiente configuradas
- [ ] Build do projeto passa sem erros
- [ ] Aguardou 2-5 minutos para logs aparecerem

Se todos os itens estão ✅ e ainda há erro, inclua:

- Screenshots do erro
- Saída de `dotnet --version`
- Saída de `az --version`
- Logs completos da aplicação
