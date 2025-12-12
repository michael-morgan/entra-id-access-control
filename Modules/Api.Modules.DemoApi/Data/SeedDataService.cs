using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.DemoApi.Data;

/// <summary>
/// Seeds POC data for Alice, Bob, and Carol with realistic scenarios.
/// </summary>
public class SeedDataService(AccessControlDbContext context)
{
    private readonly AccessControlDbContext _context = context;

    // ═══════════════════════════════════════════════════════════════════════════
    // ENTRA ID OBJECT IDS - REAL VALUES FROM TENANT
    // ═══════════════════════════════════════════════════════════════════════════

    // Users
    private const string ALICE_ID = "1c9a126e-98e7-42d8-8597-a59473bef64a";
    private const string BOB_ID = "e394d7e4-3574-4b2d-8678-2278f975caab";
    private const string CAROL_ID = "af6f8f52-0f91-490e-908c-f88c83ce1eba";

    // Platform Groups
    private const string PLATFORM_ADMINS = "e7d220ad-c492-4746-9cfd-a87688c78e82";

    // Loans Groups
    private const string LOANS_OFFICERS = "f17daf4a-2998-46f8-82d3-b049e0a8cd35";
    private const string LOANS_APPROVERS = "c029c26c-65de-46cf-9da0-f27240256465";
    private const string LOANS_SENIOR_APPROVERS = "045c925c-df54-41b7-8280-999dae20c742";
    private const string LOANS_DISBURSEMENT = "b1a534e2-5424-41aa-90bb-a722460d0a40";

    // Claims Groups
    private const string CLAIMS_ADJUDICATORS = "9bfef8ae-ab27-44ac-8cd6-b8f3765c4007";
    private const string CLAIMS_SENIOR_ADJUDICATORS = "3c5d884d-03c9-40c7-a75d-adeb66645c88";
    private const string CLAIMS_PAYMENT_PROCESSORS = "5c976bff-61aa-47f2-8d79-5c7efb9ba6b8";

    // Documents Groups
    private const string DOCUMENTS_VIEWERS = "3f5df870-bdce-4d85-864e-ec5e79ae4bba";
    private const string DOCUMENTS_UPLOADERS = "35504b82-f012-4b71-9b91-bb7180f15992";
    private const string DOCUMENTS_MANAGERS = "4002596b-8fae-4264-8cf0-d1d43890a56f";

    public async Task SeedAsync()
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine("  ACCESS CONTROL POC - SEEDING DATA FOR ALICE, BOB, AND CAROL");
        Console.WriteLine("=================================================================");

        await ClearExistingDataAsync();

        await SeedUsersAsync();
        await SeedGroupsAsync();
        await SeedUserGroupsAsync();
        await SeedResourcesAsync();
        await SeedRolesAsync();
        await SeedGroupRoleMappingsAsync();
        await SeedPoliciesAsync();
        await SeedUserAttributesAsync();
        await SeedRoleAttributesAsync();
        await SeedAttributeSchemasAsync();
        await SeedAbacRuleGroupsAsync();
        await SeedAbacRulesAsync();
        await SeedSampleDataAsync();

        await _context.SaveChangesAsync();

