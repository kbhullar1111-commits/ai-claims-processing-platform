using PolicyService.Domain.Enums;

namespace PolicyService.Domain.Entities;
public sealed record Coverage(
    CoverageType Type,
    decimal CoverageLimit);