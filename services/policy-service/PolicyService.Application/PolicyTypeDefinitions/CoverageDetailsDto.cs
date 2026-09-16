using PolicyService.Domain.Enums;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed record CoverageDetailsDto(
    CoverageType Type,
    decimal CoverageLimit);