        Console.WriteLine("\n=================================================================");
        Console.WriteLine("  ✓ SEED DATA COMPLETED SUCCESSFULLY");
        Console.WriteLine("=================================================================\n");
    }

    private async Task ClearExistingDataAsync()
    {
        Console.WriteLine("\n[0/12] Clearing existing data...");

        // Delete in correct order to respect foreign key constraints
        // Start with dependent tables first

        var abacRulesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[AbacRules]");
        var abacRuleGroupsDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[AbacRuleGroups]");
        var attributeSchemasDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[AttributeSchemas]");
        var userAttributesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[UserAttributes]");
        var groupAttributesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[GroupAttributes]");
        var roleAttributesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[RoleAttributes]");
        var casbinPoliciesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[CasbinPolicies]");
        var casbinResourcesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[CasbinResources]");
        var casbinRolesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[CasbinRoles]");
        var userGroupsDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[UserGroups]");
        var groupsDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[Groups]");
        var usersDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM [auth].[Users]");

        Console.WriteLine($"  ✓ Deleted {abacRulesDeleted} ABAC rules");
        Console.WriteLine($"  ✓ Deleted {abacRuleGroupsDeleted} ABAC rule groups");
        Console.WriteLine($"  ✓ Deleted {attributeSchemasDeleted} attribute schemas");
        Console.WriteLine($"  ✓ Deleted {userAttributesDeleted} user attributes");
        Console.WriteLine($"  ✓ Deleted {groupAttributesDeleted} group attributes");
        Console.WriteLine($"  ✓ Deleted {roleAttributesDeleted} role attributes");
        Console.WriteLine($"  ✓ Deleted {casbinPoliciesDeleted} Casbin policies");
        Console.WriteLine($"  ✓ Deleted {casbinResourcesDeleted} Casbin resources");
        Console.WriteLine($"  ✓ Deleted {casbinRolesDeleted} Casbin roles");
        Console.WriteLine($"  ✓ Deleted {userGroupsDeleted} user-group associations");
        Console.WriteLine($"  ✓ Deleted {groupsDeleted} groups");
        Console.WriteLine($"  ✓ Deleted {usersDeleted} users");
        Console.WriteLine("  ✓ All existing data cleared");
    }

    private Task SeedUsersAsync()
    {
        Console.WriteLine("\n[1/12] Seeding users (global user records)...");

        var users = new List<User>
        {
            new() {
                UserId = ALICE_ID,
                Name = "Alice",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = BOB_ID,
                Name = "Bob",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = CAROL_ID,
                Name = "Carol",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var user in users)
        {
            _context.Users.Add(user);
            Console.WriteLine($"  ✓ {user.Name} ({user.UserId})");
        }

        return Task.CompletedTask;
    }

    private Task SeedGroupsAsync()
    {
        Console.WriteLine("\n[2/12] Seeding Entra ID groups...");

        var groups = new List<Group>
        {
            // Platform Groups
            new() {
                GroupId = PLATFORM_ADMINS,
                DisplayName = "Platform-Admins",
                Description = "Platform administrators with system-wide access",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },

            // Loans Groups
            new() {
                GroupId = LOANS_OFFICERS,
                DisplayName = "Loans-Officers",
                Description = "Loan officers who can create and manage loan applications",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = LOANS_APPROVERS,
                DisplayName = "Loans-Approvers",
                Description = "Loan approvers with standard approval limits",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = LOANS_SENIOR_APPROVERS,
                DisplayName = "Loans-SeniorApprovers",
                Description = "Senior loan approvers with high approval limits",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = LOANS_DISBURSEMENT,
                DisplayName = "Loans-Disbursement",
                Description = "Loan disbursement officers who process approved loans",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },

            // Claims Groups
            new() {
                GroupId = CLAIMS_ADJUDICATORS,
                DisplayName = "Claims-Adjudicators",
                Description = "Claims adjudicators who assess insurance claims",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = CLAIMS_SENIOR_ADJUDICATORS,
                DisplayName = "Claims-SeniorAdjudicators",
                Description = "Senior claims adjudicators for high-value claims",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = CLAIMS_PAYMENT_PROCESSORS,
                DisplayName = "Claims-PaymentProcessors",
                Description = "Payment processors who issue claim payments",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },

            // Documents Groups
            new() {
                GroupId = DOCUMENTS_VIEWERS,
                DisplayName = "Documents-Viewers",
                Description = "Document viewers with read-only access",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = DOCUMENTS_UPLOADERS,
                DisplayName = "Documents-Uploaders",
                Description = "Document uploaders who can add new documents",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                GroupId = DOCUMENTS_MANAGERS,
                DisplayName = "Documents-Managers",
                Description = "Document managers with full management permissions",
                Source = "Manual",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var group in groups)
        {
            _context.Groups.Add(group);
            Console.WriteLine($"  ✓ {group.DisplayName} ({group.GroupId[..8]}...)");
        }

        return Task.CompletedTask;
    }

    private Task SeedUserGroupsAsync()
    {
        Console.WriteLine("\n[3/12] Seeding user-group associations...");

        var userGroups = new List<UserGroup>
        {
            // Alice's Groups (Loans)
            new() {
                UserId = ALICE_ID,
                GroupId = LOANS_OFFICERS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = ALICE_ID,
                GroupId = LOANS_SENIOR_APPROVERS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            },

            // Bob's Groups (Loans + Claims)
            new() {
                UserId = BOB_ID,
                GroupId = LOANS_APPROVERS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = BOB_ID,
                GroupId = CLAIMS_ADJUDICATORS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            },

            // Carol's Groups (Loans + Documents)
            new() {
                UserId = CAROL_ID,
                GroupId = LOANS_OFFICERS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = CAROL_ID,
                GroupId = DOCUMENTS_VIEWERS,
                Source = "Manual",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var userGroup in userGroups)
        {
            _context.UserGroups.Add(userGroup);
            var userName = userGroup.UserId == ALICE_ID ? "Alice" : userGroup.UserId == BOB_ID ? "Bob" : "Carol";
            var group = _context.Groups.Local.FirstOrDefault(g => g.GroupId == userGroup.GroupId);
            Console.WriteLine($"  ✓ {userName} → {group?.DisplayName ?? userGroup.GroupId[..8] + "..."}");
        }

        return Task.CompletedTask;
    }

    private Task SeedResourcesAsync()
    {
        Console.WriteLine("\n[4/12] Seeding resource definitions...");

        var resources = new[]
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // LOANS WORKSTREAM RESOURCES
            // ═══════════════════════════════════════════════════════════════════════════

            // Loan Applications
            new CasbinResource
            {
                ResourcePattern = "Loan",
                WorkstreamId = "loans",
                DisplayName = "Loan Applications",
                Description = "Root resource for all loan application operations",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Loan/Application",
                WorkstreamId = "loans",
                DisplayName = "Loan Application",
                Description = "Individual loan application management",
                ParentResource = "Loan",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Loan/Documents",
                WorkstreamId = "loans",
                DisplayName = "Loan Documents",
                Description = "Documents associated with loan applications",
                ParentResource = "Loan",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Loan/Approval",
                WorkstreamId = "loans",
                DisplayName = "Loan Approval",
                Description = "Loan approval and rejection actions",
                ParentResource = "Loan",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Loan/Disbursement",
                WorkstreamId = "loans",
                DisplayName = "Loan Disbursement",
                Description = "Loan disbursement and payment operations",
                ParentResource = "Loan",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Loan/*",
                WorkstreamId = "loans",
                DisplayName = "All Loan Resources",
                Description = "Wildcard match for all loan-related resources",
                ParentResource = "Loan",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // CLAIMS WORKSTREAM RESOURCES
            // ═══════════════════════════════════════════════════════════════════════════

            // Claims
            new CasbinResource
            {
                ResourcePattern = "Claim",
                WorkstreamId = "claims",
                DisplayName = "Insurance Claims",
                Description = "Root resource for all insurance claim operations",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Claim/Application",
                WorkstreamId = "claims",
                DisplayName = "Claim Application",
                Description = "Individual claim submission and management",
                ParentResource = "Claim",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Claim/Adjudication",
                WorkstreamId = "claims",
                DisplayName = "Claim Adjudication",
                Description = "Claim assessment and adjudication actions",
                ParentResource = "Claim",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Claim/Payment",
                WorkstreamId = "claims",
                DisplayName = "Claim Payment",
                Description = "Claim payment processing operations",
                ParentResource = "Claim",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Claim/*",
                WorkstreamId = "claims",
                DisplayName = "All Claim Resources",
                Description = "Wildcard match for all claim-related resources",
                ParentResource = "Claim",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // DOCUMENTS WORKSTREAM RESOURCES
            // ═══════════════════════════════════════════════════════════════════════════

            // Documents
            new CasbinResource
            {
                ResourcePattern = "Document",
                WorkstreamId = "documents",
                DisplayName = "Documents",
                Description = "Root resource for document management",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Document/Upload",
                WorkstreamId = "documents",
                DisplayName = "Document Upload",
                Description = "Document upload operations",
                ParentResource = "Document",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Document/View",
                WorkstreamId = "documents",
                DisplayName = "Document View",
                Description = "Document viewing operations",
                ParentResource = "Document",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Document/Manage",
                WorkstreamId = "documents",
                DisplayName = "Document Management",
                Description = "Document management operations (edit, delete, organize)",
                ParentResource = "Document",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Document/*",
                WorkstreamId = "documents",
                DisplayName = "All Document Resources",
                Description = "Wildcard match for all document-related resources",
                ParentResource = "Document",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // REPORTS (SHARED ACROSS WORKSTREAMS)
            // ═══════════════════════════════════════════════════════════════════════════

            new CasbinResource
            {
                ResourcePattern = "Report",
                WorkstreamId = "*",
                DisplayName = "Reports",
                Description = "Root resource for reporting (shared across workstreams)",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Report/Financial",
                WorkstreamId = "*",
                DisplayName = "Financial Reports",
                Description = "Financial reporting and analytics",
                ParentResource = "Report",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Report/Compliance",
                WorkstreamId = "*",
                DisplayName = "Compliance Reports",
                Description = "Compliance and regulatory reports",
                ParentResource = "Report",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            },
            new CasbinResource
            {
                ResourcePattern = "Report/Operational",
                WorkstreamId = "*",
                DisplayName = "Operational Reports",
                Description = "Operational and performance reports",
                ParentResource = "Report",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            }
        };

        _context.CasbinResources.AddRange(resources);

        Console.WriteLine($"  ✓ Seeded {resources.Length} resource definitions");
        Console.WriteLine($"    - Loans: 6 resources (Loan, Application, Documents, Approval, Disbursement, Loan/*)");
        Console.WriteLine($"    - Claims: 5 resources (Claim, Application, Adjudication, Payment, Claim/*)");
        Console.WriteLine($"    - Documents: 5 resources (Document, Upload, View, Manage, Document/*)");
        Console.WriteLine($"    - Reports: 4 global resources (Report, Financial, Compliance, Operational)");

        return Task.CompletedTask;
    }

    private Task SeedRolesAsync()
    {
        Console.WriteLine("\n[5/12] Seeding application roles...");

        var roles = new[]
        {
            // Loans workstream roles
            new CasbinRole
            {
                RoleName = "Loans.Officer",
                WorkstreamId = "loans",
                DisplayName = "Loan Officer",
                Description = "Can create and view loan applications"
            },
            new CasbinRole
            {
                RoleName = "Loans.Approver",
                WorkstreamId = "loans",
                DisplayName = "Loan Approver",
                Description = "Can approve/reject loans based on ABAC rules (approval limit, region)"
            },
            new CasbinRole
            {
                RoleName = "Loans.SeniorApprover",
                WorkstreamId = "loans",
                DisplayName = "Senior Loan Approver",
                Description = "Can approve high-value loans (inherits from Approver)"
            },
            new CasbinRole
            {
                RoleName = "Loans.Disbursement",
                WorkstreamId = "loans",
                DisplayName = "Loan Disbursement Officer",
                Description = "Can disburse approved loans"
            },

            // Claims workstream roles
            new CasbinRole
            {
                RoleName = "Claims.Adjudicator",
                WorkstreamId = "claims",
                DisplayName = "Claims Adjudicator",
                Description = "Can adjudicate insurance claims"
            },
            new CasbinRole
            {
                RoleName = "Claims.SeniorAdjudicator",
                WorkstreamId = "claims",
                DisplayName = "Senior Claims Adjudicator",
                Description = "Can adjudicate high-value claims"
            },
            new CasbinRole
            {
                RoleName = "Claims.PaymentProcessor",
                WorkstreamId = "claims",
                DisplayName = "Claims Payment Processor",
                Description = "Can issue claim payments"
            },

            // Documents workstream roles
            new CasbinRole
            {
                RoleName = "Documents.Viewer",
                WorkstreamId = "documents",
                DisplayName = "Document Viewer",
                Description = "Can view documents in their department"
            },
            new CasbinRole
            {
                RoleName = "Documents.Uploader",
                WorkstreamId = "documents",
                DisplayName = "Document Uploader",
                Description = "Can upload and view documents"
            },
            new CasbinRole
            {
                RoleName = "Documents.Manager",
                WorkstreamId = "documents",
                DisplayName = "Document Manager",
                Description = "Can manage documents including confidential ones"
            }
        };

        foreach (var role in roles)
        {
            _context.CasbinRoles.Add(role);
            Console.WriteLine($"  ✓ {role.RoleName} ({role.WorkstreamId})");
        }

        return Task.CompletedTask;
    }

    private Task SeedGroupRoleMappingsAsync()
    {
        Console.WriteLine("\n[6/12] Mapping Entra ID groups to application roles...");

        var groupMappings = new[]
        {
            // Loans Groups → Roles
            new CasbinPolicy { PolicyType = "g", V0 = LOANS_OFFICERS, V1 = "Loans.Officer", V2 = "loans", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "g", V0 = LOANS_APPROVERS, V1 = "Loans.Approver", V2 = "loans", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "g", V0 = LOANS_SENIOR_APPROVERS, V1 = "Loans.SeniorApprover", V2 = "loans", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "g", V0 = LOANS_DISBURSEMENT, V1 = "Loans.Disbursement", V2 = "loans", WorkstreamId = "loans" },

            // Claims Groups → Roles
            new CasbinPolicy { PolicyType = "g", V0 = CLAIMS_ADJUDICATORS, V1 = "Claims.Adjudicator", V2 = "claims", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "g", V0 = CLAIMS_SENIOR_ADJUDICATORS, V1 = "Claims.SeniorAdjudicator", V2 = "claims", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "g", V0 = CLAIMS_PAYMENT_PROCESSORS, V1 = "Claims.PaymentProcessor", V2 = "claims", WorkstreamId = "claims" },

            // Documents Groups → Roles
            new CasbinPolicy { PolicyType = "g", V0 = DOCUMENTS_VIEWERS, V1 = "Documents.Viewer", V2 = "documents", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "g", V0 = DOCUMENTS_UPLOADERS, V1 = "Documents.Uploader", V2 = "documents", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "g", V0 = DOCUMENTS_MANAGERS, V1 = "Documents.Manager", V2 = "documents", WorkstreamId = "documents" },

            // Role inheritance
            new CasbinPolicy { PolicyType = "g", V0 = "Loans.SeniorApprover", V1 = "Loans.Approver", V2 = "loans", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "g", V0 = "Claims.SeniorAdjudicator", V1 = "Claims.Adjudicator", V2 = "claims", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "g", V0 = "Documents.Uploader", V1 = "Documents.Viewer", V2 = "documents", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "g", V0 = "Documents.Manager", V1 = "Documents.Uploader", V2 = "documents", WorkstreamId = "documents" }
        };

        foreach (var mapping in groupMappings)
        {
            _context.CasbinPolicies.Add(mapping);
            if (mapping.V0.Length == 36) // Group ID
            {
                Console.WriteLine($"  ✓ Group {mapping.V0[..8]}... → {mapping.V1}");
            }
            else // Role inheritance
            {
                Console.WriteLine($"  ✓ {mapping.V0} inherits from {mapping.V1}");
            }
        }

        return Task.CompletedTask;
    }

    private Task SeedPoliciesAsync()
    {
        Console.WriteLine("\n[7/12] Seeding authorization policies...");

        var policies = new List<CasbinPolicy>();

        // ═══════════════════════════════════════════════════════════════════════════
        // LOANS WORKSTREAM POLICIES
        // ═══════════════════════════════════════════════════════════════════════════

        Console.WriteLine("  Loans policies:");
        policies.AddRange(new[]
        {
            // Loan Officers: create, list, read
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Officer", V1 = "loans", V2 = "Loan", V3 = "create", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Officer", V1 = "loans", V2 = "Loan", V3 = "list", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Officer", V1 = "loans", V2 = "Loan/*", V3 = "read", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Officer", V1 = "loans", V2 = "Loan/*", V3 = "write", V4 = "allow", WorkstreamId = "loans" },

            // Loan Approvers: read, approve, reject (ABAC checks approval limit)
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Approver", V1 = "loans", V2 = "Loan", V3 = "list", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Approver", V1 = "loans", V2 = "Loan/*", V3 = "read", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Approver", V1 = "loans", V2 = "Loan/*", V3 = "approve", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Approver", V1 = "loans", V2 = "Loan/*", V3 = "reject", V4 = "allow", WorkstreamId = "loans" },

            // Disbursement: read, disburse
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Disbursement", V1 = "loans", V2 = "Loan/*", V3 = "read", V4 = "allow", WorkstreamId = "loans" },
            new CasbinPolicy { PolicyType = "p", V0 = "Loans.Disbursement", V1 = "loans", V2 = "Loan/*", V3 = "disburse", V4 = "allow", WorkstreamId = "loans" }
        });

        // ═══════════════════════════════════════════════════════════════════════════
        // CLAIMS WORKSTREAM POLICIES
        // ═══════════════════════════════════════════════════════════════════════════

        Console.WriteLine("  Claims policies:");
        policies.AddRange(new[]
        {
            // Claims Adjudicators: create, list, read, adjudicate
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.Adjudicator", V1 = "claims", V2 = "Claim", V3 = "create", V4 = "allow", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.Adjudicator", V1 = "claims", V2 = "Claim", V3 = "list", V4 = "allow", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.Adjudicator", V1 = "claims", V2 = "Claim/*", V3 = "read", V4 = "allow", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.Adjudicator", V1 = "claims", V2 = "Claim/*", V3 = "adjudicate", V4 = "allow", WorkstreamId = "claims" },

            // Payment Processors: read, pay
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.PaymentProcessor", V1 = "claims", V2 = "Claim/*", V3 = "read", V4 = "allow", WorkstreamId = "claims" },
            new CasbinPolicy { PolicyType = "p", V0 = "Claims.PaymentProcessor", V1 = "claims", V2 = "Claim/*", V3 = "pay", V4 = "allow", WorkstreamId = "claims" }
        });

        // ═══════════════════════════════════════════════════════════════════════════
        // DOCUMENTS WORKSTREAM POLICIES
        // ═══════════════════════════════════════════════════════════════════════════

        Console.WriteLine("  Documents policies:");
        policies.AddRange(new[]
        {
            // Document Viewers: list, read
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Viewer", V1 = "documents", V2 = "Document", V3 = "list", V4 = "allow", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Viewer", V1 = "documents", V2 = "Document/*", V3 = "read", V4 = "allow", WorkstreamId = "documents" },

            // Document Uploaders: upload
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Uploader", V1 = "documents", V2 = "Document", V3 = "upload", V4 = "allow", WorkstreamId = "documents" },

            // Document Managers: update, delete, download
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Manager", V1 = "documents", V2 = "Document/*", V3 = "update", V4 = "allow", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Manager", V1 = "documents", V2 = "Document/*", V3 = "delete", V4 = "allow", WorkstreamId = "documents" },
            new CasbinPolicy { PolicyType = "p", V0 = "Documents.Manager", V1 = "documents", V2 = "Document/*", V3 = "download", V4 = "allow", WorkstreamId = "documents" }
        });

        _context.CasbinPolicies.AddRange(policies);

        Console.WriteLine($"  ✓ Added {policies.Count} authorization policies");

        return Task.CompletedTask;
    }

    private Task SeedUserAttributesAsync()
    {
        Console.WriteLine("\n[8/12] Seeding user attributes (workstream-scoped + global attributes)...");

        // Helper to create attributes JSON for workstream-scoped attributes
        static string createAttrs(string dept, string region, decimal approvalLimit, int mgmtLevel, string? costCenter = null)
        {
            var attrs = new Dictionary<string, object>
            {
                { "Department", dept },
                { "Region", region },
                { "ApprovalLimit", approvalLimit },
                { "ManagementLevel", mgmtLevel }
            };
            if (costCenter != null) attrs["CostCenter"] = costCenter;
            return System.Text.Json.JsonSerializer.Serialize(attrs);
        }

        // Helper to create global attributes JSON (JobTitle and Department)
        static string createGlobalAttrs(string jobTitle, string department)
        {
            var attrs = new Dictionary<string, object>
            {
                { "JobTitle", jobTitle },
                { "Department", department }
            };
            return System.Text.Json.JsonSerializer.Serialize(attrs);
        }

        var userAttributes = new List<UserAttribute>
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // GLOBAL ATTRIBUTES (Not workstream-scoped, used for display across all workstreams)
            // ═══════════════════════════════════════════════════════════════════════════
            new() {
                UserId = ALICE_ID,
                WorkstreamId = "global",
                AttributesJson = createGlobalAttrs("Senior Loan Officer", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = BOB_ID,
                WorkstreamId = "global",
                AttributesJson = createGlobalAttrs("Loan Approver", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = CAROL_ID,
                WorkstreamId = "global",
                AttributesJson = createGlobalAttrs("Junior Loan Officer", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // ALICE - Senior Loan Officer in US-WEST with high approval limit
            // ═══════════════════════════════════════════════════════════════════════════
            new() {
                UserId = ALICE_ID,
                WorkstreamId = "loans",
                AttributesJson = createAttrs("Lending", "US-WEST", 200000m, 3, "CC-WEST-001"),
                CreatedAt = DateTimeOffset.UtcNow
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // BOB - Loan Approver (US-EAST) + Claims Adjudicator (US-EAST)
            // ═══════════════════════════════════════════════════════════════════════════
            new() {
                UserId = BOB_ID,
                WorkstreamId = "loans",
                AttributesJson = createAttrs("Lending", "US-EAST", 75000m, 2, "CC-EAST-002"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = BOB_ID,
                WorkstreamId = "claims",
                AttributesJson = createAttrs("Claims", "US-EAST", 50000m, 2, "CC-EAST-003"),
                CreatedAt = DateTimeOffset.UtcNow
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // CAROL - Junior Loan Officer (US-WEST) + Documents Viewer (Legal dept)
            // ═══════════════════════════════════════════════════════════════════════════
            new() {
                UserId = CAROL_ID,
                WorkstreamId = "loans",
                AttributesJson = createAttrs("Lending", "US-WEST", 0m, 1, "CC-WEST-004"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                UserId = CAROL_ID,
                WorkstreamId = "documents",
                AttributesJson = createAttrs("Legal", "US-WEST", 0m, 1, "CC-WEST-005"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var attr in userAttributes)
        {
            _context.UserAttributes.Add(attr);
            var userId = attr.UserId == ALICE_ID ? "Alice" : attr.UserId == BOB_ID ? "Bob" : "Carol";
            Console.WriteLine($"  ✓ {userId} → {attr.WorkstreamId} workstream");
        }

        return Task.CompletedTask;
    }

    private Task SeedRoleAttributesAsync()
    {
        Console.WriteLine("\n[9/12] Seeding role attributes (global role attributes for fallback)...");

        // Helper to create global role attributes JSON (JobTitle and Department)
        static string createGlobalRoleAttrs(string jobTitle, string department)
        {
            var attrs = new Dictionary<string, object>
            {
                { "JobTitle", jobTitle },
                { "Department", department }
            };
            return System.Text.Json.JsonSerializer.Serialize(attrs);
        }

        var roleAttributes = new List<RoleAttribute>
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // GLOBAL ROLE ATTRIBUTES (Fallback when user-specific attributes not available)
            // These are used when a user doesn't have explicit JobTitle/Department attributes
            // ═══════════════════════════════════════════════════════════════════════════
            new() {
                AppRoleId = "loans-officer-role",
                RoleValue = "Loans.Officer",
                WorkstreamId = "global",
                RoleDisplayName = "Loan Officer",
                IsActive = true,
                AttributesJson = createGlobalRoleAttrs("Loan Officer", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                AppRoleId = "loans-approver-role",
                RoleValue = "Loans.Approver",
                WorkstreamId = "global",
                RoleDisplayName = "Loan Approver",
                IsActive = true,
                AttributesJson = createGlobalRoleAttrs("Loan Approver", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                AppRoleId = "loans-senior-approver-role",
                RoleValue = "Loans.SeniorApprover",
                WorkstreamId = "global",
                RoleDisplayName = "Senior Loan Approver",
                IsActive = true,
                AttributesJson = createGlobalRoleAttrs("Senior Loan Approver", "Lending"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                AppRoleId = "claims-adjudicator-role",
                RoleValue = "Claims.Adjudicator",
                WorkstreamId = "global",
                RoleDisplayName = "Claims Adjudicator",
                IsActive = true,
                AttributesJson = createGlobalRoleAttrs("Claims Adjudicator", "Claims"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new() {
                AppRoleId = "documents-viewer-role",
                RoleValue = "Documents.Viewer",
                WorkstreamId = "global",
                RoleDisplayName = "Documents Viewer",
                IsActive = true,
                AttributesJson = createGlobalRoleAttrs("Documents Viewer", "Documents"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var attr in roleAttributes)
        {
            _context.RoleAttributes.Add(attr);
            Console.WriteLine($"  ✓ {attr.RoleValue} → {attr.WorkstreamId} workstream");
        }

        return Task.CompletedTask;
    }

    private Task SeedAttributeSchemasAsync()
    {
        Console.WriteLine("\n[10/12] Seeding attribute schemas (defines what attributes each workstream uses)...");

        var schemas = new[]
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // GLOBAL WORKSTREAM SCHEMAS (shared across all workstreams)
            // ═══════════════════════════════════════════════════════════════════════════

            // Global User Attributes
            new AttributeSchema
            {
                WorkstreamId = "global",
                AttributeLevel = "User",
                AttributeName = "JobTitle",
                AttributeDisplayName = "Job Title",
                DataType = "String",
                IsRequired = false,
                Description = "User's job title for display purposes",
                DisplayOrder = 1
            },
            new AttributeSchema
            {
                WorkstreamId = "global",
                AttributeLevel = "User",
                AttributeName = "Department",
                AttributeDisplayName = "Department",
                DataType = "String",
                IsRequired = false,
                Description = "User's primary department",
                ValidationRules = "{\"allowedValues\": [\"Lending\",\"Claims\",\"Documents\",\"Legal\",\"HR\",\"Finance\",\"IT\"]}",
                DisplayOrder = 2
            },

            // Global Role Attributes
            new AttributeSchema
            {
                WorkstreamId = "global",
                AttributeLevel = "Role",
                AttributeName = "JobTitle",
                AttributeDisplayName = "Role Job Title",
                DataType = "String",
                IsRequired = false,
                Description = "Default job title for users with this role",
                DisplayOrder = 1
            },
            new AttributeSchema
            {
                WorkstreamId = "global",
                AttributeLevel = "Role",
                AttributeName = "Department",
                AttributeDisplayName = "Role Department",
                DataType = "String",
                IsRequired = false,
                Description = "Department associated with this role",
                ValidationRules = "{\"allowedValues\": [\"Lending\",\"Claims\",\"Documents\",\"Legal\",\"HR\",\"Finance\",\"IT\"]}",
                DisplayOrder = 2
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // LOANS WORKSTREAM SCHEMAS
            // ═══════════════════════════════════════════════════════════════════════════

            new AttributeSchema
            {
                WorkstreamId = "loans",
                AttributeLevel = "User",
                AttributeName = "Region",
                AttributeDisplayName = "Geographic Region",
                DataType = "String",
                IsRequired = true,
                Description = "Geographic region for regional access control",
                ValidationRules = "{\"allowedValues\": [\"US-WEST\",\"US-EAST\",\"US-CENTRAL\",\"ALL\"]}",
                DisplayOrder = 1
            },
            new AttributeSchema
            {
                WorkstreamId = "loans",
                AttributeLevel = "User",
                AttributeName = "ApprovalLimit",
                AttributeDisplayName = "Maximum Approval Limit",
                DataType = "Number",
                IsRequired = true,
                Description = "Maximum loan amount user can approve",
                ValidationRules = "{\"min\": 0, \"max\": 10000000}",
                DisplayOrder = 2
            },
            new AttributeSchema
            {
                WorkstreamId = "loans",
                AttributeLevel = "User",
                AttributeName = "Department",
                AttributeDisplayName = "Department Name",
                DataType = "String",
                IsRequired = true,
                Description = "Department name",
                ValidationRules = "{\"allowedValues\": [\"Lending\",\"Underwriting\",\"IT\"]}",
                DisplayOrder = 3
            },
            new AttributeSchema
            {
                WorkstreamId = "loans",
                AttributeLevel = "User",
                AttributeName = "ManagementLevel",
                AttributeDisplayName = "Management Level",
                DataType = "Number",
                IsRequired = false,
                Description = "Management level (1-5) for hierarchical approval workflows",
                ValidationRules = "{\"min\": 1, \"max\": 5}",
                DisplayOrder = 4
            },
            new AttributeSchema
            {
                WorkstreamId = "loans",
                AttributeLevel = "User",
                AttributeName = "CostCenter",
                AttributeDisplayName = "Cost Center Code",
                DataType = "String",
                IsRequired = false,
                Description = "Cost center for financial tracking",
                ValidationRules = "{\"pattern\": \"^CC-[A-Z]+-\\\\d{3}$\"}",
                DisplayOrder = 5
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // CLAIMS WORKSTREAM SCHEMAS
            // ═══════════════════════════════════════════════════════════════════════════
            new AttributeSchema
            {
                WorkstreamId = "claims",
                AttributeLevel = "User",
                AttributeName = "Region",
                AttributeDisplayName = "Geographic Region",
                DataType = "String",
                IsRequired = true,
                Description = "Geographic region for regional access control",
                DisplayOrder = 1
            },
            new AttributeSchema
            {
                WorkstreamId = "claims",
                AttributeLevel = "User",
                AttributeName = "ManagementLevel",
                AttributeDisplayName = "Management Level",
                DataType = "Number",
                IsRequired = true,
                Description = "Management level (1-5), required for high-value claims",
                ValidationRules = "{\"min\": 1, \"max\": 5}",
                DisplayOrder = 2
            },
            new AttributeSchema
            {
                WorkstreamId = "claims",
                AttributeLevel = "User",
                AttributeName = "Department",
                AttributeDisplayName = "Department Name",
                DataType = "String",
                IsRequired = false,
                Description = "User's department",
                ValidationRules = "{\"allowedValues\": [\"Claims\",\"Legal\",\"Finance\"]}",
                DisplayOrder = 3
            },
            new AttributeSchema
            {
                WorkstreamId = "claims",
                AttributeLevel = "User",
                AttributeName = "ApprovalLimit",
                AttributeDisplayName = "Maximum Claim Approval Limit",
                DataType = "Number",
                IsRequired = false,
                Description = "Maximum claim amount user can approve",
                ValidationRules = "{\"min\": 0, \"max\": 1000000}",
                DisplayOrder = 4
            },
            new AttributeSchema
            {
                WorkstreamId = "claims",
                AttributeLevel = "User",
                AttributeName = "CostCenter",
                AttributeDisplayName = "Cost Center Code",
                DataType = "String",
                IsRequired = false,
                Description = "Cost center for financial tracking",
                ValidationRules = "{\"pattern\": \"^CC-[A-Z]+-\\\\d{3}$\"}",
                DisplayOrder = 5
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // DOCUMENTS WORKSTREAM SCHEMAS
            // ═══════════════════════════════════════════════════════════════════════════
            new AttributeSchema
            {
                WorkstreamId = "documents",
                AttributeLevel = "User",
                AttributeName = "Department",
                AttributeDisplayName = "Department Name",
                DataType = "String",
                IsRequired = true,
                Description = "Department for document access control",
                ValidationRules = "{\"allowedValues\": [\"Legal\",\"HR\",\"Finance\",\"IT\"]}",
                DisplayOrder = 1
            },
            new AttributeSchema
            {
                WorkstreamId = "documents",
                AttributeLevel = "User",
                AttributeName = "Region",
                AttributeDisplayName = "Geographic Region",
                DataType = "String",
                IsRequired = false,
                Description = "Geographic region for regional access control",
                ValidationRules = "{\"allowedValues\": [\"US-WEST\",\"US-EAST\",\"US-CENTRAL\",\"ALL\"]}",
                DisplayOrder = 2
            },
            new AttributeSchema
            {
                WorkstreamId = "documents",
                AttributeLevel = "User",
                AttributeName = "ApprovalLimit",
                AttributeDisplayName = "Approval Limit",
                DataType = "Number",
                IsRequired = false,
                Description = "Approval limit for document-related actions",
                ValidationRules = "{\"min\": 0}",
                DisplayOrder = 3
            },
            new AttributeSchema
            {
                WorkstreamId = "documents",
                AttributeLevel = "User",
                AttributeName = "ManagementLevel",
                AttributeDisplayName = "Management Level",
                DataType = "Number",
                IsRequired = false,
                Description = "Management level (1-5)",
                ValidationRules = "{\"min\": 1, \"max\": 5}",
                DisplayOrder = 4
            },
            new AttributeSchema
            {
                WorkstreamId = "documents",
                AttributeLevel = "User",
                AttributeName = "CostCenter",
                AttributeDisplayName = "Cost Center Code",
                DataType = "String",
                IsRequired = false,
                Description = "Cost center for financial tracking",
                ValidationRules = "{\"pattern\": \"^CC-[A-Z]+-\\\\d{3}$\"}",
                DisplayOrder = 5
            }
        };

        _context.AttributeSchemas.AddRange(schemas);

        Console.WriteLine($"  ✓ Defined attribute schemas for global + 3 workstreams (User and Role levels)");

        return Task.CompletedTask;
    }

    private async Task SeedAbacRuleGroupsAsync()
    {
        Console.WriteLine("\n[11/12] Seeding ABAC rule groups (hierarchical rule organization)...");

        var ruleGroups = new[]
        {
            new AbacRuleGroup
            {
                WorkstreamId = "loans",
                GroupName = "LoanReadAccessChecks",
                Description = "All checks required for reading loan data",
                LogicalOperator = "AND",
                Resource = "Loan/*",
                Action = "read",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new AbacRuleGroup
            {
                WorkstreamId = "loans",
                GroupName = "LoanWriteAccessChecks",
                Description = "All checks required for writing loan data",
                LogicalOperator = "AND",
                Resource = "Loan/*",
                Action = "write",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new AbacRuleGroup
            {
                WorkstreamId = "loans",
                GroupName = "LoanApprovalChecks",
                Description = "All checks required for loan approval",
                LogicalOperator = "AND",
                Resource = "Loan/*",
                Action = "approve",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new AbacRuleGroup
            {
                WorkstreamId = "claims",
                GroupName = "ClaimsProcessingChecks",
                Description = "All checks for claims processing",
                LogicalOperator = "AND",
                Resource = "Claim/*",
                Action = "adjudicate",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new AbacRuleGroup
            {
                WorkstreamId = "documents",
                GroupName = "DocumentAccessChecks",
                Description = "Access control for documents",
                LogicalOperator = "AND",
                Resource = "Document/*",
                Action = "read",
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _context.AbacRuleGroups.AddRange(ruleGroups);
        await _context.SaveChangesAsync(); // Save to get IDs for linking rules

        foreach (var group in ruleGroups)
        {
            Console.WriteLine($"  ✓ {group.GroupName} ({group.WorkstreamId}) - Resource: {group.Resource}, Action: {group.Action}");
        }
    }

    private Task SeedAbacRulesAsync()
    {
        Console.WriteLine("\n[12/12] Seeding declarative ABAC rules...");

        // Get the rule group IDs that were just created
        var loanReadGroup = _context.AbacRuleGroups.First(g => g.GroupName == "LoanReadAccessChecks");
        var loanWriteGroup = _context.AbacRuleGroups.First(g => g.GroupName == "LoanWriteAccessChecks");
        var loanApprovalGroup = _context.AbacRuleGroups.First(g => g.GroupName == "LoanApprovalChecks");
        var claimsGroup = _context.AbacRuleGroups.First(g => g.GroupName == "ClaimsProcessingChecks");
        var documentsGroup = _context.AbacRuleGroups.First(g => g.GroupName == "DocumentAccessChecks");

        var rules = new[]
        {
            // Loans: Regional access control for READ operations
            new AbacRule
            {
                RuleName = "LoanRegionalAccessRead",
                WorkstreamId = "loans",
                RuleGroupId = loanReadGroup.Id,
                RuleType = "PropertyMatch",
                Configuration = "{\"userAttribute\":\"Region\",\"operator\":\"==\",\"resourceProperty\":\"Region\",\"allowWildcard\":\"ALL\"}",
                Priority = 100,
                IsActive = true,
                FailureMessage = "Regional access denied. Users can only access loans in their assigned region."
            },

            // Loans: Regional access control for WRITE operations
            new AbacRule
            {
                RuleName = "LoanRegionalAccessWrite",
                WorkstreamId = "loans",
                RuleGroupId = loanWriteGroup.Id,
                RuleType = "PropertyMatch",
                Configuration = "{\"userAttribute\":\"Region\",\"operator\":\"==\",\"resourceProperty\":\"Region\",\"allowWildcard\":\"ALL\"}",
                Priority = 100,
                IsActive = true,
                FailureMessage = "Regional access denied. Users can only modify loans in their assigned region."
            },

            // Loans: Approval limit check for APPROVAL operations
            new AbacRule
            {
                RuleName = "LoanApprovalLimitCheck",
                WorkstreamId = "loans",
                RuleGroupId = loanApprovalGroup.Id,
                RuleType = "AttributeComparison",
                Configuration = "{\"leftAttribute\":\"ApprovalLimit\",\"operator\":\"greaterThanOrEqual\",\"rightProperty\":\"RequestedAmount\"}",
                Priority = 100,
                IsActive = true,
                FailureMessage = "Approval limit exceeded. User can approve up to the amount specified in their ApprovalLimit attribute."
            },

            // Loans: Regional access control for APPROVAL operations
            new AbacRule
            {
                RuleName = "LoanRegionalAccessApproval",
                WorkstreamId = "loans",
                RuleGroupId = loanApprovalGroup.Id,
                RuleType = "PropertyMatch",
                Configuration = "{\"userAttribute\":\"Region\",\"operator\":\"==\",\"resourceProperty\":\"Region\",\"allowWildcard\":\"ALL\"}",
                Priority = 90,
                IsActive = true,
                FailureMessage = "Regional access denied. Users can only approve loans in their assigned region."
            },

            // Claims: High-value claims require management level 2+
            new AbacRule
            {
                RuleName = "HighValueClaimManagementLevel",
                WorkstreamId = "claims",
                RuleGroupId = claimsGroup.Id,
                RuleType = "ValueRange",
                Configuration = "{\"resourceProperty\":\"Amount\",\"threshold\":50000,\"requiredAttribute\":\"ManagementLevel\",\"minValue\":2}",
                Priority = 100,
                IsActive = true,
                FailureMessage = "High-value claims (>$50K) require management level 2 or higher."
            },

            // Documents: Confidential documents only during business hours
            new AbacRule
            {
                RuleName = "ConfidentialDocumentsBusinessHours",
                WorkstreamId = "documents",
                RuleGroupId = documentsGroup.Id,
                RuleType = "TimeRestriction",
                Configuration = "{\"resourceClassification\":\"Confidential\",\"allowedHours\":{\"start\":8,\"end\":18},\"timezone\":\"UTC\"}",
                Priority = 100,
                IsActive = true,
                FailureMessage = "Confidential documents can only be accessed during business hours (8 AM - 6 PM)."
            }
        };

        _context.AbacRules.AddRange(rules);

        foreach (var rule in rules)
        {
            Console.WriteLine($"  ✓ {rule.RuleName} ({rule.WorkstreamId}) - Linked to group {rule.RuleGroupId}");
        }

        return Task.CompletedTask;
    }

    private static Task SeedSampleDataAsync()
    {
        Console.WriteLine("\n[13/12] Displaying POC scenarios (no additional seeding)...");

        // NOTE: This would normally seed actual Loan, Claim, Document entities
        // For now, we're just documenting the scenarios here.

        Console.WriteLine("\n  POC Scenarios:");
        Console.WriteLine("  ══════════════════════════════════════════════════════════════");
        Console.WriteLine("\n  ALICE (Senior Loan Officer, US-WEST, $200K approval limit)");
        Console.WriteLine("  ✓ Can create loans in US-WEST");
        Console.WriteLine("  ✓ Can approve loans up to $200K in US-WEST");
        Console.WriteLine("  ✗ CANNOT approve loans in US-EAST (regional restriction)");
        Console.WriteLine("  ✗ CANNOT approve loans > $200K (approval limit)");

        Console.WriteLine("\n  BOB (Loan Approver US-EAST $75K + Claims Adjudicator US-EAST)");
        Console.WriteLine("  ✓ Can approve loans up to $75K in US-EAST");
        Console.WriteLine("  ✗ CANNOT approve loans in US-WEST (regional restriction)");
        Console.WriteLine("  ✗ CANNOT approve loans > $75K (approval limit)");
        Console.WriteLine("  ✓ Can adjudicate claims in US-EAST");
        Console.WriteLine("  ✗ CANNOT adjudicate claims > $50K (requires mgmt level 2, Bob is level 2 but high-value needs approval)");

        Console.WriteLine("\n  CAROL (Junior Loan Officer US-WEST + Documents Viewer Legal)");
        Console.WriteLine("  ✓ Can create loans in US-WEST");
        Console.WriteLine("  ✗ CANNOT approve ANY loans (approval limit = $0)");
        Console.WriteLine("  ✓ Can view documents in Legal department");
        Console.WriteLine("  ✗ CANNOT view confidential docs outside business hours");

        Console.WriteLine("\n  Cross-Workstream Scenarios:");
        Console.WriteLine("  • Alice has NO access to claims/documents workstreams");
        Console.WriteLine("  • Bob has DIFFERENT regions per workstream (loans=US-EAST, claims=US-EAST)");
        Console.WriteLine("  • Carol demonstrates multi-workstream user with different roles");

        Console.WriteLine("\n  ABAC Rules in Effect:");
        Console.WriteLine("  • Approval limits (declarative rule + code-based evaluator)");
        Console.WriteLine("  • Regional restrictions (code-based evaluator)");
        Console.WriteLine("  • Time-based access for confidential docs (declarative rule)");
        Console.WriteLine("  • Management level requirements for high-value claims");
        Console.WriteLine("  ══════════════════════════════════════════════════════════════");

        return Task.CompletedTask;
    }

    private async Task<bool> PolicyExistsAsync(CasbinPolicy policy)
    {
        return await _context.CasbinPolicies.AnyAsync(p =>
            p.PolicyType == policy.PolicyType &&
            p.V0 == policy.V0 &&
            p.V1 == policy.V1 &&
            p.V2 == policy.V2 &&
            (policy.V3 == null || p.V3 == policy.V3));
    }
}
