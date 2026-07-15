

namespace Deployment.Platform.Application.Interfaces.Validation;

public interface IExecutionEnvironmentValidator{
    Task ValidateAsync(CancellationToken cancellationToken);
    
}