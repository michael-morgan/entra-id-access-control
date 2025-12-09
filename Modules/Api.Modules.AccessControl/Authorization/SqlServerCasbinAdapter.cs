using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Casbin;
using Casbin.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// SQL Server adapter for Casbin that stores policies in the database.
/// Custom implementation for loading policies.
/// </summary>
public class SqlServerCasbinAdapter(AccessControlDbContext context)
{
    private readonly AccessControlDbContext _context = context;

    public void LoadPolicy(IEnforcer enforcer)
    {
        var policies = _context.CasbinPolicies
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToList();

        Console.WriteLine($"[CASBIN DEBUG] Loaded {policies.Count} policies from database");

        foreach (var policy in policies)
        {
            Console.WriteLine($"[CASBIN DEBUG] Policy: type={policy.PolicyType}, v0={policy.V0}, v1={policy.V1}, v2={policy.V2}, v3={policy.V3}, v4={policy.V4}");
            LoadPolicyLine(policy, enforcer);
        }
    }

    public void LoadPolicy(IPolicyStore store)
    {
        var policies = _context.CasbinPolicies
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToList();

        Console.WriteLine($"[CASBIN DEBUG] Loaded {policies.Count} policies from database");

        foreach (var policy in policies)
        {
            Console.WriteLine($"[CASBIN DEBUG] Policy: type={policy.PolicyType}, v0={policy.V0}, v1={policy.V1}, v2={policy.V2}, v3={policy.V3}, v4={policy.V4}");
            LoadPolicyLine(policy, store);
        }
    }

    public async Task LoadPolicyAsync(IPolicyStore store)
    {
        var policies = await _context.CasbinPolicies
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync();

        foreach (var policy in policies)
        {
            LoadPolicyLine(policy, store);
        }
    }

    private static void LoadPolicyLine(CasbinPolicy policy, IEnforcer enforcer)
    {
        var valuesList = new List<string>();
        if (!string.IsNullOrWhiteSpace(policy.V0)) valuesList.Add(policy.V0);
        if (!string.IsNullOrWhiteSpace(policy.V1)) valuesList.Add(policy.V1);
        if (!string.IsNullOrWhiteSpace(policy.V2)) valuesList.Add(policy.V2);
        if (!string.IsNullOrWhiteSpace(policy.V3)) valuesList.Add(policy.V3);
        if (!string.IsNullOrWhiteSpace(policy.V4)) valuesList.Add(policy.V4);
        if (!string.IsNullOrWhiteSpace(policy.V5)) valuesList.Add(policy.V5);

        // Use the appropriate enforcer method based on policy type
        if (policy.PolicyType.StartsWith("g"))
        {
            // Grouping policy (role assignment) - use AddNamedGroupingPolicy for custom g types
            enforcer.AddNamedGroupingPolicy(policy.PolicyType, [.. valuesList]);
        }
        else
        {
            // Regular policy
            enforcer.AddNamedPolicy(policy.PolicyType, [.. valuesList]);
        }
    }

    private static void LoadPolicyLine(CasbinPolicy policy, IPolicyStore store)
    {
        var valuesList = new List<string>();
        if (!string.IsNullOrWhiteSpace(policy.V0)) valuesList.Add(policy.V0);
        if (!string.IsNullOrWhiteSpace(policy.V1)) valuesList.Add(policy.V1);
        if (!string.IsNullOrWhiteSpace(policy.V2)) valuesList.Add(policy.V2);
        if (!string.IsNullOrWhiteSpace(policy.V3)) valuesList.Add(policy.V3);
        if (!string.IsNullOrWhiteSpace(policy.V4)) valuesList.Add(policy.V4);
        if (!string.IsNullOrWhiteSpace(policy.V5)) valuesList.Add(policy.V5);

        // Get section based on policy type (p for policies, g for grouping)
        var section = policy.PolicyType.StartsWith("g") ? "g" : "p";

        // Create policy values from list using Casbin's factory method
        var policyValues = Casbin.Model.Policy.ValuesFrom(valuesList);
        store.AddPolicy(section, policy.PolicyType, policyValues);
    }

