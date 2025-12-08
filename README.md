# Access Control Framework

A production-grade access control framework for .NET applications with Entra ID (Azure AD) authentication, combining RBAC (Role-Based Access Control) and ABAC (Attribute-Based Access Control) in a modular monolith architecture.

## Features

- **Two-Layer Authorization**: Controller-level (fast RBAC) + Service-level (RBAC + ABAC with full entity context)
- **RBAC via Casbin**: Role-based permissions with group-to-role mapping and role inheritance
- **Custom ABAC Evaluators**: Workstream-specific business rules (approval limits, regional restrictions, etc.)
- **Entra ID Integration**: JWT bearer authentication with group and role claims
- **Workstream Isolation**: Multi-tenant design where each workstream operates independently
- **Event Sourcing**: Complete audit trail of all business actions and data changes
- **Modular Monolith**: Clean separation between framework and business logic

## Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Applications                     │
│         (Browser, Mobile App, Third-party Service)          │
└───────────────────────┬─────────────────────────────────────┘
                        │ JWT Token (from Entra ID)
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                   Api.Modules.DemoApi                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Controllers (Layer 1 - RBAC Authorization)            │ │
│  │  - LoansController, ClaimsController, DocumentsController│
│  │  - [AuthorizeResource] attribute checks                │ │
│  └────────┬───────────────────────────────────────────────┘ │
│           │                                                 │
│  ┌────────▼───────────────────────────────────────────────┐ │
│  │  Services (Layer 2 - RBAC + ABAC Authorization)        │ │
│  │  - LoanService, ClaimService, DocumentService          │ │
│  │  - Full entity data available for ABAC evaluation      │ │
│  └────────┬───────────────────────────────────────────────┘ │
│           │                                                 │
│  ┌────────▼───────────────────────────────────────────────┐ │
│  │  Repositories (Data Access)                            │ │
│  │  - In-memory storage (POC) / EF Core (production)      │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│             Api.Modules.AccessControl (Framework)           │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Authorization Components                              │ │
│  │  - AuthorizationEnforcer                               │ │
│  │  - DefaultAbacContextProvider                          │ │
│  │  - WorkstreamAbacEvaluatorRegistry                     │ │
│  │  - Casbin Policy Enforcer                              │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Middleware & Context                                  │ │
│  │  - JWT Authentication                                  │ │
│  │  - Correlation Context (Workstream, Request IDs)       │ │
│  │  - Business Process Manager                            │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│           SQL Server Database (3 Schemas)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ auth Schema  │  │events Schema │  │ audit Schema │       │
│  │              │  │              │  │              │       │
│  │ - Policies   │  │ - Business   │  │ - Audit      │       │
│  │ - Roles      │  │   Events     │  │   Logs       │       │
│  │ - Attributes │  │ - Processes  │  │              │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### Two-Layer Authorization

**Layer 1 - Controller (API Gateway)**

- Validates JWT token with Entra ID
- Checks basic RBAC permissions via Casbin
- Fast, lightweight checks before hitting business logic
- No entity data available yet → ABAC evaluator returns null (defers to RBAC)

**Layer 2 - Service (Business Logic)**

- Retrieves full entity from repository
- Re-validates RBAC permissions
- Evaluates ABAC business rules with complete entity context
- Has access to loan amount, region, status, ownership, etc.
- Makes fine-grained, context-aware authorization decisions

### Workstream Context Flow

```
HTTP Request with X-Workstream-Id header
    ↓
Middleware captures workstream ID
    ↓
CorrelationContext stores workstream throughout request
    ↓
Authorization queries policies scoped to workstream
    ↓
ABAC evaluator registered for that workstream executes
```

## Project Structure

