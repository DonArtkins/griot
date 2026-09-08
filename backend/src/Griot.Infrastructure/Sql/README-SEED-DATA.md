# Seed Data for GraphQL Testing

## Overview

This directory contains `seed-test-data.sql` which populates the Griot database with test data for development and testing purposes.

## Running the Seed Script

```bash
# From the backend directory
cd backend
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot \
  < src/Griot.Infrastructure/Sql/seed-test-data.sql
```

## Test Data Created

### Users (4 total)
- **owner@griot.test** - Owner User (`11111111-1111-1111-1111-111111111111`)
- **admin@griot.test** - Admin User (`22222222-2222-2222-2222-222222222222`)
- **member@griot.test** - Member User (`33333333-3333-3333-3333-333333333333`)
- **guest@griot.test** - Guest User (`44444444-4444-4444-4444-444444444444`)

*Note: Passwords are hashed with Argon2. In production, users should be created via `/api/auth/register`.*

### Workspaces
- **Engineering Team** (`AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA`)
  - Owner: owner@griot.test
  - Members: all 4 users with different roles

- **Marketing Team** (`BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB`)
  - Owner: owner@griot.test

### Projects (3 in Engineering Team)
- **Backend API** (`CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC`, Key: BE)
- **Web Frontend** (`DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD`, Key: WEB)
- **Mobile App** (`EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE`, Key: MOB)

### Boards
- **Sprint 1** (Backend API) (`FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF`)
  - Columns: Backlog, In Progress, Review, Done
  - 6 tasks distributed across columns

### Tasks (6 total in Sprint 1 board)
1. **Implement GraphQL schema** (In Progress, High priority)
2. **Add DataLoaders for N+1 prevention** (In Progress, High priority)
3. **Write unit tests for services** (Todo, Medium priority)
4. **Setup JWT authentication** (Todo, High priority)
5. **Code review for GraphQL implementation** (In Progress, Medium priority)
6. **Deploy to staging** (Done, High priority)

### Comments (3 total)
- 2 comments on "Implement GraphQL schema" task
- 1 comment on "Add DataLoaders" task

### Notifications (3 total)
- 2 unread notifications for admin@griot.test
- 1 read notification for member@griot.test

### Activity Logs (3 total)
- Project creation, task creation, and task status updates

## Testing GraphQL Queries

The GraphQL endpoint is available at: **http://localhost:5064/graphql**

### View the Schema (SDL)
```bash
curl 'http://localhost:5064/graphql?sdl'
```

### Important Note on Authentication

Most GraphQL queries require authentication (JWT bearer token). Feature spec 07 implements authentication. Until then, you can:

1. **Browse the schema** using the SDL endpoint (no auth required)
2. **Use Banana Cake Pop or GraphQL Playground** to explore the schema
3. **Test unauthenticated queries** (if any are marked as `[AllowAnonymous]`)

### Example Queries (Once Authentication is Implemented)

#### Query a Board
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

#### Query Tasks with Pagination
```graphql
query {
  tasks(boardId: "ffffffff-ffff-ffff-ffff-ffffffffffff", first: 10) {
    nodes {
      id
      title
      status
      priority
      assigneeId
      dueDate
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}
```

#### Query Tasks with DataLoaders (N+1 Prevention Test)
```graphql
query {
  tasks(boardId: "ffffffff-ffff-ffff-ffff-ffffffffffff", first: 10) {
    nodes {
      id
      title
      assignee {
        id
        displayName
        email
      }
      comments {
        nodes {
          body
          author {
            displayName
          }
        }
      }
    }
  }
}
```

## Accessing Banana Cake Pop (HotChocolate GraphQL IDE)

HotChocolate includes a built-in GraphQL IDE called Banana Cake Pop.

Visit: **http://localhost:5064/graphql/**

Features:
- Schema explorer
- Query editor with autocomplete
- Query execution
- Schema documentation
- HTTP header management (for JWT tokens)

## Clearing Test Data

To clear all data and re-run the seed script:

```bash
# The seed script already includes DELETE statements at the beginning
# Just re-run it:
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot \
  < src/Griot.Infrastructure/Sql/seed-test-data.sql
```

## Production Warning

⚠️ **This seed data is for LOCAL DEVELOPMENT ONLY.**

Never run this script against:
- Staging environments
- Production databases
- Any database with real user data

The script contains DELETE statements that will wipe all data from multiple tables.
