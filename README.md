# FIAP Cloud Games - Payments API

Microsserviço de Pagamentos responsável por processar (simular) o pagamento de compras de jogos no ecossistema CloudGames.

## 📋 Índice

- [Visão Geral](#-visão-geral)
- [Tecnologias](#-tecnologias)
- [Arquitetura](#-arquitetura)
- [Como Rodar a Aplicação](#-como-rodar-a-aplicação)
- [Endpoints da API](#-endpoints-da-api)
- [Eventos e Mensageria](#-eventos-e-mensageria)
- [Configuração](#-configuração)
- [Kubernetes](#-kubernetes)
- [Estrutura do Projeto](#-estrutura-do-projeto)

---

## 🎯 Visão Geral

O PaymentsAPI é um microsserviço que:

- **Processa pagamentos** de compras de jogos (simulação)
- **Consome eventos** `OrderPlacedEvent` via Azure Service Bus
- **Publica eventos** `PaymentProcessedEvent` após processar pagamentos
- **Expõe API REST** para processamento manual de pagamentos

### Regra de Negócio

O serviço simula um gateway de pagamento:
- ✅ **Aprovado**: Pagamentos com valor inferior a R$ 10.000,00
- ❌ **Rejeitado**: Pagamentos com valor igual ou superior a R$ 10.000,00

---

## 🛠 Tecnologias

| Tecnologia | Versão | Descrição |
|------------|--------|-----------|
| .NET | 9.0 | Framework principal |
| ASP.NET Core | 9.0 | Web API |
| MassTransit | 8.5.7 | Abstração para mensageria |
| MassTransit.Azure.ServiceBus.Core | 8.5.7 | Transporte Azure Service Bus |
| Application Insights | 2.22.0 | Telemetria integrada ao Log Analytics |
| Swashbuckle | 6.5.0 | Documentação Swagger/OpenAPI |
| JWT Bearer | 9.0 | Autenticação via token JWT |
| Docker | - | Containerização |
| Kubernetes | - | Orquestração (opcional) |

---

## 🏗 Arquitetura

```mermaid
flowchart LR
    subgraph Produtores
        A[CatalogAPI]
    end

    subgraph Mensageria
      B[(Azure Service Bus<br/>Topics/Subscriptions)]
    end

    subgraph PaymentsAPI
        C[OrderPlacedConsumer]
        D[PaymentService]
    end

    subgraph Consumidores
        E[NotificationsAPI]
    end

    A -->|OrderPlacedEvent| B
    B -->|OrderPlacedEvent| C
    C --> D
    D -->|PaymentProcessedEvent| B
    B -->|PaymentProcessedEvent| E
```

### Fluxo de Dados

1. **Recebe** `OrderPlacedEvent` da fila `OrderPlacedEvent`
2. **Processa** o pagamento via `PaymentService`
3. **Publica** `PaymentProcessedEvent` no tópico `payment-processed`

---

## 🚀 Como Rodar a Aplicação

### Pré-requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) e Docker Compose
- [Git](https://git-scm.com/)
- Azure Service Bus Namespace com tópico `order-placed` e assinatura `payments-api`
- Connection string do Application Insights (workspace-based para Log Analytics)

---

### 🐛 Opção 1: Com debug (Recomendado para Desenvolvimento)

Esta opção permite usar **breakpoints** e todas as funcionalidades de debug, conectando diretamente aos serviços Azure.

#### Passo 1: Configurar variáveis de ambiente

Defina as variáveis abaixo antes de iniciar a API:

```bash
export ServiceBus__ConnectionString="<sua-connection-string>"
export ServiceBus__OrderPlacedTopicName="order-placed"
export ServiceBus__OrderPlacedSubscriptionName="payments-api"
export ServiceBus__PaymentProcessedTopicName="payment-processed"
export APPLICATIONINSIGHTS_CONNECTION_STRING="<sua-connection-string-appinsights>"
```

> 💡 **Dica:** Utilize o Azure Portal para validar tópicos/assinaturas do Service Bus e métricas no Application Insights.

#### Passo 2: Rodar a aplicação com Debug no VS Code

1. Abra o projeto no VS Code
2. Abra o arquivo `Payments.slnx` ou a pasta `PaymentsAPI`
3. Coloque seus **breakpoints** nos arquivos desejados (ex: `PaymentsController.cs`, `OrderPlacedConsumer.cs`)
4. Pressione `F5` ou vá em **Run > Start Debugging**
5. Selecione o perfil `http` ou `https`

A aplicação estará disponível em:
- **API:** http://localhost:5059
- **Swagger:** http://localhost:5059/swagger

---

### 🐳 Opção 2: Docker Compose Completo (Produção/Testes)

Para rodar tudo em containers (sem debug/breakpoints):

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/PaymentsAPI.git
cd PaymentsAPI

# Suba todos os serviços
docker-compose up -d

# Verifique se os containers estão rodando
docker-compose ps

# Acompanhe os logs
docker-compose logs -f payments-api
```

**Serviços disponíveis:**
| Serviço | URL | Descrição |
|---------|-----|-----------|
| PaymentsAPI | http://localhost:5058 | API de Pagamentos |
| PaymentsAPI Swagger | http://localhost:5058/swagger | Documentação da API |

> ⚠️ **Nota:** O docker-compose não provisiona recursos Azure. Configure `ServiceBus__ConnectionString` e `APPLICATIONINSIGHTS_CONNECTION_STRING` antes de subir os containers.

---

### 💻 Opção 3: Desenvolvimento Local (sem VS Code Debug)

Para rodar localmente via terminal:

```bash
# 1. Primeiro, exporte as variáveis de ambiente Azure (veja Opção 1 - Passo 1)

# 2. Navegue até o projeto
cd src/Payments.Api

# 3. Restaure as dependências
dotnet restore

# 4. Execute a aplicação
dotnet run

# A API estará disponível em:
# - http://localhost:5059
# - http://localhost:5059/swagger (Swagger UI)
```

### Verificar se a aplicação está rodando

```bash
# Health check
curl http://localhost:5059/api/payments/health

# Resposta esperada:
# {"status":"healthy","service":"PaymentsAPI"}

# Ou acesse a raiz
curl http://localhost:5059

# Resposta esperada:
# PaymentsAPI is running...
```

### Parar a aplicação

```bash
# Docker Compose
docker-compose down

# Para remover volumes também
docker-compose down -v
```

---

## 📡 Endpoints da API

### POST /api/payments/process

Processa um pagamento manualmente via API REST.

**Request:**
```bash
curl -X POST http://localhost:5059/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "11111111-2222-3333-4444-555555555555",
    "gameId": "aaaabbbb-cccc-dddd-eeee-ffffffffffff",
    "emailUser": "usuario@email.com",
    "price": 59.99
  }'
```

**Response (Sucesso - 200):**
```json
{
  "message": "Pagamento aprovado com sucesso.",
  "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Response (Rejeitado - 400):**
```json
{
  "message": "Pagamento recusado.",
  "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### GET /api/payments/health

Verifica o status de saúde do serviço.

**Request:**
```bash
curl http://localhost:5059/api/payments/health
```

**Response:**
```json
{
  "status": "healthy",
  "service": "PaymentsAPI"
}
```

### GET /

Endpoint raiz para verificação rápida.

**Response:**
```
PaymentsAPI is running...
```

### Autenticação JWT

A API utiliza autenticação via **JWT Bearer Token**. Para acessar os endpoints protegidos:

1. Obtenha um token JWT válido através do serviço de autenticação do ecossistema CloudGames
2. Inclua o token no header `Authorization: Bearer <seu-token>`

**Políticas de Autorização:**

| Política | Roles Permitidos | Descrição |
|----------|------------------|-----------|
| `Admin` | `Admin` | Acesso total ao sistema |
| `Leitura` | `Leitura`, `Admin` | Acesso de leitura |

**Exemplo de requisição autenticada:**
```bash
curl -X POST http://localhost:5059/api/payments/process \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"orderId": "...", "userId": "...", "gameId": "...", "emailUser": "...", "price": 59.99}'
```

---

## 📨 Eventos e Mensageria

O serviço utiliza **MassTransit** com **Azure Service Bus** para comunicação assíncrona.

### OrderPlacedEvent (Consumidor)

Este serviço consome o evento `OrderPlacedEvent` via tópico e assinatura no Service Bus.

**Tópico:** `order-placed`  
**Assinatura:** `payments-api`

**Payload:**
```json
{
  "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "11111111-2222-3333-4444-555555555555",
  "gameId": "aaaabbbb-cccc-dddd-eeee-ffffffffffff",
  "emailUser": "usuario@email.com",
  "price": 59.99
}
```

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `orderId` | string | Identificador único do pedido |
| `userId` | string | Identificador do usuário |
| `gameId` | string | Identificador do jogo comprado |
| `emailUser` | string | Email do usuário para notificação |
| `price` | decimal | Valor do pagamento |

> **⚠️ Importante:** O contrato da mensagem deve ser compatível com o tipo `OrderPlacedEvent` publicado no tópico do Service Bus.

### PaymentProcessedEvent (Produtor)

Após processar o pagamento, o serviço publica um evento `PaymentProcessedEvent`.

**Tópico:** `payment-processed`

**Payload:**
```json
{
  "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "11111111-2222-3333-4444-555555555555",
  "gameId": "aaaabbbb-cccc-dddd-eeee-ffffffffffff",
  "emailUser": "usuario@email.com",
  "price": 59.99,
  "status": "Approved"
}
```

| Campo | Tipo | Valores | Descrição |
|-------|------|---------|-----------|
| `orderId` | string | - | Identificador do pedido |
| `userId` | string | - | Identificador do usuário |
| `gameId` | string | - | Identificador do jogo comprado |
| `emailUser` | string | - | Email do usuário para notificação |
| `price` | decimal | - | Valor do pagamento |
| `status` | enum | `Approved`, `Rejected` | Resultado do pagamento (PaymentStatus) |

---

## ⚙️ Configuração

### Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Development` |
| `ASPNETCORE_URLS` | URLs da aplicação | `http://localhost:5059` |
| `Jwt__Key` | Chave secreta para assinatura do JWT | `chave-secreta-super-forte-123456` |
| `Jwt__Issuer` | Emissor do token JWT | `CloudGamesAPI` |
| `Jwt__Audience` | Audiência do token JWT | `CloudGamesAPIClient` |
| `ServiceBus__ConnectionString` | Connection string do Azure Service Bus | `` |
| `ServiceBus__OrderPlacedTopicName` | Tópico de entrada | `order-placed` |
| `ServiceBus__OrderPlacedSubscriptionName` | Assinatura de entrada | `payments-api` |
| `ServiceBus__PaymentProcessedTopicName` | Tópico de saída | `payment-processed` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetria para Application Insights/Log Analytics | `` |

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "chave-secreta-super-forte-123456",
    "Issuer": "CloudGamesAPI",
    "Audience": "CloudGamesAPIClient"
  },
  "ServiceBus": {
    "ConnectionString": "",
    "OrderPlacedTopicName": "order-placed",
    "OrderPlacedSubscriptionName": "payments-api",
    "PaymentProcessedTopicName": "payment-processed"
  },
  "AllowedHosts": "*"
}
```

## ☸️ Kubernetes

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) com Kubernetes habilitado
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (já incluso no Docker Desktop)

### Habilitar Kubernetes no Docker Desktop

1. Abra o **Docker Desktop**
2. Vá em **Settings** (ícone de engrenagem)
3. Clique em **Kubernetes** no menu lateral
4. Marque **Enable Kubernetes**
5. Clique em **Apply & Restart**
6. Aguarde o Kubernetes iniciar (ícone verde no canto inferior esquerdo)

### Deploy da Aplicação

#### Passo 1: Construir a imagem Docker

```bash
# Na raiz do projeto
docker build -t payments-api:latest .
```

#### Passo 2: Aplicar os manifests Kubernetes

```bash
# Aplicar todos os recursos (ConfigMap, Secret, Deployment e Service)
kubectl apply -f ./k8s/
```

**Saída esperada:**
```
configmap/payments-api-config created
deployment.apps/payments-api created
secret/payments-api-secret created
service/payments-api created
```

#### Passo 3: Verificar o status

```bash
# Ver status dos pods
kubectl get pods

# Ver status dos serviços
kubectl get services

# Ver logs da aplicação
kubectl logs -f deployment/payments-api
```

**Saída esperada:**
```
NAME                           READY   STATUS    RESTARTS   AGE
payments-api-75b78fc9f-xxxxx   1/1     Running   0          30s
```

#### Passo 4: Acessar a aplicação

Como o Service é do tipo `ClusterIP`, use **port-forward** para acessar localmente:

```bash
kubectl port-forward service/payments-api 5058:5058
```

A aplicação estará disponível em:
- **API:** http://localhost:5058
- **Swagger:** http://localhost:5058/swagger

### Arquivos de Configuração Kubernetes

| Arquivo | Descrição |
|---------|-----------|
| `k8s/configmap.yaml` | Configurações não-sensíveis (nomes de tópico/assinatura do Service Bus) |
| `k8s/secret.yaml` | Credenciais sensíveis (Service Bus e Application Insights) |
| `k8s/deployment.yaml` | Definição do pod, replicas, health checks e recursos |
| `k8s/service.yaml` | Exposição do serviço internamente no cluster |

### Comandos Úteis

```bash
# Ver detalhes do pod
kubectl describe pod -l app=payments-api

# Ver eventos do cluster
kubectl get events --sort-by='.lastTimestamp'

# Escalar replicas
kubectl scale deployment/payments-api --replicas=3

# Atualizar após mudanças na imagem
docker build -t payments-api:latest .
kubectl rollout restart deployment/payments-api

# Remover todos os recursos
kubectl delete -f ./k8s/
```

### Troubleshooting

| Problema | Solução |
|----------|---------|
| Pod em `CrashLoopBackOff` | Verifique logs: `kubectl logs deployment/payments-api` |
| Pod em `Pending` | Verifique recursos: `kubectl describe pod -l app=payments-api` |
| Conexão recusada | Verifique se o port-forward está ativo |
| Service Bus não conecta | Verifique `ServiceBus__ConnectionString` e permissões para tópico/assinatura |

> ⚠️ **Nota:** A aplicação depende de recursos Azure externos (Service Bus e Application Insights). Configure os secrets antes do deploy.

---

## 📁 Estrutura do Projeto

```
PaymentsAPI/
├── docker-compose.yaml          # Orquestração Docker
├── Dockerfile                   # Build da imagem
├── Payments.slnx                # Solução .NET
├── README.md                    # Esta documentação
├── k8s/                         # Manifests Kubernetes
│   ├── configmap.yaml           # Configurações (env vars)
│   ├── deployment.yaml          # Definição do pod
│   ├── secret.yaml              # Credenciais (Base64)
│   └── service.yaml             # Exposição do serviço
└── src/
    └── Payments.Api/
        ├── Program.cs           # Ponto de entrada
        ├── Payments.Api.csproj  # Projeto .NET
        ├── appsettings.json     # Configurações
        ├── Configurations/
        │   └── MassTransitConfig.cs    # Config do MassTransit/Azure Service Bus
        ├── Consumers/
        │   └── OrderPlacedConsumer.cs  # Consumidor de eventos
        ├── Controllers/
        │   └── PaymentsController.cs   # Endpoints REST
        ├── Models/
        │   ├── OrderPlacedEvent.cs     # Evento de entrada
        │   ├── PaymentProcessedEvent.cs# Evento de saída
        │   ├── PaymentRequest.cs       # Request da API
        │   └── ServiceBusSettings.cs   # Configurações Azure Service Bus
        ├── Properties/
        │   └── launchSettings.json     # Perfis de execução
        └── Services/
            ├── PaymentService.cs       # Lógica de pagamento
            └── Interfaces/
            └── IPaymentService.cs  # Contrato do serviço
```

---

## 🔗 Serviços Relacionados

Este microsserviço faz parte do ecossistema **CloudGames**:

| Serviço | Descrição | Comunicação |
|---------|-----------|-------------|
| **CatalogAPI** | Gerencia pedidos | Produz `OrderPlacedEvent` |
| **PaymentsAPI** | Processa pagamentos | Consome `OrderPlacedEvent`, Produz `PaymentProcessedEvent` |
| **NotificationsAPI** | Envia notificações | Consome `PaymentProcessedEvent` |

---

## 📝 Licença

Este projeto faz parte de um estudo de arquitetura de microsserviços.
