using System.Diagnostics.Metrics;
using ClaimsService.Application.Interfaces;
using ClaimsService.Infrastructure.Observability.Constants;

namespace ClaimsService.Infrastructure.Observability.Metrics;

public class ClaimsMetrics : IClaimsMetrics
{
    public const string MeterName = TelemetryConstants.MeterName;

    public static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _claimsSubmittedCount;
    private readonly Counter<long> _claimsApprovedCount;
    private readonly Counter<long> _claimsRejectedCount;

    public ClaimsMetrics()
    {
        _claimsSubmittedCount = Meter.CreateCounter<long>("claims_submitted_total");
        _claimsApprovedCount  = Meter.CreateCounter<long>("claims_approved_total");
        _claimsRejectedCount  = Meter.CreateCounter<long>("claims_rejected_total");
    }

    public void ClaimsSubmitted()
        => _claimsSubmittedCount.Add(1);

    public void ClaimApproved()
        => _claimsApprovedCount.Add(1);

    public void ClaimRejected(string reason)
        => _claimsRejectedCount.Add(1, new KeyValuePair<string, object?>("reason", reason));
}