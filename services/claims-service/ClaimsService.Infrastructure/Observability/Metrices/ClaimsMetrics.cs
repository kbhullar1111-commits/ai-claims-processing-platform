using System.Diagnostics.Metrics;
using ClaimsService.Application.Interfaces;
using ClaimsService.Infrastructure.Observability.Constants;

namespace ClaimsService.Infrastructure.Observability.Metrics;

public class ClaimsMetrics : IClaimsMetrics
{
    public const string MeterName = TelemetryConstants.MeterName;

    public static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _claimsSubmittedCount;

    public ClaimsMetrics()
    {
        _claimsSubmittedCount = Meter.CreateCounter<long>("claims_submitted_total");
    }

    public void ClaimsSubmitted()
        => _claimsSubmittedCount.Add(1);
}