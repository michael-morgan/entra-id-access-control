# Access Control Framework

A production-grade access control framework for .NET applications with Entra ID (Azure AD) authentication, combining RBAC (Role-Based Access Control) and ABAC (Attribute-Based Access Control) in a modular monolith architecture.

## Features

- **Two-Layer Authorization**: Controller-level (fast RBAC) + Service-level (RBAC + ABAC with full entity context)
- **RBAC via Casbin**: Role-based permissions with group-to-role mapping and role inheritance
- **Hybrid ABAC System**:
  - **Code-Based Evaluators**: Workstream-specific business rules for complex logic (approval limits, regional restrictions, etc.)
  - **Declarative Rules**: Database-driven ABAC rules for simpler scenarios (attribute comparisons, value ranges, time restrictions)
  - **Rule Groups**: Hierarchical rule organization with AND/OR logic operators
- **Dynamic Attribute System**:
  - **Attribute Schemas**: Define custom attributes per workstream with validation rules
  - **Auto-Generated Forms**: UI forms dynamically generated from attribute schemas
  - **Type-Safe Access**: Dictionary-based storage with type-safe helper methods
- **Entra ID Integration**: JWT bearer authentication with group and role claims
- **Microsoft Graph API**: User and group management with read-only access and 15-minute caching
- **Authorization Testing Tools**:
  - **Token Analysis**: Decode JWTs and view all applicable policies, attributes, and ABAC rules
  - **Authorization Testing**: Test any authorization scenario with mock entities
  - **Scenario Generation**: Auto-generate test scenarios based on user's permissions
