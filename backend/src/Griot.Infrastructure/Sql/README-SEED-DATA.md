# Seed Data for GraphQL Testing

## Overview

This directory contains `seed-test-data.sql` which populates the Griot database with test data for development and testing purposes.

## Running the Seed Script

```bash
# From the backend directory
cd backend
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SABABISHA_SA_PASSWORD" -C -d Griot \
  < src/Griot.Infrastructure/Sql/seed-test-data.sql
```

## Test Data Created

### Organization (1 — spec 29 Pool model)
- **Sababisha Solutions** (`88888888-8888-8888-8888-888888888888`, slug `sababisha`, plan `Free`)
  - Owner: owner@griot.test (org role `Owner`)
  - Members: admin@griot.test (`Admin`), member@griot.test (`ProjectManager`), guest@griot.test (`Client`)

### Users (4 total)
- **owner@griot.test** - Owner User (`11111111-1111-1111-1111-111111111111`, PlatformRole `SuperAdmin`)
- **admin@griot.test** - Admin User (`22222222-2222-2222-2222-222222222222`, PlatformRole `User`)
- **member@griot.test** - Member User (`33333333-3333-3333-3333-333333333333`, PlatformRole `User`)
- **guest@griot.test** - Guest User (`44444444-4444-4444-4444-444444444444`, PlatformRole `User`)

*Note: Passwords are DUMMY Argon2 strings — these accounts cannot log in. Register real
users via `/api/auth/register`. The `owner@griot.test` PlatformRole `SuperAdmin` makes the
seeded tenant data visible to a platform-admin JWT; every tenant row carries
`OrganizationId = 88888888-8888-8888-8888-888888888888`.*

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

### Tasks (4 total in Sprint 1 board, all org-stamped)
1. **Implement GraphQL schema** (In Progress, High priority, assignee admin@griot.test)
2. **Add DataLoaders for N+1 prevention** (In Progress, High priority, assignee admin@griot.test)
3. **Write unit tests for services** (Todo, Medium priority, assignee member@griot.test)
4. **Set up CI pipeline** (Done, Low priority, assignee admin@griot.test)

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
  -S localhost -U sa -P "$SABABISHA_SA_PASSWORD" -C -d Griot \
  < src/Griot.Infrastructure/Sql/seed-test-data.sql
```

## Production Warning

⚠️ **This seed data is for LOCAL DEVELOPMENT ONLY.**

Never run this script against:
- Staging environments
- Production databases
- Any database with real user data

The script contains DELETE statements that will wipe all data from multiple tables.
