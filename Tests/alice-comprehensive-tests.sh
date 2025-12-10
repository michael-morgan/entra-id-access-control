#!/bin/bash

################################################################################
# Alice Comprehensive Access Control Test Suite
# Tests RBAC and ABAC authorization for the Loans workstream
################################################################################

# Note: Don't use 'set -e' because we want to continue running tests even if some fail

# ANSI color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
API_URL="${API_URL:-https://localhost:7015}"
TOKEN="${ALICE_TOKEN}"

# Counters
TESTS_TOTAL=0
TESTS_PASSED=0
TESTS_FAILED=0

# Helper functions
print_header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}========================================${NC}\n"
}

print_test() {
    echo -e "${BLUE}[TEST $1]${NC} $2"
}

print_success() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    ((TESTS_PASSED++))
}

print_failure() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    ((TESTS_FAILED++))
}

print_info() {
    echo -e "${YELLOW}ℹ INFO:${NC} $1"
}

# Validate prerequisites
if [ -z "$TOKEN" ]; then
    echo -e "${RED}ERROR: ALICE_TOKEN environment variable is not set${NC}"
    echo "Please set it with: export ALICE_TOKEN='your-jwt-token'"
    exit 1
fi

################################################################################
# Test Suite
################################################################################

print_header "ALICE COMPREHENSIVE ACCESS CONTROL TEST SUITE"
echo "API URL: $API_URL"
echo "User: Alice (Loans Officer + Senior Approver)"
echo "Attributes: ApprovalLimit=\$200,000, Region=US-WEST, ManagementLevel=3"
echo ""

################################################################################
# SECTION 1: RBAC Tests - Role-Based Access Control
################################################################################

print_header "SECTION 1: RBAC TESTS"

# Test 1: Create loan (Loans.Officer permission)
print_test "1.1" "Create loan as Loans.Officer (RBAC)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "RBAC-001",
    "applicantName": "RBAC Test Borrower",
    "requestedAmount": 100000,
    "termMonths": 360,
    "region": "US-WEST"
  }' 2>&1)

if [ $? -ne 0 ]; then
    print_failure "Curl command failed - is the API running?"
    HTTP_CODE="000"
else
    HTTP_CODE=$(echo "$RESPONSE" | tail -1)
    BODY=$(echo "$RESPONSE" | head -n -1)
fi

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_RBAC_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created with ID: $LOAN_RBAC_ID"
elif [ "$HTTP_CODE" = "000" ]; then
    print_failure "Could not connect to API at $API_URL"
else
    print_failure "Expected 201, got $HTTP_CODE - Response: $(echo "$BODY" | head -c 200)"
fi

# Test 2: List loans (Loans.Officer + Loans.Approver permission)
print_test "1.2" "List loans (RBAC - both roles have permission)"
((TESTS_TOTAL++))
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X GET "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Successfully listed loans"
else
    print_failure "Expected 200, got $HTTP_CODE"
fi

# Test 3: Read specific loan (Loans.Officer permission)
print_test "1.3" "Read specific loan (RBAC)"
((TESTS_TOTAL++))
if [ -n "$LOAN_RBAC_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X GET "$API_URL/api/loans/$LOAN_RBAC_ID" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans")

    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Successfully read loan $LOAN_RBAC_ID"
    else
        print_failure "Expected 200, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

################################################################################
# SECTION 2: ABAC Tests - Attribute-Based Access Control
################################################################################

print_header "SECTION 2: ABAC TESTS - APPROVAL LIMIT"

# Test 4: Create loan within approval limit ($150K)
print_test "2.1" "Create loan \$150K (within \$200K limit)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "ABAC-001",
    "applicantName": "Alpha Borrower",
    "requestedAmount": 150000,
    "termMonths": 360,
    "region": "US-WEST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_150K_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created: $LOAN_150K_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 5: Approve loan within limit
