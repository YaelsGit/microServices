#!/bin/bash
# API Gateway Testing Guide
# This file contains curl commands to test the gateway functionality

echo "=== API Gateway Testing Commands ==="
echo ""
echo "Make sure all services are running:"
echo "- AuthService: http://localhost:5001"
echo "- CatalogService: http://localhost:5002"
echo "- OrderService: http://localhost:5003"
echo "- LotteryService: http://localhost:5004"
echo "- API Gateway: http://localhost:5000"
echo ""

# Test 1: Gateway is running
echo "1. Test gateway is responding:"
echo "curl http://localhost:5000/health"
echo ""

# Test 2: Catalog (No auth required)
echo "2. Test Catalog Service routing (no auth):"
echo "curl http://localhost:5000/catalog/products/list"
echo ""

# Test 3: Auth - Login
echo "3. Test Auth Service - Login:"
echo 'curl -X POST http://localhost:5000/auth/users/login \\'
echo '  -H "Content-Type: application/json" \\'
echo '  -d "{\"email\": \"test@example.com\", \"password\": \"password123\"}"'
echo ""
echo "Response will contain JWT token like:"
echo '{"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."}'
echo ""

# Test 4: Use JWT token for protected endpoints
echo "4. Test Orders Service routing (with JWT):"
echo "Replace YOUR_JWT_TOKEN with actual token from login response"
echo 'curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \\'
echo '     http://localhost:5000/orders/orders/list'
echo ""

# Test 5: Test invalid token
echo "5. Test Invalid token rejection:"
echo 'curl -H "Authorization: Bearer invalid.token.here" \\'
echo '     http://localhost:5000/orders/orders/list'
echo "Expected: 401 Unauthorized"
echo ""

# Test 6: Test expired token
echo "6. Test Protected endpoint without token:"
echo "curl http://localhost:5000/orders/orders/list"
echo "Expected: 401 Unauthorized (no token or allowed through, depends on config)"
echo ""

# Test 7: Test Lottery Service
echo "7. Test Lottery Service routing:"
echo 'curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \\'
echo '     http://localhost:5000/lottery/lotteries/list'
echo ""

# Test 8: Test POST request
echo "8. Test Create Order (POST):"
echo 'curl -X POST http://localhost:5000/orders/orders/create \\'
echo '  -H "Authorization: Bearer YOUR_JWT_TOKEN" \\'
echo '  -H "Content-Type: application/json" \\'
echo '  -d "{\"userId\": 1, \"items\": [{\"productId\": 1, \"quantity\": 2}]}"'
echo ""

# Test 9: Test PUT request
echo "9. Test Update Order (PUT):"
echo 'curl -X PUT http://localhost:5000/orders/orders/1 \\'
echo '  -H "Authorization: Bearer YOUR_JWT_TOKEN" \\'
echo '  -H "Content-Type: application/json" \\'
echo '  -d "{\"status\": \"Completed\"}"'
echo ""

# Test 10: Test DELETE request
echo "10. Test Delete Order (DELETE):"
echo 'curl -X DELETE http://localhost:5000/orders/orders/1 \\'
echo '  -H "Authorization: Bearer YOUR_JWT_TOKEN"'
echo ""

echo "=== Quick Testing Script ==="
echo ""
echo "To run these tests programmatically, create a test.sh file and use:"
echo ""
echo "#!/bin/bash"
echo "GATEWAY_URL='http://localhost:5000'"
echo ""
echo "# Get JWT token"
echo "echo 'Logging in...'"
echo "TOKEN=\$(curl -s -X POST \$GATEWAY_URL/auth/users/login \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{\"email\": \"test@example.com\", \"password\": \"password123\"}' | jq -r '.token')"
echo ""
echo "echo \"Got token: \$TOKEN\""
echo ""
echo "# Test catalog"
echo "echo 'Testing catalog...'"
echo "curl -s \$GATEWAY_URL/catalog/products/list | jq"
echo ""
echo "# Test orders with token"
echo "echo 'Testing orders...'"
echo "curl -s -H \"Authorization: Bearer \$TOKEN\" \\"
echo "     \$GATEWAY_URL/orders/orders/list | jq"
echo ""
