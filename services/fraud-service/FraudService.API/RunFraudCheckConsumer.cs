using MassTransit;
using BuildingBlocks.Contracts.Fraud;

public class RunFraudCheckConsumer : IConsumer<RunFraudCheck>
{
    public async Task Consume(ConsumeContext<RunFraudCheck> context)
    {
        await Task.Delay(1500);

        await context.Publish(new FraudCheckCompleted(
            context.Message.ClaimId,
            0.2m, // RiskScore
            false,
            null, // Reason
            DateTime.UtcNow
        ));
    }
}