```
entra-id-access-control/
├── Modules/
│   ├── Api.Modules.AccessControl/          # Authorization Framework
│   │   ├── Authorization/
│   │   │   ├── AuthorizationEnforcer.cs    # Main authorization orchestrator
│   │   │   ├── DefaultAbacContextProvider.cs  # Builds ABAC context
│   │   │   ├── WorkstreamAbacEvaluatorRegistry.cs
│   │   │   ├── CasbinAbacFunctions.cs      # Casbin custom functions
│   │   │   └── Sql*Store.cs                # Attribute stores
│   │   ├── BusinessEvents/                 # Event sourcing
│   │   ├── Correlation/                    # Request/workstream context
│   │   ├── Persistence/                    # DbContext (3 schemas)
│   │   └── casbin-model.conf               # RBAC policy model
│   │
│   ├── Api.Modules.DemoApi/                # Demo Application (Loans, Claims, Documents)
│   │   ├── Authorization/
│   │   │   └── LoansAbacEvaluator.cs       # Loan-specific business rules
│   │   ├── Controllers/                    # Layer 1 authorization
│   │   │   ├── LoansController.cs
│   │   │   ├── ClaimsController.cs
│   │   │   └── DocumentsController.cs
│   │   ├── Services/                       # Layer 2 authorization
│   │   │   ├── LoanService.cs
│   │   │   ├── ClaimService.cs
│   │   │   └── DocumentService.cs
│   │   ├── Data/                           # In-memory repositories
│   │   └── Models/                         # DTOs and entities
│   │
│   └── UI.Modules.AccessControl/           # Admin Web UI
│       ├── Controllers/                    # MVC controllers for admin
│       ├── Views/                          # Razor views
│       └── Models/                         # View models
│
├── Tests/
│   ├── alice-comprehensive-tests.sh        # Full test suite (18 tests)
│   ├── test-connection.sh                  # API connection verification
│   └── README.md                           # Testing documentation
│
└── README.md                               # This file
```

### Key Files

| File                                                                                                           | Purpose                                 |
| -------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| [casbin-model.conf](Modules/Api.Modules.AccessControl/casbin-model.conf)                                       | RBAC policy model definition            |
| [AuthorizationEnforcer.cs](Modules/Api.Modules.AccessControl/Authorization/AuthorizationEnforcer.cs)           | Main authorization flow orchestrator    |
| [DefaultAbacContextProvider.cs](Modules/Api.Modules.AccessControl/Authorization/DefaultAbacContextProvider.cs) | Builds rich context for ABAC evaluation |
| [LoansAbacEvaluator.cs](Modules/Api.Modules.DemoApi/Authorization/LoansAbacEvaluator.cs)                       | Example ABAC business rules for loans   |
| [AccessControlDbContext.cs](Modules/Api.Modules.AccessControl/Persistence/AccessControlDbContext.cs)           | Unified DbContext with 3 schemas        |

## Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - LocalDB (included with Visual Studio) or full SQL Server instance
- **Entra ID Tenant** - Azure AD with app registration configured
- **Git Bash or WSL** (Windows only) - For running test scripts

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/entra-id-access-control.git
cd entra-id-access-control
```

### 2. Configure User Secrets

All three modules use .NET user secrets to store sensitive configuration. Configure them as follows:

#### Api.Modules.AccessControl

```bash
dotnet user-secrets set "ConnectionStrings:AccessControlDb" "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AccessControl;Integrated Security=True" --project Modules/Api.Modules.AccessControl
```

#### Api.Modules.DemoApi

```bash
# Database connection
dotnet user-secrets set "ConnectionStrings:AccessControlDb" "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AccessControl;Integrated Security=True" --project Modules/Api.Modules.DemoApi

# Entra ID configuration
dotnet user-secrets set "EntraId:TenantId" "your-tenant-id" --project Modules/Api.Modules.DemoApi
dotnet user-secrets set "EntraId:ClientId" "your-client-id" --project Modules/Api.Modules.DemoApi
dotnet user-secrets set "EntraId:ClientSecret" "your-client-secret" --project Modules/Api.Modules.DemoApi
```

#### UI.Modules.AccessControl

```bash
# Database connection
dotnet user-secrets set "ConnectionStrings:AccessControlDb" "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AccessControl;Integrated Security=True" --project Modules/UI.Modules.AccessControl

# Entra ID configuration (for admin UI authentication and Graph API access)
dotnet user-secrets set "EntraId:Domain" "yourtenant.onmicrosoft.com" --project Modules/UI.Modules.AccessControl
dotnet user-secrets set "EntraId:TenantId" "your-tenant-id" --project Modules/UI.Modules.AccessControl
dotnet user-secrets set "EntraId:ClientId" "your-client-id" --project Modules/UI.Modules.AccessControl
dotnet user-secrets set "EntraId:ClientSecret" "your-client-secret" --project Modules/UI.Modules.AccessControl
```

**Note**: The UI admin application requires Microsoft Graph API permissions to display user and group information from Entra ID. See the [Graph API Setup](#graph-api-setup) section below for required permissions.

### 3. Create and Seed the Database

```bash
# Navigate to the DemoApi module
cd Modules/Api.Modules.DemoApi

