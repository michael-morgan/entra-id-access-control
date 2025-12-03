using Api.Modules.DemoApi.Models.Claims;
using System.Collections.Concurrent;

namespace Api.Modules.DemoApi.Data;

/// <summary>
/// In-memory repository for Claim entities.
/// </summary>
public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetByRegionAsync(string region, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetByClaimantAsync(string claimantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetByAdjudicatorAsync(string adjudicatorId, CancellationToken cancellationToken = default);
    Task<Claim> CreateAsync(Claim claim, CancellationToken cancellationToken = default);
    Task<Claim> UpdateAsync(Claim claim, CancellationToken cancellationToken = default);
}

public class InMemoryClaimRepository : IClaimRepository
{
    private readonly ConcurrentDictionary<Guid, Claim> _claims = new();

    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _claims.TryGetValue(id, out var claim);
        return Task.FromResult(claim);
    }

    public Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Claim>>(_claims.Values.ToList());
    }

    public Task<IReadOnlyList<Claim>> GetByRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        var claims = _claims.Values.Where(c => c.Region == region).ToList();
        return Task.FromResult<IReadOnlyList<Claim>>(claims);
    }

    public Task<IReadOnlyList<Claim>> GetByClaimantAsync(string claimantId, CancellationToken cancellationToken = default)
    {
        var claims = _claims.Values.Where(c => c.ClaimantId == claimantId).ToList();
        return Task.FromResult<IReadOnlyList<Claim>>(claims);
    }

    public Task<IReadOnlyList<Claim>> GetByAdjudicatorAsync(string adjudicatorId, CancellationToken cancellationToken = default)
    {
        var claims = _claims.Values.Where(c => c.AssignedAdjudicatorId == adjudicatorId).ToList();
        return Task.FromResult<IReadOnlyList<Claim>>(claims);
    }

    public Task<Claim> CreateAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        claim.CreatedAt = DateTimeOffset.UtcNow;
        _claims[claim.Id] = claim;
        return Task.FromResult(claim);
    }

    public Task<Claim> UpdateAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        claim.ModifiedAt = DateTimeOffset.UtcNow;
        _claims[claim.Id] = claim;
        return Task.FromResult(claim);
    }
}
