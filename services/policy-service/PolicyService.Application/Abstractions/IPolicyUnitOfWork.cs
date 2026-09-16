namespace PolicyService.Application.Abstractions;

public interface IPolicyUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}