# Apply migrations (creates database and schema)
dotnet ef database update --context AccessControlDbContext

# Seed test data (creates Alice, Bob, Carol and their policies)
dotnet run --seed
```

### 4. Run the Applications

#### API (Backend)

```bash
cd Modules/Api.Modules.DemoApi
dotnet run
```

API will be available at:

- **HTTPS**: https://localhost:7015
- **Swagger**: https://localhost:7015/swagger

#### Admin UI (Frontend)

```bash
cd Modules/UI.Modules.AccessControl
dotnet run
```

UI will be available at https://localhost:7006.

## Running Tests

The test suite validates all RBAC and ABAC authorization scenarios.

### Prerequisites

1. **API Running**: Start the DemoApi on https://localhost:7015
2. **Valid JWT Token**: Obtain Alice's JWT token from Entra ID (Using Postman or your choice)
3. **Database Seeded**: Ensure test data exists (see step 3 above)

### Quick Test

```bash
# Set Alice's JWT token
export ALICE_TOKEN="your-jwt-token-here"

# Verify API connectivity
bash Tests/test-connection.sh

# Run full test suite (18 tests)
bash Tests/alice-comprehensive-tests.sh
```

### Expected Results

```
Total Tests:  18
Passed:       18
Failed:       0

✓ ALL TESTS PASSED!
```

See [Tests/README.md](Tests/README.md) for detailed test documentation.

## Configuration

### appsettings.json Structure

```json
{
  "EntraId": {
    "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "Audience": "api://{clientId}",
    "TenantId": "(from user secrets)",
    "ClientId": "(from user secrets)",
    "ClientSecret": "(from user secrets)"
  },
  "AccessControl": {
    "Correlation": {
      "DefaultWorkstreamId": "platform",
      "IncludeInResponse": true,
      "GenerateRequestIdIfMissing": true
    },
    "Authorization": {
      "CasbinModelPath": "../Api.Modules.AccessControl/casbin-model.conf",
      "PolicyCacheDuration": "00:05:00",
      "BusinessHoursStart": 8,
      "BusinessHoursEnd": 18,
      "InternalNetworkRanges": ["10.", "192.168.", "172.16."]
    }
  },
  "ConnectionStrings": {
    "AccessControlDb": "(from user secrets)"
  }
}
```

### Entra ID App Registration

Your Entra ID app registration should be configured with:

1. **Redirect URIs**:
   - **Web**: `https://localhost:7015/signin-oidc` (DemoApi)
   - **Web**: `https://localhost:7006/signin-oidc` (UI Admin)

2. **API Permissions** (for DemoApi):
   - Microsoft Graph: `User.Read`, `GroupMember.Read.All`

3. **API Permissions** (for UI Admin with Graph API):
   - Microsoft Graph: `User.Read`
   - Microsoft Graph: `User.Read.All` (Delegated) - *Requires admin consent*
   - Microsoft Graph: `Group.Read.All` (Delegated) - *Requires admin consent*
   - Microsoft Graph: `GroupMember.Read.All` (Delegated) - *Requires admin consent*

4. **Grant Admin Consent**:
   - After adding API permissions, click "Grant admin consent for [Tenant]"
   - This is required for the delegated Graph API permissions

5. **Token Configuration**:
   - Add optional claims: `groups`
   - Enable ID tokens

6. **App Roles** (optional): Define application-specific roles

7. **Groups**: Create groups for role assignment:
   - `Loans-Officers` → `f17daf4a-2998-46f8-82d3-b049e0a8cd35`
   - `Loans-SeniorApprovers` → `045c925c-df54-41b7-8280-999dae20c742`

### Graph API Setup

The UI admin application integrates with Microsoft Graph API to provide a comprehensive user management interface.

**Features enabled by Graph API integration:**

- **Users Management**: View all Entra ID users with search capability
- **User Details**: Display user profile, group memberships, role assignments, and local attributes
- **Group Information**: Show human-readable group names instead of GUIDs
- **Role Associations**: Visualize how users inherit roles through group memberships

**What is NOT modified:**

- No changes to Entra ID (read-only access)
- User/group creation is done in Azure Portal
- Role assignments are managed locally via Casbin policies

**Implementation Details:**

- Uses delegated permissions (user context maintained for audit)
- 15-minute caching for frequently accessed data
- Automatic pagination for large result sets
- Built-in retry logic for throttling

**Entra ID App Registration Configuration:**

The UI admin application requires the following configuration in your Entra ID app registration:

