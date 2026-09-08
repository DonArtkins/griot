# Using GraphQL in Your Browser — Complete Guide

## 🎯 What You're Seeing

When you visit **http://localhost:5064/graphql** in your browser, you have access to THREE different things depending on how you access it:

### 1. **Banana Cake Pop** (Interactive GraphQL IDE) 🍰
**URL:** http://localhost:5064/graphql/ ← **(note the trailing slash!)**

This is HotChocolate's built-in GraphQL IDE called **Banana Cake Pop**. It's like Postman or Swagger, but specifically for GraphQL.

**What you can do:**
- ✅ Browse the complete schema (all types, queries, mutations)
- ✅ Write and execute GraphQL queries interactively
- ✅ Get autocomplete suggestions as you type
- ✅ See documentation for every field and type
- ✅ Add authentication headers (for JWT tokens when Feature 07 is done)
- ✅ View query execution results with syntax highlighting
- ✅ Save and organize your queries

**Why it's useful:**
- You don't need Postman yet — this is your GraphQL testing tool
- It's faster than writing `curl` commands
- Autocomplete helps you discover what's available
- Perfect for development and testing

### 2. **Schema Definition Language (SDL)**
**URL:** http://localhost:5064/graphql?sdl

This shows you the raw GraphQL schema in SDL format (GraphQL's schema language).

**What you can do:**
- ✅ See all available types, queries, and mutations
- ✅ Copy the schema for documentation or code generation
- ✅ Understand the full API contract

**Example output:**
```graphql
type GriotQuery {
  board(id: UUID!): BoardType
  tasks(boardId: UUID!, first: Int): TasksConnection
  ...
}

type TaskItemType {
  id: UUID!
  title: String!
  status: String!
  ...
}
```

### 3. **GraphQL Endpoint** (POST requests)
**URL:** http://localhost:5064/graphql

This is the actual endpoint for executing GraphQL queries. It only accepts POST requests with JSON payloads.

**Used by:**
- Your web app (React)
- Your mobile app (Flutter)
- API clients (Postman, curl)
- The Banana Cake Pop UI itself

## 🚀 How to Use Banana Cake Pop (The Interactive UI)

### Step 1: Access the UI
Open your browser and navigate to:
```
http://localhost:5064/graphql/
```
*Note: The trailing slash is important!*

### Step 2: Explore the Schema
1. Look for the **"Schema"** or **"Docs"** tab (usually on the right side)
2. Click on it to see all available queries and mutations
3. Click on any type (like `TaskItemType`) to see its fields

### Step 3: Write Your First Query

Click in the query editor (left panel) and start typing:

```graphql
query {
  __schema {
    queryType {
      name
    }
  }
}
```

Press the **Play** button (▶️) or `Ctrl+Enter` to execute.

**Result:**
```json
{
  "data": {
    "__schema": {
      "queryType": {
        "name": "GriotQuery"
      }
    }
  }
}
```

### Step 4: Try a Real Query (After Authentication)

Once Feature 07 is implemented, you can query actual data:

```graphql
query GetBoard {
  board(id: "ffffffff-ffff-ffff-ffff-ffffffffffff") {
    id
    name
    projectId
    createdAt
  }
}
```

### Step 5: Add Authentication Headers

When Feature 07 is done and you have a JWT token:

1. Look for the **"Headers"** or **"HTTP Headers"** section
2. Add a header:
   - **Key:** `Authorization`
   - **Value:** `Bearer YOUR_JWT_TOKEN_HERE`
3. Run your queries — they'll now be authenticated!

## 📋 Example Queries You Can Test

### Query 1: Introspection (Works NOW without auth)
```graphql
{
  __schema {
    queryType {
      name
      fields {
        name
        description
      }
    }
  }
}
```

### Query 2: Board Details (Requires auth — Feature 07)
```graphql
query {
  board(id: "ffffffff-ffff-ffff-ffff-ffffffffffff") {
    id
    name
    projectId
    createdAt
  }
}
```

### Query 3: Tasks with Pagination (Requires auth)
```graphql
query {
  tasks(boardId: "ffffffff-ffff-ffff-ffff-ffffffffffff", first: 5) {
    nodes {
      id
      title
      status
      priority
      dueDate
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

### Query 4: Tasks with DataLoaders (Tests N+1 prevention)
```graphql
query {
  tasks(boardId: "ffffffff-ffff-ffff-ffff-ffffffffffff", first: 10) {
    nodes {
      id
      title
      # DataLoader batches these efficiently
      assignee {
        id
        displayName
        email
      }
      # DataLoader batches comments too
      comments {
        nodes {
          body
          createdAt
          author {
            displayName
          }
        }
      }
    }
  }
}
```

### Mutation Example: Create a Task (Requires auth)
```graphql
mutation {
  createTask(
    boardId: "ffffffff-ffff-ffff-ffff-ffffffffffff"
    columnId: "14141414-1414-1414-1414-141414141414"
    title: "New task from GraphQL"
    description: "Testing mutations"
    priority: "Medium"
  ) {
    id
    title
    status
    createdAt
  }
}
```

## ⚠️ Current Limitation: Authentication

Right now, most queries will return:
```json
{
  "errors": [{
    "message": "The current user is not authorized to access this resource.",
    "extensions": {
      "code": "AUTH_NOT_AUTHENTICATED"
    }
  }]
}
```

**This is expected!** Authentication is implemented in **Feature 07** (JWT + Argon2 + Redis).

**What works NOW without auth:**
- ✅ Schema introspection (`__schema`, `__type` queries)
- ✅ SDL endpoint (`/graphql?sdl`)
- ✅ Exploring the schema in the UI

## 🆚 Banana Cake Pop vs Postman

| Feature | Banana Cake Pop | Postman |
|---------|----------------|---------|
| GraphQL-specific | ✅ Yes | ⚠️ Generic (works but not specialized) |
| Schema exploration | ✅ Built-in docs | ❌ Manual |
| Autocomplete | ✅ Yes | ⚠️ Limited |
| Query variables | ✅ Dedicated tab | ✅ Yes |
| Fragments | ✅ First-class | ⚠️ Manual |
| Real-time schema sync | ✅ Yes | ❌ No |
| Best for | GraphQL development | API collection management |

**Verdict:** Use **Banana Cake Pop** during development, use **Postman** for full API test suites (REST + GraphQL) in Feature 08.

## 🔧 Troubleshooting

### "I see a blank page or 404"
- **Fix:** Make sure you're visiting `http://localhost:5064/graphql/` with the **trailing slash**
- **Why:** The UI is served at `/graphql/`, the endpoint is at `/graphql`

### "The UI isn't loading"
- **Fix:** Restart the API server
  ```bash
  cd backend
  # If using dotnet watch:
  # Just save Program.cs to trigger hot reload
  # OR stop and restart:
  dotnet run --project src/Griot.Api
  ```

### "All my queries return AUTH_NOT_AUTHENTICATED"
- **Status:** Expected until Feature 07 is implemented
- **Timeline:** Feature 07 (JWT authentication) comes next
- **Workaround:** Use introspection queries for now (they work without auth)

### "I want to test with real data"
- **Step 1:** Wait for Feature 07 to be implemented (JWT auth)
- **Step 2:** Register a user via `/api/auth/register`
- **Step 3:** Login via `/api/auth/login` to get a JWT token
- **Step 4:** Add the token to Banana Cake Pop headers
- **Step 5:** Run your queries!

## 📚 Learning Resources

### HotChocolate Docs
- **Banana Cake Pop Guide:** https://chillicream.com/docs/bananacakepop
- **Query Syntax:** https://chillicream.com/docs/hotchocolate/v13/fetching-data/queries

### GraphQL Docs
- **Official Guide:** https://graphql.org/learn/queries/
- **Schema Language:** https://graphql.org/learn/schema/

## 🎯 Quick Reference

| What You Want | URL | Works Now? |
|---------------|-----|------------|
| Interactive UI | http://localhost:5064/graphql/ | ✅ Yes |
| Schema (SDL) | http://localhost:5064/graphql?sdl | ✅ Yes |
| Execute queries | http://localhost:5064/graphql | ⚠️ Auth required (Feature 07) |
| Browse docs | Use Banana Cake Pop UI | ✅ Yes |
| Test mutations | Use Banana Cake Pop UI | ⚠️ Auth required (Feature 07) |

---

## 🚀 Bottom Line

**YES, use the browser UI NOW!**

You can:
- ✅ Explore the complete schema
- ✅ See all 23 types, 11 queries, 11 mutations
- ✅ Test introspection queries
- ✅ Learn the GraphQL syntax
- ✅ Prepare your queries for when auth is ready

**You DON'T need to wait for Postman** — Banana Cake Pop is your GraphQL IDE for development!

Once Feature 07 (authentication) is done, you'll be able to:
- Execute all queries and mutations
- Test with real data from the seed script
- Debug DataLoader batching (N+1 prevention)
- Verify filtering and sorting
- Test pagination

**Pro tip:** Open Banana Cake Pop in one browser tab, and keep this guide in another! 🎉