    public void SavePolicy(IPolicyStore store)
    {
        throw new NotImplementedException("Use SavePolicyAsync for database operations");
    }

    public static async Task SavePolicyAsync(IPolicyStore store)
    {
        // Note: This method is typically not needed for read-only policy loading from database
        // For this POC, policies are managed directly in the database via seed data
        // If dynamic policy updates are needed, implement policy iteration logic here
        await Task.CompletedTask;
        throw new NotImplementedException("SavePolicyAsync not implemented - policies are managed directly in database");
    }

    public void AddPolicy(string sec, string ptype, IEnumerable<string> rule)
    {
        throw new NotImplementedException("Use AddPolicyAsync for database operations");
    }

    public async Task AddPolicyAsync(string sec, string ptype, IEnumerable<string> rule)
    {
        var ruleArray = rule.ToArray();
        var policy = new CasbinPolicy
        {
            PolicyType = ptype,
            V0 = ruleArray.Length > 0 ? ruleArray[0] : string.Empty,
            V1 = ruleArray.Length > 1 ? ruleArray[1] : null,
            V2 = ruleArray.Length > 2 ? ruleArray[2] : null,
            V3 = ruleArray.Length > 3 ? ruleArray[3] : null,
            V4 = ruleArray.Length > 4 ? ruleArray[4] : null,
            V5 = ruleArray.Length > 5 ? ruleArray[5] : null
        };

        _context.CasbinPolicies.Add(policy);
        await _context.SaveChangesAsync();
    }

    public void RemovePolicy(string sec, string ptype, IEnumerable<string> rule)
    {
        throw new NotImplementedException("Use RemovePolicyAsync for database operations");
    }

    public async Task RemovePolicyAsync(string sec, string ptype, IEnumerable<string> rule)
    {
        var ruleArray = rule.ToArray();

        var policy = await _context.CasbinPolicies
            .FirstOrDefaultAsync(p =>
                p.PolicyType == ptype &&
                p.V0 == (ruleArray.Length > 0 ? ruleArray[0] : string.Empty) &&
                p.V1 == (ruleArray.Length > 1 ? ruleArray[1] : null) &&
                p.V2 == (ruleArray.Length > 2 ? ruleArray[2] : null) &&
                p.V3 == (ruleArray.Length > 3 ? ruleArray[3] : null) &&
                p.V4 == (ruleArray.Length > 4 ? ruleArray[4] : null) &&
                p.V5 == (ruleArray.Length > 5 ? ruleArray[5] : null));

        if (policy != null)
        {
            _context.CasbinPolicies.Remove(policy);
            await _context.SaveChangesAsync();
        }
    }

    public void RemoveFilteredPolicy(string sec, string ptype, int fieldIndex, params string[] fieldValues)
    {
        throw new NotImplementedException("Use RemoveFilteredPolicyAsync for database operations");
    }

    public async Task RemoveFilteredPolicyAsync(string sec, string ptype, int fieldIndex, params string[] fieldValues)
    {
        var query = _context.CasbinPolicies.Where(p => p.PolicyType == ptype);

        for (int i = 0; i < fieldValues.Length; i++)
        {
            var index = fieldIndex + i;
            var value = fieldValues[i];

            if (string.IsNullOrWhiteSpace(value))
                continue;

            query = index switch
            {
                0 => query.Where(p => p.V0 == value),
                1 => query.Where(p => p.V1 == value),
                2 => query.Where(p => p.V2 == value),
                3 => query.Where(p => p.V3 == value),
                4 => query.Where(p => p.V4 == value),
                5 => query.Where(p => p.V5 == value),
                _ => query
            };
        }

        var policies = await query.ToListAsync();
        _context.CasbinPolicies.RemoveRange(policies);
        await _context.SaveChangesAsync();
    }
}
