using BuildingBlocks.Contracts.Fraud;

namespace FraudService.Application;
public interface IFraudCheckProcessor
{
    Task<FraudCheckCompleted> ProcessAsync(RunFraudCheck command, CancellationToken cancellationToken);
}