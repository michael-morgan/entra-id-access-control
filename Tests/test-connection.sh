#!/bin/bash

################################################################################
# Connection Test - Verify API and Token
################################################################################

API_URL="${API_URL:-http://localhost:5163}"
TOKEN="${ALICE_TOKEN}"

echo "================================"
echo "API Connection Test"
echo "================================"
echo ""

# Check if token is set
if [ -z "$TOKEN" ]; then
    echo "ERROR: ALICE_TOKEN environment variable is not set"
    echo "Set it with: export ALICE_TOKEN='your-jwt-token'"
    exit 1
fi

echo "API URL: $API_URL"
echo "Token length: ${#TOKEN} characters"
echo "Token preview: ${TOKEN:0:50}..."
echo ""

# Test 1: Check if API is reachable
echo "[TEST 1] Checking if API is reachable..."
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null "$API_URL/swagger/index.html" 2>&1)
CURL_EXIT=$?

if [ $CURL_EXIT -ne 0 ]; then
    echo "FAIL: Could not connect to API at $API_URL"
    echo "Curl exit code: $CURL_EXIT"
    echo ""
    echo "Is the API running? Try:"
    echo "  cd Modules/Api.Modules.DemoApi"
    echo "  dotnet run"
    exit 1
else
    echo "SUCCESS: API is reachable (HTTP $HTTP_CODE)"
fi
echo ""

# Test 2: Try to list loans with authentication
echo "[TEST 2] Testing authentication with loans endpoint..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X GET "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" 2>&1)

CURL_EXIT=$?
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | grep -v "HTTP_CODE:")

echo "Curl exit code: $CURL_EXIT"
echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" = "200" ]; then
    echo "SUCCESS: Authentication working!"
    echo "Response preview: $(echo "$BODY" | head -c 200)"
elif [ "$HTTP_CODE" = "401" ]; then
    echo "FAIL: Authentication failed (401 Unauthorized)"
    echo "Possible causes:"
    echo "  - Token has expired"
    echo "  - Token is invalid"
    echo "  - Wrong audience or issuer"
    echo ""
    echo "Response: $BODY"
elif [ "$HTTP_CODE" = "403" ]; then
    echo "FAIL: Authorization failed (403 Forbidden)"
    echo "User is authenticated but doesn't have permission"
    echo ""
    echo "Response: $BODY"
else
    echo "UNEXPECTED: Got HTTP $HTTP_CODE"
    echo "Response: $BODY"
fi
echo ""

# Test 3: Try to create a loan
echo "[TEST 3] Testing loan creation..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$API_URL/api/loans" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Workstream-Id: loans" \
  -H "Content-Type: application/json" \
  -d '{
    "applicantId": "TEST-001",
    "applicantName": "Connection Test",
    "requestedAmount": 50000,
    "termMonths": 360,
    "region": "US-WEST"
  }' 2>&1)

CURL_EXIT=$?
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | grep -v "HTTP_CODE:")

echo "Curl exit code: $CURL_EXIT"
echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" = "201" ]; then
    echo "SUCCESS: Loan created!"
    LOAN_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "Loan ID: $LOAN_ID"
    echo ""
    echo "All tests passed! You can now run the full test suite:"
    echo "  bash Tests/alice-comprehensive-tests.sh"
elif [ "$HTTP_CODE" = "401" ]; then
    echo "FAIL: Authentication failed (401 Unauthorized)"
    echo "Your token may have expired. Get a new token."
    echo ""
    echo "Response: $BODY"
elif [ "$HTTP_CODE" = "400" ]; then
    echo "FAIL: Bad request (400)"
    echo "This might be a model validation error."
    echo ""
    echo "Response: $BODY"
else
    echo "UNEXPECTED: Got HTTP $HTTP_CODE"
    echo "Response: $BODY"
fi
echo ""

echo "================================"
echo "Test Complete"
echo "================================"
