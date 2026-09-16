using PolicyService.Domain.Enums;
using PolicyService.Domain.Entities;

namespace PolicyService.Application.Policies;

public sealed record UpdatePolicyCommand(
    Guid PolicyId,
    PolicyStatus Status);