print_test "2.2" "Approve \$150K loan (ABAC - within limit)"
((TESTS_TOTAL++))
if [ -n "$LOAN_150K_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_150K_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 150000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - within approval limit",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Approved \$150K loan (within \$200K limit)"
    else
        print_failure "Expected 200, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

# Test 6: Create loan at exact approval limit ($200K)
print_test "2.3" "Create loan \$200K (exactly at limit)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "ABAC-002",
    "applicantName": "Beta Borrower",
    "requestedAmount": 200000,
    "termMonths": 360,
    "region": "US-WEST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_200K_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created: $LOAN_200K_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 7: Approve loan at exact limit
print_test "2.4" "Approve \$200K loan (ABAC - exactly at limit)"
((TESTS_TOTAL++))
if [ -n "$LOAN_200K_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_200K_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 200000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - exactly at approval limit",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Approved \$200K loan (boundary test passed)"
    else
        print_failure "Expected 200, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

# Test 8: Create loan exceeding approval limit ($250K)
print_test "2.5" "Create loan \$250K (exceeds \$200K limit)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "ABAC-003",
    "applicantName": "Gamma Borrower",
    "requestedAmount": 250000,
    "termMonths": 360,
    "region": "US-WEST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_250K_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created: $LOAN_250K_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 9: Attempt to approve loan exceeding limit (should fail)
print_test "2.6" "Approve \$250K loan (ABAC - SHOULD DENY)"
((TESTS_TOTAL++))
if [ -n "$LOAN_250K_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_250K_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 250000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - exceeds approval limit",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "403" ]; then
        print_success "Correctly denied approval (exceeds \$200K limit)"
    else
        print_failure "Expected 403 Forbidden, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

################################################################################
# SECTION 3: ABAC Tests - Regional Restrictions
################################################################################

print_header "SECTION 3: ABAC TESTS - REGIONAL RESTRICTIONS"

# Test 10: Create loan in correct region (US-WEST)
print_test "3.1" "Create loan in US-WEST (correct region)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "REGION-001",
    "applicantName": "Region Test US-WEST",
    "requestedAmount": 100000,
    "termMonths": 360,
    "region": "US-WEST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_WEST_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created in US-WEST: $LOAN_WEST_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 11: Approve loan in correct region
print_test "3.2" "Approve US-WEST loan (ABAC - same region)"
((TESTS_TOTAL++))
if [ -n "$LOAN_WEST_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_WEST_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 100000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - correct region",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Approved US-WEST loan (region match)"
    else
        print_failure "Expected 200, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

# Test 12: Create loan in wrong region (US-EAST)
print_test "3.3" "Create loan in US-EAST (wrong region)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "REGION-002",
    "applicantName": "Region Test US-EAST",
    "requestedAmount": 100000,
    "termMonths": 360,
    "region": "US-EAST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_EAST_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created in US-EAST: $LOAN_EAST_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 13: Attempt to approve cross-region loan (should fail - ManagementLevel < 4)
print_test "3.4" "Approve US-EAST loan (ABAC - SHOULD DENY, wrong region)"
((TESTS_TOTAL++))
if [ -n "$LOAN_EAST_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_EAST_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 100000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - cross-region attempt",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "403" ]; then
        print_success "Correctly denied cross-region approval (ManagementLevel=3 < 4)"
    else
        print_failure "Expected 403 Forbidden, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

################################################################################
# SECTION 4: Combined ABAC Tests
################################################################################

print_header "SECTION 4: COMBINED ABAC TESTS"

# Test 14: Attempt to approve loan that violates both limit and region
print_test "4.1" "Create \$300K US-EAST loan (violates both limit and region)"
((TESTS_TOTAL++))
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "COMBINED-001",
    "applicantName": "Combined ABAC Test",
    "requestedAmount": 300000,
    "termMonths": 360,
    "region": "US-EAST"
  }')
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "201" ]; then
    LOAN_COMBINED_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    print_success "Loan created: $LOAN_COMBINED_ID"
else
    print_failure "Expected 201, got $HTTP_CODE"
fi

# Test 15: Attempt approval (should fail on first rule - limit check happens before region)
print_test "4.2" "Approve \$300K US-EAST loan (ABAC - SHOULD DENY)"
((TESTS_TOTAL++))
if [ -n "$LOAN_COMBINED_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_COMBINED_ID/approve" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "approvedAmount": 300000,
        "interestRate": 4.5,
        "approvalNotes": "ABAC test - violates both rules",
        "isFinalApproval": true
      }')

    if [ "$HTTP_CODE" = "403" ]; then
        print_success "Correctly denied (violates approval limit, checked first)"
    else
        print_failure "Expected 403 Forbidden, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID from previous test"
fi

################################################################################
# SECTION 5: Role Inheritance Tests
################################################################################

print_header "SECTION 5: ROLE INHERITANCE TESTS"

# Test 16: Verify SeniorApprover inherits Approver permissions
print_test "5.1" "Reject loan (inherited from Loans.Approver role)"
((TESTS_TOTAL++))
if [ -n "$LOAN_250K_ID" ]; then
    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/loans/$LOAN_250K_ID/reject" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Workstream-Id: loans" \
      -H "Content-Type: application/json" \
      -d '{
        "rejectionReason": "Test rejection - role inheritance verification"
      }')

    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Successfully rejected loan (SeniorApprover inherits Approver permissions)"
    else
        print_failure "Expected 200, got $HTTP_CODE"
    fi
else
    print_failure "Cannot test - no loan ID available"
fi

################################################################################
# SECTION 6: Workstream Isolation Tests
################################################################################

print_header "SECTION 6: WORKSTREAM ISOLATION TESTS"

# Test 17: Attempt to access loans without workstream header (should default to 'platform')
print_test "6.1" "List loans without X-Workstream-Id header"
((TESTS_TOTAL++))
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X GET "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN")

if [ "$HTTP_CODE" = "403" ] || [ "$HTTP_CODE" = "401" ]; then
    print_success "Correctly denied access (no workstream header)"
else
    print_info "Got $HTTP_CODE - may use default workstream 'platform'"
fi

# Test 18: Attempt to access with wrong workstream
print_test "6.2" "List loans with wrong workstream (claims)"
((TESTS_TOTAL++))
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X GET "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: claims")

if [ "$HTTP_CODE" = "403" ] || [ "$HTTP_CODE" = "404" ]; then
    print_success "Correctly isolated workstreams"
else
    print_info "Got $HTTP_CODE - workstream isolation may allow fallback"
fi

################################################################################
# Test Summary
################################################################################

print_header "TEST SUMMARY"
echo "Total Tests:  $TESTS_TOTAL"
echo -e "${GREEN}Passed:       $TESTS_PASSED${NC}"
echo -e "${RED}Failed:       $TESTS_FAILED${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED!${NC}"
    exit 0
else
    echo -e "${RED}✗ SOME TESTS FAILED${NC}"
    exit 1
fi
