# Queries KQL - Log Analytics / Application Insights

Queries úteis para monitoramento do PaymentsAPI no Azure Log Analytics.

---

## 📊 Dashboards e Queries Essenciais

### 1. Logs Gerais da Aplicação

```kql
// Todos os logs das últimas 24 horas
traces
| where timestamp > ago(24h)
| order by timestamp desc
| project timestamp, message, severityLevel, customDimensions
```

### 2. Logs de Processamento de Pagamentos

```kql
// Logs relacionados a processamento de pagamentos
traces
| where timestamp > ago(24h)
| where message contains "OrderId" or message contains "Payment"
| order by timestamp desc
| project timestamp, message, severityLevel, operation_Name
```

### 3. Erros e Exceções

```kql
// Todas as exceções nas últimas 24 horas
exceptions
| where timestamp > ago(24h)
| order by timestamp desc
| project
    timestamp,
    type,
    outerMessage,
    innermostMessage,
    operation_Name,
    problemId
```

### 4. Performance de Requisições HTTP

```kql
// Tempo de resposta das APIs
requests
| where timestamp > ago(24h)
| summarize
    Count = count(),
    AvgDuration = avg(duration),
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
    by name
| order by Count desc
```

### 5. Taxa de Sucesso das Requisições

```kql
// Taxa de sucesso por endpoint
requests
| where timestamp > ago(24h)
| summarize
    Total = count(),
    Success = countif(success == true),
    Failed = countif(success == false)
    by name, resultCode
| extend SuccessRate = (Success * 100.0) / Total
| order by Total desc
```

### 6. Eventos de Mensageria (Service Bus)

```kql
// Logs de consumo e publicação de mensagens
traces
| where timestamp > ago(1h)
| where message contains "OrderPlaced" or message contains "PaymentProcessed"
| order by timestamp desc
| project
    timestamp,
    message,
    severityLevel,
    customDimensions.OrderId,
    customDimensions.Status
```

### 7. Alertas de Falhas de Pagamento

```kql
// Pagamentos rejeitados nas últimas 24h
traces
| where timestamp > ago(24h)
| where message contains "Pagamento recusado" or message contains "Status=Rejected"
| summarize Count = count() by bin(timestamp, 1h)
| render timechart
```

### 8. Volume de Transações por Hora

```kql
// Contagem de processamentos de pagamento por hora
traces
| where timestamp > ago(24h)
| where message contains "ProcessPayment iniciado"
| summarize Transactions = count() by bin(timestamp, 1h)
| render timechart
```

### 9. Correlação de Logs por OrderId

```kql
// Rastrear todo o fluxo de um pedido específico
let orderId = "123e4567-e89b-12d3-a456-426614174000";
union traces, requests, exceptions
| where timestamp > ago(24h)
| where * contains orderId
| order by timestamp asc
| project timestamp, itemType, message, severityLevel, operation_Name
```

### 10. Latência de Dependências (Service Bus)

```kql
// Tempo de resposta do Service Bus
dependencies
| where timestamp > ago(24h)
| where type == "Azure Service Bus"
| summarize
    Count = count(),
    AvgDuration = avg(duration),
    P95 = percentile(duration, 95)
    by target, name
| order by Count desc
```

---

## 🚨 Alertas Recomendados

### Alert 1: Taxa de Erro Alta

```kql
// Dispara se taxa de erro > 5% em 15 minutos
requests
| where timestamp > ago(15m)
| summarize
    Total = count(),
    Failed = countif(success == false)
| extend ErrorRate = (Failed * 100.0) / Total
| where ErrorRate > 5
```

**Configuração no Portal:**

- Threshold: ErrorRate > 5
- Evaluation frequency: 5 minutes
- Time window: 15 minutes
- Severity: 2 (Warning)

### Alert 2: Exceções Críticas

```kql
// Dispara quando houver exceções
exceptions
| where timestamp > ago(5m)
| where severityLevel >= 3
| summarize Count = count()
| where Count > 0
```

**Configuração no Portal:**

- Threshold: Count > 0
- Evaluation frequency: 5 minutes
- Severity: 1 (Error)

### Alert 3: Service Bus Connection Failed

```kql
// Dispara quando houver falha de conexão com Service Bus
exceptions
| where timestamp > ago(5m)
| where outerMessage contains "ServiceBusConnectionException"
| summarize Count = count()
| where Count > 0
```

**Configuração no Portal:**

- Threshold: Count > 0
- Evaluation frequency: 5 minutes
- Severity: 0 (Critical)

### Alert 4: Latência Alta

```kql
// Dispara se P95 > 2 segundos
requests
| where timestamp > ago(15m)
| summarize P95 = percentile(duration, 95)
| where P95 > 2000
```

**Configuração no Portal:**

- Threshold: P95 > 2000ms
- Evaluation frequency: 5 minutes
- Time window: 15 minutes
- Severity: 2 (Warning)