- **Workstream Isolation**: Multi-tenant design where each workstream operates independently
- **Repository Pattern**: Clean data access layer with interfaces for all entities
- **Event Sourcing**: Complete audit trail of all business actions and data changes
- **Audit Logging**: Before/after snapshots of all data modifications
- **Modular Monolith**: Clean separation between framework and business logic
- **Admin Web UI**: Full-featured admin interface for managing policies, attributes, rules, resources, roles, and users

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
│   │   │   ├── AbacEvaluationService.cs    # ABAC rule evaluation
│   │   │   ├── AttributeMerger.cs          # Attribute precedence (User > Role > Group)
│   │   │   ├── ResourceAttributeExtractor.cs # Extract attributes from entities
│   │   │   ├── EnvironmentContextProvider.cs # Environment attributes (time, IP, etc.)
│   │   │   ├── WorkstreamAbacEvaluatorRegistry.cs
│   │   │   ├── GenericAbacEvaluator.cs     # Evaluates declarative ABAC rules
│   │   │   ├── CasbinPolicyEngine.cs       # Policy engine abstraction
│   │   │   ├── CasbinAbacFunctions.cs      # Casbin custom functions
│   │   │   ├── Sql*Store.cs                # Attribute stores
│   │   │   ├── SqlServerCasbinAdapter.cs   # Casbin-EF Core adapter
│   │   │   └── CasbinAuthorizationHandler.cs
│   │   ├── BusinessEvents/                 # Event sourcing
│   │   │   ├── BusinessEventPublisher.cs
│   │   │   ├── BusinessEventStore.cs
│   │   │   └── BusinessEventQueryService.cs
│   │   ├── Correlation/                    # Request/workstream context
│   │   │   ├── CorrelationMiddleware.cs
│   │   │   ├── BusinessProcessManager.cs
│   │   │   └── BackgroundCorrelationProvider.cs
│   │   ├── Persistence/                    # Data Access Layer
│   │   │   ├── AccessControlDbContext.cs   # Unified DbContext (3 schemas)
│   │   │   ├── Entities/                   # Database entities
│   │   │   │   ├── Authorization/          # Auth entities
│   │   │   │   │   ├── CasbinPolicy.cs
│   │   │   │   │   ├── CasbinRole.cs
│   │   │   │   │   ├── CasbinResource.cs
│   │   │   │   │   ├── UserAttribute.cs
│   │   │   │   │   ├── GroupAttribute.cs
│   │   │   │   │   ├── RoleAttribute.cs
│   │   │   │   │   ├── AttributeSchema.cs
│   │   │   │   │   ├── AbacRule.cs
│   │   │   │   │   └── AbacRuleGroup.cs
│   │   │   │   ├── Events/                 # Event entities
│   │   │   │   └── Audit/                  # Audit entities
│   │   │   └── Repositories/               # Repository pattern
│   │   │       ├── Authorization/          # Policy, Role, Resource, Schema repositories
│   │   │       ├── Attributes/             # User, Group, Role attribute repositories
│   │   │       ├── AbacRules/              # ABAC rule repositories
│   │   │       └── Audit/                  # Audit log repository
│   │   ├── Interfaces/                     # All framework interfaces
│   │   ├── Models/                         # Domain models and DTOs
│   │   └── casbin-model.conf               # RBAC policy model
│   │
│   └── UI.Modules.AccessControl/           # Admin Web UI
│       ├── Controllers/                    # MVC controllers (organized by domain)
│       │   ├── Home/                       # Home, Workstream selection
│       │   ├── Authorization/              # Policies, Roles, Attribute Schemas
│       │   ├── Attributes/                 # User, Group, Role Attributes, Resources
│       │   ├── AbacRules/                  # ABAC Rules, Rule Groups
│       │   ├── Observability/              # Events, Authorization Testing
│       │   ├── Users/                      # User Management (Graph API)
│       │   └── Api/                        # API endpoints for AJAX
│       ├── Services/                       # Business logic layer (organized by domain)
│       │   ├── Authorization/              # Policy, Role, Resource, Schema, ABAC management
│       │   │   ├── Policies/
│       │   │   ├── Roles/
│       │   │   ├── Resources/
│       │   │   ├── AbacRules/
│       │   │   └── Users/
│       │   ├── Attributes/                 # Attribute management services
│       │   ├── Graph/                      # Microsoft Graph API integration
│       │   │   ├── GraphUserService.cs
│       │   │   ├── GraphGroupService.cs
│       │   │   ├── CachedGraphUserService.cs  # 15-min caching
│       │   │   └── CachedGraphGroupService.cs
│       │   ├── Testing/                    # Authorization testing tools
│       │   │   ├── TokenAnalysisService.cs
│       │   │   ├── AuthorizationTestingService.cs
│       │   │   └── ScenarioTestingService.cs
│       │   └── Audit/                      # Audit log services
│       ├── Views/                          # Razor views (organized by feature)
│       │   ├── Home/
│       │   ├── Policies/
│       │   ├── Roles/
│       │   ├── Resources/
│       │   ├── AttributeSchemas/
│       │   ├── UserAttributes/
│       │   ├── GroupAttributes/
│       │   ├── RoleAttributes/
│       │   ├── AbacRules/
│       │   ├── AbacRuleGroups/
│       │   ├── Users/
│       │   ├── Events/
│       │   ├── Test/                       # Authorization testing UI
│       │   └── Shared/
│       ├── Models/                         # View models
│       ├── Middleware/                     # Custom middleware
│       └── wwwroot/                        # Static files
│           ├── css/
│           └── js/                         # Client-side tools
│               ├── abac-rule-builder.js    # Interactive rule builder
│               ├── attribute-form-builder.js  # Dynamic form generation
│               └── test-analyzer.js        # Authorization test analyzer
│
├── Tests/
│   ├── alice-comprehensive-tests.sh        # Full test suite (18 tests)
│   ├── test-connection.sh                  # API connection verification
│   └── README.md                           # Testing documentation
│
└── README.md                               # This file
```

### Key Files

| File                                                                                                                | Purpose                                 |
| ------------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| **Authorization Framework**                                                                                         |                                         |
| [casbin-model.conf](Modules/Api.Modules.AccessControl/casbin-model.conf)                                            | RBAC policy model definition            |
| [AuthorizationEnforcer.cs](Modules/Api.Modules.AccessControl/Authorization/AuthorizationEnforcer.cs)                | Main authorization flow orchestrator    |
| [DefaultAbacContextProvider.cs](Modules/Api.Modules.AccessControl/Authorization/DefaultAbacContextProvider.cs)      | Builds rich context for ABAC evaluation |
| [AbacEvaluationService.cs](Modules/Api.Modules.AccessControl/Authorization/AbacEvaluationService.cs)                | Evaluates declarative ABAC rules        |
| [GenericAbacEvaluator.cs](Modules/Api.Modules.AccessControl/Authorization/GenericAbacEvaluator.cs)                  | Processes database-driven ABAC rules    |
| [AttributeMerger.cs](Modules/Api.Modules.AccessControl/Authorization/AttributeMerger.cs)                            | Handles attribute precedence logic      |
| **Database & Persistence**                                                                                          |                                         |
| [AccessControlDbContext.cs](Modules/Api.Modules.AccessControl/Persistence/AccessControlDbContext.cs)                | Unified DbContext with 3 schemas        |
| [AbacRule.cs](Modules/Api.Modules.AccessControl/Persistence/Entities/Authorization/AbacRule.cs)                     | Declarative ABAC rule entity            |
| [AbacRuleGroup.cs](Modules/Api.Modules.AccessControl/Persistence/Entities/Authorization/AbacRuleGroup.cs)           | Rule group entity with AND/OR logic     |
| [AttributeSchema.cs](Modules/Api.Modules.AccessControl/Persistence/Entities/Authorization/AttributeSchema.cs)       | Dynamic attribute schema definition     |
| [PolicyRepository.cs](Modules/Api.Modules.AccessControl/Persistence/Repositories/Authorization/PolicyRepository.cs) | Policy data access                      |
| **Demo Application**                                                                                                |                                         |
| [LoansAbacEvaluator.cs](Modules/Api.Modules.DemoApi/Authorization/LoansAbacEvaluator.cs)                            | Example code-based ABAC rules for loans |
| **Admin UI**                                                                                                        |                                         |
| [TestController.cs](Modules/UI.Modules.AccessControl/Controllers/Observability/TestController.cs)                   | Authorization testing interface         |
| [TokenAnalysisService.cs](Modules/UI.Modules.AccessControl/Services/Testing/TokenAnalysisService.cs)                | JWT token analysis and context display  |
| [GraphUserService.cs](Modules/UI.Modules.AccessControl/Services/Graph/GraphUserService.cs)                          | Microsoft Graph API user operations     |
| [AbacRulesController.cs](Modules/UI.Modules.AccessControl/Controllers/AbacRules/AbacRulesController.cs)             | Declarative ABAC rule management UI     |

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

**API Module (JWT Bearer):**
```json
{
  "EntraId": {
    "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "TenantId": "(from user secrets)"
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

**UI Module (Microsoft.Identity.Web SSO):**
```json
{
  "EntraId": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "(from user secrets)",
    "ClientId": "(from user secrets)",  // Required for OIDC/SSO
    "ClientSecret": "(from user secrets)",  // Required for Graph API calls
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
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
   - Microsoft Graph: `User.Read.All` (Delegated) - _Requires admin consent_
   - Microsoft Graph: `Group.Read.All` (Delegated) - _Requires admin consent_
   - Microsoft Graph: `GroupMember.Read.All` (Delegated) - _Requires admin consent_

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

The system uses a single database with three schemas for separation of concerns.

### auth Schema (Authorization & ABAC)

**RBAC Components:**

- **CasbinPolicies**: RBAC policies (permissions `p` and group-to-role mappings `g`)
  - Columns: PolicyType, WorkstreamId, V0-V5 (flexible policy parameters), IsActive
  - Example: `p, Loans.Approver, loans, Loan/*, approve, allow`
- **CasbinRoles**: Explicit role definitions with descriptions
  - Columns: RoleName, WorkstreamId, DisplayName, Description
  - Enables role catalog and documentation
- **CasbinResources**: Resource pattern registry
  - Columns: ResourcePattern, WorkstreamId, DisplayName, Description
  - Documents available resources per workstream (e.g., `Loan/*`, `Claim/*`)

**Attribute System:**

- **AttributeSchemas**: Dynamic attribute definitions per workstream
  - Columns: AttributeName, WorkstreamId, AppliesTo (User/Group/Role), DataType, IsRequired, ValidationRules (JSON)
  - Enables UI form generation and validation
  - Example: `{ "attributeName": "ApprovalLimit", "dataType": "Number", "validation": {"min": 0, "max": 1000000} }`
- **UserAttributes**: User-specific attributes stored as JSON
  - Columns: UserId, WorkstreamId, AttributesJson
  - Example JSON: `{"ApprovalLimit": 200000, "Region": "US-WEST", "ManagementLevel": 3}`
- **GroupAttributes**: Group-inherited attributes (JSON storage)
  - Members of group inherit these attributes (unless overridden at user level)
- **RoleAttributes**: Role-inherited attributes (JSON storage)
  - Users with role inherit these attributes (unless overridden at group/user level)
  - Attribute precedence: User > Role > Group

**Declarative ABAC Rules:**

- **AbacRules**: Database-driven ABAC rules (alternative to code-based evaluators)
  - Columns: RuleName, RuleType, WorkstreamId, Configuration (JSON), Priority, IsActive
  - Rule Types: AttributeComparison, PropertyMatch, ValueRange, TimeRestriction, LocationRestriction
  - Example: `{"ruleType": "ValueRange", "userAttribute": "ApprovalLimit", "operator": ">=", "resourceProperty": "Amount"}`
- **AbacRuleGroups**: Hierarchical rule organization
  - Columns: GroupName, WorkstreamId, LogicalOperator (AND/OR), Priority
  - Groups multiple rules with logical operators for complex conditions
  - Example: Loan approval requires (ApprovalLimit check AND Region check) OR ManagementLevel >= 4

### events Schema (Event Sourcing)

- **BusinessEvents**: Immutable event log (who, what, when, where, why)
  - Captures all business actions for compliance and audit trails
  - Columns: EventType, WorkstreamId, EntityId, UserId, Timestamp, Payload (JSON), CorrelationId
- **BusinessProcesses**: Long-running workflow tracking
  - Links related events together across multi-step processes
  - Columns: ProcessId, ProcessType, WorkstreamId, Status, StartedAt, CompletedAt, InitiatorUserId

### audit Schema (Data Changes)

- **AuditLogs**: Before/after snapshots of all data modifications
  - Tracks every Create, Update, Delete operation
  - Columns: TableName, RecordId, ChangeType, UserId, ChangedAt, OldValues (JSON), NewValues (JSON), CorrelationId
  - Enables rollback analysis and compliance reporting

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

The [DefaultAbacContextProvider](Modules/Api.Modules.AccessControl/Authorization/DefaultAbacContextProvider.cs) builds rich context by merging data from multiple sources.

**Architecture:**

```csharp
public class AbacContext
{
    // Identity
    public string UserId { get; set; }
    public string UserDisplayName { get; set; }
    public string[] Roles { get; set; }
    public string[] Groups { get; set; }

    // Dynamic Attributes (Dictionary-based for flexibility)
    public Dictionary<string, object> UserAttributes { get; set; }
    public Dictionary<string, object> ResourceAttributes { get; set; }
    public Dictionary<string, object> EnvironmentAttributes { get; set; }
}
```

**User Attributes** (merged from database with precedence):

- Loaded from UserAttributes, GroupAttributes, and RoleAttributes tables
- Merged using AttributeMerger with precedence: **User > Role > Group**
- Type-safe access via helper methods: `context.GetUserAttribute<decimal>("ApprovalLimit")`
- Common attributes: `ApprovalLimit`, `Region`, `ManagementLevel`, `Department`, `CostCenter`
- Workstream-specific schemas define available attributes dynamically

**Resource Attributes** (extracted from entity by ResourceAttributeExtractor):

- Uses reflection to extract all properties from entity
- Special mappings for common patterns:
  - `ResourceValue`: Maps to `RequestedAmount`, `Amount`, or `Value` (first non-null wins)
  - `ResourceOwnerId`: Maps to `OwnerId` or `CreatedBy`
  - `ResourceStatus`: Auto-converts enum values to strings
- Type-safe access: `context.GetResourceAttribute<string>("Region")`
- All entity properties available in ResourceAttributes dictionary

**Environment Attributes** (computed by EnvironmentContextProvider):

- `RequestTime`: Current timestamp (DateTimeOffset)
- `ClientIpAddress`: IP from X-Forwarded-For or connection
- `IsBusinessHours`: Computed from configuration (default: 8 AM - 6 PM)
- `IsInternalNetwork`: IP range check (default: 10._, 192.168._, 172.16.\*)
- Type-safe access: `context.GetEnvironmentAttribute<bool>("IsBusinessHours")`

**Key Benefits:**

- **Flexible**: Add new attributes without code changes (via AttributeSchemas)
- **Type-Safe**: Helper methods with generics prevent runtime casting errors
- **Self-Documenting**: AttributeSchemas provide metadata and validation rules
- **Workstream-Specific**: Each workstream defines its own attribute schemas

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
        if (!context.ResourceAttributes.ContainsKey("Amount")) return null;

        var loanAmount = context.GetResourceAttribute<decimal>("Amount");
        var approvalLimit = context.GetUserAttribute<decimal?>("ApprovalLimit");

        // Business Rule 1: Approval limit check
        if (approvalLimit.HasValue && loanAmount > approvalLimit.Value)
            return AbacEvaluationResult.Deny("Exceeds approval limit");

        // Business Rule 2: Regional restriction
        var userRegion = context.GetUserAttribute<string>("Region");
        var loanRegion = context.GetResourceAttribute<string>("Region");
        var managementLevel = context.GetUserAttribute<int?>("ManagementLevel") ?? 0;

        if (userRegion != loanRegion && managementLevel < 4)
            return AbacEvaluationResult.Deny("Wrong region");

        return AbacEvaluationResult.Allow("All checks passed");
    }
}
```

### Declarative ABAC Rules

As an alternative to code-based evaluators, the system supports database-driven ABAC rules.

**Rule Types:**

1. **AttributeComparison** - Compare user attribute to resource property

   ```json
   {
     "ruleType": "AttributeComparison",
     "userAttribute": "ApprovalLimit",
     "operator": ">=",
     "resourceProperty": "Amount"
   }
   ```

2. **ValueRange** - Check if value falls within min/max bounds

   ```json
   {
     "ruleType": "ValueRange",
     "attributeName": "RequestedAmount",
     "min": 1000,
     "max": 50000
   }
   ```

3. **PropertyMatch** - Exact match between user and resource attributes

   ```json
   {
     "ruleType": "PropertyMatch",
     "userAttribute": "Region",
     "resourceProperty": "Region"
   }
   ```

4. **TimeRestriction** - Time-based access control

   ```json
   {
     "ruleType": "TimeRestriction",
     "allowedHours": {
       "start": 8,
       "end": 18
     },
     "timezone": "America/New_York"
   }
   ```

5. **LocationRestriction** - IP/network-based restrictions
   ```json
   {
     "ruleType": "LocationRestriction",
     "allowedNetworks": ["10.0.0.0/8", "192.168.0.0/16"]
   }
   ```

**Rule Evaluation:**

- Rules are evaluated by `GenericAbacEvaluator`
- Executed in priority order (higher priority first)
- Can be enabled/disabled without deployment
- Combine multiple rules using `AbacRuleGroups` with AND/OR logic

**When to Use:**

- **Code-based evaluators**: Complex business logic, external service calls, performance-critical paths
- **Declarative rules**: Simple comparisons, configuration-driven rules, frequently changing logic

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

## Admin Web UI (UI.Modules.AccessControl)

The Admin Web UI provides a comprehensive interface for managing all aspects of the access control system.

### Features

**1. Policy Management** (`/Policies`)

- View all RBAC policies scoped by workstream
- Filter by policy type (p, g, g2)
- Create permission policies (p) and group-to-role mappings (g)
- Edit and delete policies
- Search across all policy fields

**2. Role Management** (`/Roles`)

- Define application roles with descriptions
- View role catalog per workstream
- Link roles to policies and attribute schemas
- Document role responsibilities

**3. Resource Management** (`/Resources`)

- Define resource patterns (e.g., `Loan/*`, `Claim/{id}`)
- Document available resources per workstream
- Link resources to policies
- Provides resource catalog for policy creation

**4. Attribute Schema Management** (`/AttributeSchemas`)

- Define custom attributes per workstream
- Specify data types (String, Number, Boolean, Date, Array, Object)
- Set validation rules (min/max, regex, required fields)
- Designate where attributes apply (User, Group, Role)
- **Auto-generates UI forms** based on schema definitions

**5. User Attributes** (`/UserAttributes`)

- Assign attributes to specific users per workstream
- Dynamic forms generated from AttributeSchemas
- Override group and role attributes at user level
- Real-time validation based on schema rules

**6. Group Attributes** (`/GroupAttributes`)

- Assign attributes to Entra ID groups per workstream
- Members inherit group attributes automatically
- Supports organizational hierarchy modeling

**7. Role Attributes** (`/RoleAttributes`)

- Assign attributes to roles per workstream
- Users with role inherit these attributes
- Useful for role-based default values

**8. Declarative ABAC Rules** (`/AbacRules`)

- Create database-driven ABAC rules without code
- Rule types:
  - **AttributeComparison**: Compare user attribute to resource property
  - **ValueRange**: Check if value falls within range
  - **PropertyMatch**: Exact match between properties
  - **TimeRestriction**: Time-based access control
  - **LocationRestriction**: IP/network-based restrictions
- Set priority for rule evaluation order
- Enable/disable rules without deployment

**9. ABAC Rule Groups** (`/AbacRuleGroups`)

- Organize rules into logical groups
- Combine rules with AND/OR operators
- Create complex conditions: `(Rule1 AND Rule2) OR Rule3`
- Priority-based evaluation

**10. User Management** (`/Users`)

- View all Entra ID users (via Microsoft Graph API)
- Search users by name or email
- View user details:
  - Group memberships (with human-readable names)
  - Assigned roles (via policy mappings)
  - Effective attributes (merged from user/role/group)
  - Applicable policies
- Manage role assignments per workstream
- **Read-only access** to Entra ID (no modifications)

**11. Authorization Testing** (`/Test`)

- **Token Analysis**: Paste JWT token to view:
  - Decoded claims (oid, name, roles, groups)
  - Applicable policies for the user
  - Effective attributes (merged with precedence)
  - ABAC rules that apply
- **Authorization Testing**: Test any authorization scenario
  - Select workstream, resource, action
  - Optionally provide mock entity JSON
  - View step-by-step evaluation trace
  - See exactly why authorization passed or failed
- **Scenario Generation**: Auto-generate test scenarios
  - Analyzes user's permissions
  - Creates realistic test cases
  - One-click execution with results

**12. Event Explorer** (`/Events`)

- View all business events
- Filter by workstream, event type, time range
- View event payload and context
- Track business processes across related events
- Timeline visualization

**13. Audit Log Viewer** (`/Events/Audit`)

- View all data modifications
- See before/after snapshots
- Filter by table, user, time range
- Compliance reporting and rollback analysis

### Microsoft Graph API Integration

The UI integrates with Microsoft Graph API for enhanced user experience:

**Features:**

- **User Directory**: Browse all Entra ID users
- **Group Resolution**: Display group names instead of GUIDs
- **Profile Information**: Show user display names, emails, job titles
- **Read-Only**: No modifications to Entra ID
- **Caching**: 15-minute cache reduces API calls
- **Delegated Permissions**: User context preserved for audit trails

**Required API Permissions** (Delegated):

- `User.Read` - Sign in and read user profile
- `User.Read.All` - Read all users' profiles
- `Group.Read.All` - Read all groups
- `GroupMember.Read.All` - Read group memberships

**Configuration:**

```json
{
  "EntraId": {
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "(from user secrets)",
    "ClientId": "(from user secrets)",
    "ClientSecret": "(from user secrets)"
  },
  "DownstreamApi": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "user.read user.read.all group.read.all groupmember.read.all"
  }
}
```

### Interactive JavaScript Tools

**1. ABAC Rule Builder** (`abac-rule-builder.js`)

- Visual rule creation interface
- Real-time configuration preview
- Validates rule structure before submission

**2. Attribute Form Builder** (`attribute-form-builder.js`)

- Dynamically generates forms from AttributeSchemas
- Client-side validation matching schema rules
- Type-appropriate input controls (number spinners, date pickers, etc.)

**3. Test Analyzer** (`test-analyzer.js`)

- Visualizes authorization test results
- Step-by-step trace rendering
- Highlights passing/failing checks
- JSON payload formatting

### Architecture Patterns

**Service Layer:**

- Management services handle all business logic
- Repository pattern for data access
- Graph services abstracted behind interfaces
- Caching services wrap Graph API calls

**Controller Organization:**

- Organized by domain (Authorization, Attributes, ABAC, Observability, Users)
- Thin controllers delegate to services
- Consistent error handling and validation

**View Models:**

- DTOs specific to UI needs
- Validation attributes for model binding
- Display attributes for UI generation

## Security

### What NOT to Commit

**NEVER commit these to source control:**

- Database connection strings with credentials
- Entra ID Client Secrets
- JWT tokens
- API keys or passwords

**Safe to commit:**

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

## Architecture Decisions

### Why Hybrid ABAC (Declarative + Code-Based)?

**Declarative Rules (Database):**

- **Pros**: Non-developers can modify rules, no deployment needed, configuration-driven, auditable changes
- **Cons**: Limited to predefined rule types, no external service calls, less performant for complex logic
- **Best for**: Simple comparisons, frequently changing rules, business-user-managed policies

**Code-Based Evaluators:**

- **Pros**: Unlimited complexity, external API calls, optimal performance, full language features
- **Cons**: Requires deployment, developer-only changes, harder to audit
- **Best for**: Complex business logic, performance-critical paths, integration with external systems

**Combined Approach**: System evaluates both, allowing teams to choose the right tool for each scenario.

### Why Attribute Schemas?

**Problem**: Hard-coded attribute properties make the system inflexible and require code changes for new attributes.

**Solution**: Dynamic attribute schemas stored in database.

**Benefits:**

- **Self-Documenting**: Schemas provide metadata (data type, validation, description)
- **UI Generation**: Admin forms automatically generated from schemas
- **Workstream-Specific**: Each workstream defines its own attributes
- **Validation**: Rules enforced at database and UI layers
- **Type-Safe**: Helper methods provide compile-time safety with runtime flexibility

**Trade-off**: Slightly more complex than hard-coded properties, but vastly more maintainable.

### Why Repository Pattern?

**Benefits:**

- **Testability**: Easy to mock repositories for unit tests
- **Separation of Concerns**: Business logic doesn't depend on EF Core
- **Flexibility**: Can swap data sources without changing business logic
- **Query Optimization**: Centralize complex queries in one place
- **Interface-Driven**: Promotes dependency injection and SOLID principles

**Implementation**: Complete repository layer for all entities with interfaces and organized by domain.

### Why JWT Claims Don't Include Attributes?

**Problem**: Attributes like ApprovalLimit, Region change frequently. JWTs are long-lived (hours).

**Solution**: Store attributes in database, query on each request.

**Benefits:**

- **Real-time Updates**: Changes effective immediately without waiting for token expiration
- **Smaller Tokens**: JWT size stays manageable
- **Attribute Precedence**: Can merge user/role/group attributes with proper precedence
- **Audit Trail**: All attribute changes tracked in database

**Trade-off**: Additional database queries, mitigated by efficient repository pattern and potential caching.

### Why Two-Layer Authorization Instead of One?

**Layer 1 (Controller)**:

- Rejects unauthorized requests before expensive database queries
- Fast RBAC-only checks
- Protects against unauthorized API exploration
- No entity data available yet → ABAC returns null

**Layer 2 (Service)**:

- Has full entity context for fine-grained decisions
- Can evaluate ABAC business rules
- Makes context-aware authorization decisions
- Called after entity retrieval from repository

**Benefits:**

- **Performance**: Most unauthorized requests rejected immediately
- **Security**: Defense in depth - both layers must pass
- **Separation of Concerns**: API gateway vs business logic
- **Flexibility**: Some operations only need RBAC, others need ABAC

### Why Workstream Scoping?

**Multi-Tenant Requirements:**

- Single codebase, multiple business domains
- Independent policy management per workstream
- Isolation between workstreams (loans can't see claims policies)
- Different ABAC rules per workstream

**Implementation:**

- `X-Workstream-Id` header on each request
- CorrelationMiddleware captures workstream context
- All database queries filtered by workstream
- ABAC evaluators registered per workstream

**Benefits:**

- **Modular Monolith**: Workstreams evolve independently
- **Security**: Built-in isolation between domains
- **Scalability**: Can extract workstreams to microservices later if needed

### Why Microsoft Graph API Integration?

**Problem**: Storing user/group data locally creates duplication and sync issues. GUIDs are not user-friendly in UI.

**Solution**: Read-only integration with Microsoft Graph API.

**Benefits:**

- **Single Source of Truth**: Entra ID remains authoritative for users/groups
- **User-Friendly**: Display names instead of GUIDs in UI
- **No Duplication**: Don't store user profile data locally
- **Audit Context**: Know who made changes (delegated permissions preserve user context)
- **Always Current**: No sync lag or stale data

**Implementation:**

- 15-minute caching reduces API calls
- Graceful degradation if Graph API unavailable (falls back to GUIDs)
- Delegated permissions (not application) to preserve user context

**Trade-off**: Requires internet connectivity and proper API permissions, but provides superior UX.

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
