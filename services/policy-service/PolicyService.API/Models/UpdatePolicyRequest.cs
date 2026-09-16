using PolicyService.Domain.Enums;

namespace PolicyService.API.Models;

public sealed record UpdatePolicyRequest(
    PolicyStatus Status);