---

## 📈 Workbook de Monitoramento

### Dashboard Completo - JSON Template

Para criar um workbook personalizado:

1. No Application Insights, vá em **Workbooks** → **+ New**
2. Adicione as queries abaixo como **Query tiles**

#### Tile 1: Overview de Saúde

```kql
let timeRange = 1h;
let healthData = requests
| where timestamp > ago(timeRange)
| summarize
    TotalRequests = count(),
    SuccessfulRequests = countif(success == true),
    FailedRequests = countif(success == false),
    AvgResponseTime = avg(duration)
| extend SuccessRate = (SuccessfulRequests * 100.0) / TotalRequests;
healthData
| project
    Metric = "Health Status",
    TotalRequests,
    SuccessRate = round(SuccessRate, 2),
    AvgResponseTime = round(AvgResponseTime, 2),
    FailedRequests
```

#### Tile 2: Top Endpoints

```kql
requests
| where timestamp > ago(1h)
| summarize Count = count(), AvgDuration = avg(duration) by name
| order by Count desc
| take 10
```

#### Tile 3: Errors Over Time

```kql
exceptions
| where timestamp > ago(24h)
| summarize Count = count() by bin(timestamp, 1h), type
| render timechart
```

---

## 🔍 Queries Avançadas de Troubleshooting

### 1. Identificar Gargalos de Performance

```kql
// Requisições mais lentas (top 10)
requests
| where timestamp > ago(1h)
| order by duration desc
| take 10
| project
    timestamp,
    name,
    duration,
    resultCode,
    success,
    operation_Id
```

### 2. Trace End-to-End de uma Transação

```kql
// Usar operation_Id para rastrear toda a transação
let operationId = "your-operation-id-here";
union traces, requests, dependencies, exceptions
| where operation_Id == operationId
| order by timestamp asc
| project timestamp, itemType, name, message, duration, success
```

### 3. Análise de Dependências Externas

```kql
// Todas as chamadas a dependências externas
dependencies
| where timestamp > ago(24h)
| summarize
    Count = count(),
    FailureCount = countif(success == false),
    AvgDuration = avg(duration)
    by target, type
| extend FailureRate = (FailureCount * 100.0) / Count
| order by FailureRate desc
```

### 4. Logs com Context Custom Dimensions

```kql
// Extrair custom dimensions dos logs
traces
| where timestamp > ago(1h)
| extend
    OrderId = tostring(customDimensions.OrderId),
    UserId = tostring(customDimensions.UserId),
    Status = tostring(customDimensions.Status)
| where isnotempty(OrderId)
| project timestamp, message, OrderId, UserId, Status
| order by timestamp desc
```

### 5. Detecção de Anomalias

```kql
// Detectar picos anormais no volume de requisições
requests
| where timestamp > ago(7d)
| make-series RequestCount = count() default = 0 on timestamp step 1h
| extend anomalies = series_decompose_anomalies(RequestCount, 1.5)
| render anomalychart with (anomalycolumns=anomalies)
```

---

## 📦 Exportar Logs para Análise

### Export para CSV

```kql
// Exportar dados de pagamentos para análise
traces
| where timestamp > ago(7d)
| where message contains "Payment"
| extend
    OrderId = extract(@"OrderId=([a-f0-9\-]+)", 1, message),
    Status = extract(@"Status=(\w+)", 1, message)
| where isnotempty(OrderId)
| project timestamp, OrderId, Status, message
| order by timestamp desc
```

Após executar, clique em **Export** → **Export to CSV**

---

## 🎯 Métricas de SLA

### Disponibilidade (Target: 99.9%)

```kql
requests
| where timestamp > ago(30d)
| summarize
    Total = count(),
    Success = countif(success == true)
| extend Availability = (Success * 100.0) / Total
| project
    Period = "Last 30 days",
    Availability = round(Availability, 3),
    Target = 99.9,
    Status = iff(Availability >= 99.9, "✅ Met", "❌ Missed")
```

### Latência P95 (Target: < 1s)

```kql
requests
| where timestamp > ago(30d)
| summarize P95 = percentile(duration, 95)
| extend
    P95_Seconds = round(P95 / 1000, 2),
    Target = 1.0,
    Status = iff(P95 < 1000, "✅ Met", "❌ Missed")
| project P95_Seconds, Target, Status
```

---

## 🔗 Recursos Adicionais

- [KQL Quick Reference](https://docs.microsoft.com/azure/data-explorer/kql-quick-reference)
- [Application Insights Query Language](https://docs.microsoft.com/azure/azure-monitor/logs/get-started-queries)
- [Workbooks Documentation](https://docs.microsoft.com/azure/azure-monitor/visualize/workbooks-overview)

---

**💡 Dica**: Salve suas queries favoritas como **Functions** no Log Analytics para reutilização!
