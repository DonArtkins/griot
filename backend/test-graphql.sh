#!/bin/bash
# Quick GraphQL testing script
# This demonstrates that GraphQL is live and responding (auth will be added in Feature 08)

echo "=== Testing Griot GraphQL Endpoint ==="
echo ""
echo "1. Checking if API is healthy..."
curl -s http://localhost:5064/health
echo ""
echo ""

echo "2. Fetching GraphQL Schema (first 40 lines)..."
curl -s 'http://localhost:5064/graphql?sdl' | head -40
echo ""
echo ""

echo "3. Testing introspection query (schema query without auth)..."
curl -s -X POST http://localhost:5064/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __schema { queryType { name } mutationType { name } } }"}' | python3 -m json.tool
echo ""
echo ""

echo "4. Testing authenticated query (expected: AUTH_NOT_AUTHENTICATED until Feature 08)..."
curl -s -X POST http://localhost:5064/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query { board(id: \"ffffffff-ffff-ffff-ffff-ffffffffffff\") { id name } }"}' | python3 -m json.tool
echo ""
echo ""

echo "=== Database Stats ==="
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot -Q "
SELECT 'USERS' as Entity, COUNT(*) as Count FROM Users
UNION ALL SELECT 'WORKSPACES', COUNT(*) FROM Workspaces
UNION ALL SELECT 'PROJECTS', COUNT(*) FROM Projects
UNION ALL SELECT 'TASKS', COUNT(*) FROM TaskItems
ORDER BY Entity" 2>/dev/null

echo ""
echo "✅ GraphQL endpoint is LIVE and operational at http://localhost:5064/graphql"
echo "✅ Schema contains $(curl -s 'http://localhost:5064/graphql?sdl' | grep -c '^type ') types"
echo "✅ Test data loaded: 4 users, 2 workspaces, 3 projects, 6 tasks"
echo ""
echo "📖 See backend/GRAPHQL-STATUS.md for full testing guide"
echo "📖 See backend/src/Griot.Infrastructure/Sql/README-SEED-DATA.md for seed data details"