1. **Redirect URI**: `https://localhost:7006/signin-oidc` (Type: Web)
2. **Authentication Flow**: Authorization Code Flow (DO NOT enable "Implicit grant and hybrid flows")
3. **API Permissions** (Delegated):
   - `User.Read` (sign-in and read user profile)
   - `User.Read.All` (read all users' full profiles)
   - `Group.Read.All` (read all groups)
   - `GroupMember.Read.All` (read group memberships)

**Why no implicit/hybrid flows?**
- Authorization Code Flow is more secure and modern
- Tokens are exchanged server-side, never exposed to browser
- Supports refresh tokens for long-running sessions

**Key Files:**

- [GraphUserService.cs](Modules/UI.Modules.AccessControl/Services/GraphUserService.cs) - User operations
- [GraphGroupService.cs](Modules/UI.Modules.AccessControl/Services/GraphGroupService.cs) - Group operations
- [UsersController.cs](Modules/UI.Modules.AccessControl/Controllers/UsersController.cs) - User management UI

## Database Schema

### auth Schema (Authorization)

- **CasbinPolicies**: RBAC policies (permissions and group-to-role mappings)
- **CasbinResources**: Resource definitions (Loan, Claim, Document, etc.)
- **CasbinRoles**: Application role definitions
- **UserAttributes**: User-specific attributes (ApprovalLimit, Region, ManagementLevel)
- **GroupAttributes**: Group-inherited attributes
- **RoleAttributes**: Role-inherited attributes

### events Schema (Event Sourcing)

- **BusinessEvents**: Immutable event log (who, what, when, where, why)
- **BusinessProcesses**: Long-running workflow tracking

### audit Schema (Data Changes)

- **AuditLogs**: Before/after snapshots of all data modifications

## Key Components

### Authorization Flow

1. **JWT Authentication** ([Program.cs](Modules/Api.Modules.DemoApi/Program.cs))

   - Validates token with Entra ID
   - Extracts claims: `oid` (user ID), `groups`, `roles`

2. **Correlation Middleware** ([CorrelationMiddleware.cs](Modules/Api.Modules.AccessControl/AspNetCore/CorrelationMiddleware.cs))

   - Captures `X-Workstream-Id` header
   - Establishes request context

3. **Controller Authorization** (Layer 1)

   - `[AuthorizeResource("Loan", "create")]` attribute
   - Checks if user's role has permission
   - ABAC evaluator returns `null` (no entity data yet)

4. **Service Authorization** (Layer 2)
   - `await _enforcer.EnsureAuthorizedAsync("Loan/id", "approve", loanEntity)`
   - ABAC evaluator has full entity data
   - Evaluates business rules (approval limit, region, etc.)

### ABAC Context

The [DefaultAbacContextProvider](Modules/Api.Modules.AccessControl/Authorization/DefaultAbacContextProvider.cs) builds rich context including:

**User Attributes** (from database):

- `ApprovalLimit`, `Region`, `ManagementLevel`, `Department`, `CostCenter`

**Resource Attributes** (extracted from entity):

- `ResourceValue` (RequestedAmount, Amount, or Value)
- `ResourceRegion`, `ResourceStatus`, `ResourceOwnerId`, `ResourceClassification`

**Environment Attributes**:

- `RequestTime`, `ClientIpAddress`, `IsBusinessHours`, `IsInternalNetwork`

Attribute precedence: **User > Role > Group**

### ABAC Evaluators

Workstream-specific business rules are implemented in evaluators:

**Example: [LoansAbacEvaluator.cs](Modules/Api.Modules.DemoApi/Authorization/LoansAbacEvaluator.cs)**

```csharp
public class LoansAbacEvaluator : IWorkstreamAbacEvaluator
{
    public string WorkstreamId => "loans";

    public Task<AbacEvaluationResult?> EvaluateAsync(AbacContext context, ...)
    {
        // Return null if entity data not available (Layer 1)
        if (context.ResourceValue == null) return null;

        // Business Rule 1: Approval limit check
        if (context.ResourceValue > context.ApprovalLimit)
            return AbacEvaluationResult.Deny("Exceeds approval limit");

        // Business Rule 2: Regional restriction
        if (context.Region != context.ResourceRegion && context.ManagementLevel < 4)
            return AbacEvaluationResult.Deny("Wrong region");

        return AbacEvaluationResult.Allow("All checks passed");
    }
}
```

### Casbin Policy Structure

**Group-to-Role Mappings (g policies)**:

```
g, {groupId}, {role}, {workstream}
g, f17daf4a..., Loans.Officer, loans
```

**Role Inheritance**:

```
g, Loans.SeniorApprover, Loans.Approver, loans
```

**Permissions (p policies)**:

```
p, {role}, {workstream}, {resource}, {action}, {effect}
p, Loans.Approver, loans, Loan/*, approve, allow
```

## Security

### What NOT to Commit

❌ **NEVER commit these to source control:**

- Database connection strings with credentials
- Entra ID Client Secrets
- JWT tokens
- API keys or passwords

✅ **Safe to commit:**

- appsettings.json with `"(from user secrets)"` placeholders
- Test user GUIDs (these are demo data, not real credentials)
- .csproj files with `<UserSecretsId>` configured

### User Secrets Storage

User secrets are stored outside the project directory:

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`

These files are **never** included in source control.

## Development Workflow

### Adding a New Workstream

1. Create workstream folder in `Modules/Api.Modules.DemoApi/`
2. Implement `IWorkstreamAbacEvaluator`
3. Register in `Program.cs`:
   ```csharp
   services.AddWorkstreamAbacEvaluator<YourEvaluator>("workstream-id");
   ```
4. Seed policies in `DatabaseSeeder.cs`
5. Add user/group/role attributes for workstream

### Running Database Migrations

```bash
cd Modules/Api.Modules.DemoApi

# Create a new migration
dotnet ef migrations add YourMigrationName --context AccessControlDbContext

# Apply to database
dotnet ef database update --context AccessControlDbContext
```

### Debugging Authorization

Enable debug logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "AccessControl": "Debug"
    }
  }
}
```

Look for log prefixes:

- `[JWT AUTH]` - Token validation
- `[CASBIN DEBUG]` - Policy enforcement
- `[ABAC RULES DEBUG]` - ABAC evaluation
- `[LOANS ABAC]` - Workstream-specific rules

## Test Users

The seed data creates three test users:

### Alice (Loans Officer + Senior Approver)

- **User ID**: `1c9a126e-98e7-42d8-8597-a59473bef64a`
- **Roles**: `Loans.Officer`, `Loans.SeniorApprover`
- **Attributes**: ApprovalLimit=$200K, Region=US-WEST, ManagementLevel=3

### Bob (Claims Adjudicator)

- **User ID**: `2d8b237f-a9f8-43e9-96a8-b6a584cfe75b`
- **Roles**: `Claims.Adjudicator`
- **Attributes**: ApprovalLimit=$150K, Region=US-EAST, ManagementLevel=2

### Carol (Documents Manager)

- **User ID**: `3e9c348a-bad9-54fa-a7b9-c7b695dg86c`
- **Roles**: `Documents.Manager`
- **Attributes**: Department=Compliance, Region=US-CENTRAL

## Troubleshooting

### Database Connection Issues

**Error**: "Cannot open database 'AccessControl'"

**Solution**: Ensure SQL Server is running and the connection string is correct in user secrets.

### JWT Token Validation Fails

**Error**: "401 Unauthorized" even with valid token

**Solution**:

1. Check that `TenantId` and `ClientId` match your Entra ID app
2. Verify the token audience matches `api://{clientId}`
3. Ensure token hasn't expired (check `exp` claim)

### ABAC Rules Not Evaluating

**Issue**: ABAC evaluator not being called

**Check**:

1. Evaluator registered in `Program.cs`
2. Workstream ID matches (`X-Workstream-Id` header)
3. Entity data available (Layer 2 authorization)
4. Debug logs show `[ABAC RULES DEBUG]` messages

### EF Core Migration Issues

**Error**: "No DbContext named 'AccessControlDbContext' was found"

**Solution**: Make sure you're running migrations from the `Api.Modules.DemoApi` project directory.

## Contributing

This is a proof-of-concept implementation. For production use:

1. Replace in-memory repositories with EF Core
2. Add comprehensive error handling
3. Implement caching for policy lookups
4. Add API rate limiting
5. Configure HTTPS certificates
6. Set up CI/CD pipelines
7. Add integration tests for all workstreams
8. Implement audit log retention policies

## License

[Your License Here]

## Support

For questions or issues:

1. Check the [Tests/README.md](Tests/README.md) for testing guidance
2. Review debug logs with `"AccessControl": "Debug"` logging enabled
3. Examine the Casbin policies in the database (`auth.CasbinPolicies`)
