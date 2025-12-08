# Access Control Test Suite

This directory contains integration tests for the Access Control framework, covering RBAC (Role-Based Access Control) and ABAC (Attribute-Based Access Control) scenarios.

## Test Scripts

### alice-comprehensive-tests.sh

Comprehensive test suite for Alice (Loans Officer + Senior Approver) covering all proof-of-concept scenarios.

**Alice's Configuration:**

- **User ID**: `1c9a126e-98e7-42d8-8597-a59473bef64a`
- **Groups**:
  - `f17daf4a-2998-46f8-82d3-b049e0a8cd35` (Loans-Officers)
  - `045c925c-df54-41b7-8280-999dae20c742` (Loans-SeniorApprovers)
- **Roles**:
  - `Loans.Officer` (via Loans-Officers group)
  - `Loans.SeniorApprover` (via Loans-SeniorApprovers group, inherits Loans.Approver)
- **Attributes**:
  - `ApprovalLimit`: $200,000
  - `Region`: US-WEST
  - `ManagementLevel`: 3

## Running the Tests

### Prerequisites

1. **API Running**: Ensure the DemoApi is running on `https://localhost:7015`

   ```bash
   cd Modules/Api.Modules.DemoApi
   dotnet run
   ```

2. **Valid JWT Token**: Obtain a fresh JWT token for Alice from Entra ID

   ```bash
   # Use your authentication flow to get Alice's token
   export ALICE_TOKEN="eyJ0eXAi..."
   ```

3. **Database Seeded**: Ensure the database has been seeded with test data
   ```bash
   cd Modules/Api.Modules.DemoApi
   dotnet run -- --seed
   ```

### Running the Test Suite

```bash
# Set the JWT token
export ALICE_TOKEN="your-jwt-token-here"

# Optional: Set custom API URL (default: https://localhost:7015)
export API_URL="https://localhost:7015"

# Run the tests
bash Tests/alice-comprehensive-tests.sh
```

### Using Git Bash on Windows

```bash
# Navigate to the project root
cd /c/Users/YourName/Documents/entra-id-access-control

# Set environment variables
export ALICE_TOKEN="your-jwt-token-here"

# Run tests
bash Tests/alice-comprehensive-tests.sh
```

## Test Coverage

### Section 1: RBAC Tests (3 tests)

Tests basic role-based permissions without attribute evaluation:

1. **Create Loan** - Verify `Loans.Officer` role can create loans
2. **List Loans** - Verify both `Loans.Officer` and `Loans.Approver` can list loans
3. **Read Loan** - Verify `Loans.Officer` can read specific loan details

### Section 2: ABAC Approval Limit Tests (6 tests)

Tests attribute-based approval limit enforcement:

1. **Create $150K Loan** - Within Alice's $200K approval limit
2. **Approve $150K Loan** - Should succeed (within limit)
3. **Create $200K Loan** - Exactly at Alice's approval limit (boundary test)
4. **Approve $200K Loan** - Should succeed (boundary test)
5. **Create $250K Loan** - Exceeds Alice's approval limit
6. **Approve $250K Loan** - Should FAIL (exceeds $200K limit)

### Section 3: ABAC Regional Restriction Tests (4 tests)

Tests attribute-based regional restrictions:

1. **Create US-WEST Loan** - Alice's region
2. **Approve US-WEST Loan** - Should succeed (same region)
3. **Create US-EAST Loan** - Different region
4. **Approve US-EAST Loan** - Should FAIL (Alice is US-WEST, ManagementLevel 3 < 4 required for cross-region)

### Section 4: Combined ABAC Tests (2 tests)

Tests multiple ABAC rules together:

1. **Create $300K US-EAST Loan** - Violates both approval limit and region
2. **Approve $300K US-EAST Loan** - Should FAIL (checks approval limit first)

### Section 5: Role Inheritance Tests (1 test)

Tests that `Loans.SeniorApprover` inherits `Loans.Approver` permissions:

1. **Reject Loan** - Verify SeniorApprover can use Approver's reject permission

### Section 6: Workstream Isolation Tests (2 tests)

Tests workstream boundary enforcement:

1. **Access Without Workstream Header** - Should fail or use default workstream
2. **Access With Wrong Workstream** - Verify loans workstream isolated from claims

## Expected Results

```
Total Tests:  18
Passed:       18
Failed:       0

✓ ALL TESTS PASSED!
```

### ABAC Evaluation Logic

The tests verify the following ABAC rules from [LoansAbacEvaluator.cs](../Modules/Api.Modules.DemoApi/Authorization/LoansAbacEvaluator.cs):

1. **Approval Limit Check** (lines 61-67)

   - Denies if `loanAmount > userApprovalLimit`
   - Alice: $200,000 limit

2. **Management Level Check** (lines 70-81)

   - Loans > $500K require ManagementLevel ≥ 3
   - Alice: ManagementLevel 3 (passes for loans up to $500K if within her $200K approval limit)

3. **Regional Restriction** (lines 98-113)

   - Denies if `userRegion != loanRegion` AND `ManagementLevel < 4`
   - Alice: US-WEST, ManagementLevel 3 (cannot approve cross-region)

4. **Status Check** (lines 119-125)
   - Only "Submitted" or "UnderReview" loans can be approved

## Two-Layer Authorization Architecture

The tests demonstrate the framework's two-layer authorization:

### Layer 1: Controller (API Gateway)

- Validates JWT token
- Checks RBAC permissions (role-based)
- Fast, coarse-grained checks
- No entity data available yet
- ABAC evaluator returns `null` (defers to RBAC)

### Layer 2: Service (Business Logic)

- Retrieves full entity from repository
- Re-validates RBAC permissions
- Evaluates ABAC business rules with full entity context
- Fine-grained, context-aware decisions
- ABAC evaluator has access to loan amount, region, status, etc.

## Troubleshooting

### Token Expired

If tests fail with 401 Unauthorized:

```bash
# Get a fresh token and re-export
export ALICE_TOKEN="new-token-here"
```

### API Not Running

If tests fail with connection errors:

```bash
# Start the API
cd Modules/Api.Modules.DemoApi
dotnet run
```

### Database Not Seeded

If tests fail because Alice's attributes aren't found:

```bash
# Reseed the database
cd Modules/Api.Modules.DemoApi
dotnet run -- --seed
```

### Permission Issues on Windows

If you can't execute the script:

```bash
# Use Git Bash or WSL
bash Tests/alice-comprehensive-tests.sh
```

## Adding New Tests

To add new test scenarios:

1. Add a new section or test within an existing section
2. Follow the pattern:

   ```bash
   print_test "X.Y" "Description of test"
   ((TESTS_TOTAL++))

   # Make API call
   HTTP_CODE=$(curl -s -w "%{http_code}" ...)

   # Verify result
   if [ "$HTTP_CODE" = "expected_code" ]; then
       print_success "Success message"
   else
       print_failure "Failure message"
   fi
   ```

3. Update this README with the new test description

## Related Documentation

- [LoansAbacEvaluator.cs](../Modules/Api.Modules.DemoApi/Authorization/LoansAbacEvaluator.cs) - ABAC business rules
- [AuthorizationEnforcer.cs](../Modules/Api.Modules.AccessControl/Authorization/AuthorizationEnforcer.cs) - Authorization flow
- [DefaultAbacContextProvider.cs](../Modules/Api.Modules.AccessControl/Authorization/DefaultAbacContextProvider.cs) - Context building
