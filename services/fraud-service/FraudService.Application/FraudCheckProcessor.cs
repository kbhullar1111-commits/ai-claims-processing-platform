using BuildingBlocks.Contracts.Fraud;
using Microsoft.Extensions.Logging;

namespace FraudService.Application;

public sealed class FraudCheckProcessor(ILogger<FraudCheckProcessor> logger) : IFraudCheckProcessor
{
    public async Task<FraudCheckCompleted> ProcessAsync(RunFraudCheck command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Fraud check started. ClaimId={ClaimId}", command.ClaimId);

        await Task.Delay(1000, cancellationToken);

        var riskScore = 0.2m;
        var isFraudulent = false;

        logger.LogInformation(
            "Fraud check completed. ClaimId={ClaimId}, RiskScore={RiskScore}, IsFraudulent={IsFraudulent}",
            command.ClaimId,
            riskScore,
            isFraudulent);

        return new FraudCheckCompleted(
            command.ClaimId,
            riskScore,
            isFraudulent,
            null,
            DateTime.UtcNow
        );
    }
}