# Observability Queries (Application Insights / Log Analytics)

Generated: 2026-04-28

Use these KQL queries in Application Insights Logs. Replace placeholder values such as `CLAIM-ID` when needed.

## 1) Latest Claim Workflow Logs
```kusto
traces
| where message contains "Claim"
| order by timestamp desc
| take 100
```

## 2) End-to-End Single Claim
```kusto
let claimId = "CLAIM-ID";
union traces, requests, dependencies, exceptions
| where message contains claimId
    or tostring(customDimensions.ClaimId) == claimId
    or operation_Id == claimId
| order by timestamp asc
```

## 3) Failed Dependencies
```kusto
dependencies
| where success == false
| summarize count() by target, name, resultCode
| order by count_ desc
```

## 4) Slowest APIs
```kusto
requests
| summarize avg_duration=avg(duration), max_duration=max(duration), request_count=count() by name
| order by avg_duration desc
```

## 5) Queue Consumer Performance
```kusto
requests
| where name has "ServiceBus" or name has "ProcessMessage"
| summarize avg_duration=avg(duration), max_duration=max(duration), request_count=count() by cloud_RoleName, name
| order by avg_duration desc
```

## 6) Exceptions (Latest)
```kusto
exceptions
| order by timestamp desc
| take 50
```

## 7) Claims Approved Today
```kusto
traces
| where timestamp >= startofday(now())
| where message contains "Claim approved"
| summarize approved_count=count()
```

## 8) Workflow Throughput by Hour
```kusto
traces
| where message contains "Claim approved"
| summarize approved_count=count() by bin(timestamp, 1h)
| render timechart
```

## 9) Noisy OTLP Calls
```kusto
dependencies
| where target contains "4317" or name contains "opentelemetry.proto.collector.trace.v1.TraceService"
| summarize call_count=count(), avg_duration=avg(duration), max_duration=max(duration) by target, name
| order by call_count desc
```

## 10) Payment Latency Trend
```kusto
requests
| where cloud_RoleName contains "payment" or name contains "Payment"
| summarize avg_duration=avg(duration), max_duration=max(duration), request_count=count() by bin(timestamp, 15m)
| render timechart
```

## 11) Service Bus Dependency Volume
```kusto
dependencies
| where target contains "servicebus.windows.net"
| summarize call_count=count(), failed_count=countif(success == false), avg_duration=avg(duration) by cloud_RoleName, name, target
| order by call_count desc
```

## 12) Trace-Request-Dependency Correlation by Operation
```kusto
let claimId = "CLAIM-ID";
let ops = traces
| where tostring(customDimensions.ClaimId) == claimId or message contains claimId
| distinct operation_Id;
union traces, requests, dependencies, exceptions
| where operation_Id in (ops)
| project timestamp, itemType, operation_Id, cloud_RoleName, name, message, success, resultCode, duration
| order by timestamp asc
```

## 13) Top Dependency Targets (Last 24h)
```kusto
dependencies
| where timestamp > ago(24h)
| summarize call_count=count(), failed_count=countif(success == false), avg_duration=avg(duration) by target
| order by call_count desc
```

## 14) Error Rate by API (Last 24h)
```kusto
requests
| where timestamp > ago(24h)
| summarize total=count(), failed=countif(success == false) by name
| extend error_rate_pct = round(100.0 * todouble(failed) / iif(total == 0, 1, total), 2)
| order by error_rate_pct desc, total desc
```

## 15) API Health Endpoint Traffic
```kusto
requests
| where name has "/health" or name has "/live" or name has "/ready"
| summarize count(), failed=countif(success == false), avg_duration=avg(duration) by cloud_RoleName, name
| order by count_ desc
```

## Notes
- If your claim id is not in `customDimensions.ClaimId`, first verify how it is logged in traces or scopes.
- For service-specific views, common role names are `claims-api`, `document-api`, `notification-api`, `fraud-api`, and `payment-api`.
- Old OTLP noise can remain in historical data even after code/config fixes; focus validation on new timestamps.
