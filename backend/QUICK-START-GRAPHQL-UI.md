# 🚀 QUICK START — Use GraphQL RIGHT NOW in Your Browser!

## ✅ The GraphQL UI is NOW LIVE!

Open your browser and go to:

```
http://localhost:5064/graphql/
```

**⚠️ IMPORTANT: Note the trailing slash `/` — without it you'll get a blank page!**

---

## What You'll See

You'll see **Nitro IDE** (formerly called Banana Cake Pop) — HotChocolate's GraphQL IDE. It looks similar to Postman or Swagger, but it's specifically designed for GraphQL.

### The Interface Has 3 Main Areas:

1. **Left Panel** — Query editor where you type your GraphQL queries
2. **Right Panel** — Results appear here after you run a query
3. **Right Sidebar** — Schema documentation browser (click "Docs" or "Schema" tab)

---

## 🎯 What You Can Do RIGHT NOW (No Auth Required)

### 1. Explore the Schema
- Click the **"Schema"** or **"Docs"** button (usually top-right)
- Browse all 23 types (UserType, TaskItemType, BoardType, etc.)
- See all 11 queries and 11 mutations
- Click on any type to see its fields and descriptions

### 2. Run Introspection Queries

**Copy and paste this into the left panel:**

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

**Click the Play button (▶️) or press `Ctrl+Enter`**

You'll see a list of all available queries!

### 3. Check What Mutations Are Available

```graphql
{
  __schema {
    mutationType {
      name
      fields {
        name
        description
      }
    }
  }
}
```

### 4. Explore a Specific Type

```graphql
{
  __type(name: "TaskItemType") {
    name
    fields {
      name
      type {
        name
        kind
      }
    }
  }
}
```

---

## ⚠️ What WON'T Work Yet (Needs Feature 08 Auth)

If you try to query actual data, like:

```graphql
{
  board(id: "ffffffff-ffff-ffff-ffff-ffffffffffff") {
    id
    name
  }
}
```

You'll get:
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

**This is CORRECT and EXPECTED!** 

Authentication comes in Feature 08 (JWT + Argon2 + Redis). Once that's implemented:
1. You'll register/login to get a JWT token
2. Add the token to the "Headers" section in the UI
3. All queries and mutations will work!

---

## 🎨 Cool Features to Try

### Autocomplete
Start typing a query and press `Ctrl+Space` — you'll get suggestions!

```graphql
query {
  # Press Ctrl+Space here to see available queries
}
```

### Multiple Queries
You can write multiple queries and choose which one to run:

```graphql
query GetSchema {
  __schema {
    queryType { name }
  }
}

query GetTypes {
  __type(name: "TaskItemType") {
    name
  }
}
```

Use the dropdown next to the Play button to select which query to execute.

### Variables
Use variables for dynamic values:

**Query:**
```graphql
query GetBoard($boardId: UUID!) {
  board(id: $boardId) {
    id
    name
  }
}
```

**Variables (in the "Variables" tab at the bottom):**
```json
{
  "boardId": "ffffffff-ffff-ffff-ffff-ffffffffffff"
}
```

---

## 📊 Test Data Available (For When Auth is Ready)

The database has been seeded with:
- 4 users (owner, admin, member, guest)
- 2 workspaces
- 3 projects
- 6 tasks with comments
- 3 notifications

UUIDs you can use:
- **Board:** `ffffffff-ffff-ffff-ffff-ffffffffffff`
- **Workspace:** `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`
- **User (owner):** `11111111-1111-1111-1111-111111111111`

See `backend/src/Griot.Infrastructure/Sql/README-SEED-DATA.md` for full details.

---

## 🆚 This vs Postman

**Use the browser UI (Nitro IDE) for GraphQL development** because:
- ✅ Schema documentation is built-in
- ✅ Autocomplete knows your schema
- ✅ Syntax highlighting for GraphQL
- ✅ Better error messages
- ✅ GraphQL-specific features (fragments, directives, etc.)

**Use Postman (Feature 07) for:**
- Full API test suites (REST + GraphQL combined)
- Team collaboration (shared collections)
- CI/CD integration (Newman)

---

## 🐛 Troubleshooting

### "I see a blank page"
→ Make sure you're going to `http://localhost:5064/graphql/` **with the trailing slash**

### "The UI looks weird or isn't loading"
→ Refresh the page (F5) or hard refresh (Ctrl+Shift+R)

### "All queries return AUTH_NOT_AUTHENTICATED"
→ This is expected! Only introspection queries work before Feature 08

### "I want to test with real data NOW"
→ You need to wait for Feature 08 (JWT authentication) first

---

## ✅ Summary

**YES, go use it now!** You can:

1. ✅ Browse the complete GraphQL schema
2. ✅ See all 23 types, 11 queries, 11 mutations
3. ✅ Run introspection queries
4. ✅ Learn GraphQL syntax with autocomplete
5. ✅ Prepare queries for when auth is ready

**You DON'T need Postman yet** — this is your GraphQL IDE! 🎉

---

For more details, see `backend/GRAPHQL-BROWSER-UI-GUIDE.md`
