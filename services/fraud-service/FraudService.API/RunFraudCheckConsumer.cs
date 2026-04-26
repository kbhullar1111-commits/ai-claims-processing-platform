using MassTransit;
using BuildingBlocks.Contracts.Fraud;
using Microsoft.Extensions.Logging;

public class RunFraudCheckConsumer : IConsumer<RunFraudCheck>
{
    private readonly ILogger<RunFraudCheckConsumer> _logger;

    public RunFraudCheckConsumer(ILogger<RunFraudCheckConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RunFraudCheck> context)
    {
        _logger.LogInformation(
            "Fraud check started. ClaimId={ClaimId}",
            context.Message.ClaimId);

        await Task.Delay(1500);

        var riskScore = 0.2m;
        var isFraudulent = false;

        _logger.LogInformation(
            "Fraud check completed. ClaimId={ClaimId}, RiskScore={RiskScore}, IsFraudulent={IsFraudulent}",
            context.Message.ClaimId,
            riskScore,
            isFraudulent);

        await context.Publish(new FraudCheckCompleted(
            context.Message.ClaimId,
            riskScore,
            isFraudulent,
            null,
            DateTime.UtcNow
        ));
    }
}