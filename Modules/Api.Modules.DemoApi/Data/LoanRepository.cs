using Api.Modules.DemoApi.Models.Loans;
using System.Collections.Concurrent;

namespace Api.Modules.DemoApi.Data;

/// <summary>
/// In-memory repository for Loan entities.
/// For production, replace with EF Core DbContext.
/// </summary>
public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetByRegionAsync(string region, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetByApplicantAsync(string applicantId, CancellationToken cancellationToken = default);
    Task<Loan> CreateAsync(Loan loan, CancellationToken cancellationToken = default);
    Task<Loan> UpdateAsync(Loan loan, CancellationToken cancellationToken = default);
}

public class InMemoryLoanRepository : ILoanRepository
{
    private readonly ConcurrentDictionary<Guid, Loan> _loans = new();

    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _loans.TryGetValue(id, out var loan);
        return Task.FromResult(loan);
    }

    public Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Loan>>(_loans.Values.ToList());
    }

    public Task<IReadOnlyList<Loan>> GetByRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        var loans = _loans.Values.Where(l => l.Region == region).ToList();
        return Task.FromResult<IReadOnlyList<Loan>>(loans);
    }

    public Task<IReadOnlyList<Loan>> GetByApplicantAsync(string applicantId, CancellationToken cancellationToken = default)
    {
        var loans = _loans.Values.Where(l => l.ApplicantId == applicantId).ToList();
        return Task.FromResult<IReadOnlyList<Loan>>(loans);
    }

    public Task<Loan> CreateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        loan.CreatedAt = DateTimeOffset.UtcNow;
        _loans[loan.Id] = loan;
        return Task.FromResult(loan);
    }

    public Task<Loan> UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        loan.ModifiedAt = DateTimeOffset.UtcNow;
        _loans[loan.Id] = loan;
        return Task.FromResult(loan);
    }
}
