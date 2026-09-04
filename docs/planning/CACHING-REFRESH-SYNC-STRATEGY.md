# Caching, Refresh & Offline/Online Sync Strategy — Griot

> Comprehensive strategy for client-side caching (web/mobile), refresh patterns, offline tolerance, and online sync. Covers all three systems: backend Redis, web Apollo/TanStack, and mobile GraphQL/sqflite.

**Status:** Planning → Implementation  
**Priority:** Phase 1 (dashboard caching, refresh) + Phase 3 (mobile offline queue)  
**Cross-system:** backend, web, mobile

---

## 1. Overview: Three Caching Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT TIER                             │
│  ┌──────────────────┐              ┌─────────────────────────┐  │
│  │  Web (Browser)   │              │  Mobile (Flutter)       │  │
│  │  ───────────────  │              │  ─────────────────────  │  │
│  │  Apollo Cache    │              │  GraphQL Cache          │  │
│  │  (GraphQL reads) │              │  (GraphQL reads)        │  │
│  │                  │              │                         │  │
│  │  TanStack Query  │              │  Dio Cache (REST)       │  │
│  │  (REST, 30s)     │              │  (attachments, auth)    │  │
│  │                  │              │                         │  │
│  │  Zustand         │              │  Riverpod               │  │
│  │  (accessToken)   │              │  (accessToken)          │  │
│  └────────┬─────────┘              └─────────┬───────────────┘  │
│           │                                  │                  │
│           │ GraphQL/REST over HTTPS          │                  │
│           ▼                                  ▼                  │
└───────────────────────────────────────────────────────────────┘
            │                                  │
            ▼                                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                       BACKEND TIER (Railway)                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Redis 7 (Phase 1 + Phase 2)                             │  │
│  │  ────────────────────────────────────────────────────────  │  │
│  │  • Rate limiting (sliding window, per IP+email)          │  │
│  │  • Refresh token metadata (rotation family tracking)     │  │
│  │  • AI token budgets (per workspace)                      │  │
│  │  • Dashboard summary cache (60s TTL) ← Phase 1           │  │
│  │  • GraphQL response cache (board queries) ← Phase 2      │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SQL Server 2022 (source of truth)                       │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    OFFLINE PERSISTENCE (Phase 3)                │
│  ┌──────────────────┐              ┌─────────────────────────┐  │
│  │  Web (v1: none)  │              │  Mobile (sqflite)       │  │
│  │  Phase 3: option │              │  ─────────────────────  │  │
│  │  apollo-cache-   │              │  • Queued writes table  │  │
│  │  persist         │              │  • idempotencyKey (PK)  │  │
│  │  (localStorage)  │              │  • operation (JSON)     │  │
│  │                  │              │  • createdAt, syncedAt  │  │
│  └──────────────────┘              └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Backend Redis Caching (Phase 1 + Phase 2)

### 2.1 Current Redis usage (baseline)
| Key pattern | Purpose | TTL | Size estimate |
|---|---|---|---|
| `rate:{ip}:{email}` | Login rate limiting (5 attempts/min) | 60s | ~100 bytes |
| `refresh:{userId}` | Refresh token metadata (family tracking) | 7 days | ~200 bytes |
| `ai:budget:{workspaceId}` | AI token budget tracking | 24 hours | ~50 bytes |

**Total baseline:** ~10 MB for 10k sessions (see CAPACITY-PLAN.md §4)

### 2.2 Phase 1: Dashboard summary caching

**Implementation:**
```csharp
// backend/Griot.Application/Services/DashboardService.cs
public async Task<DashboardSummary> GetSummaryAsync(Guid workspaceId, CancellationToken ct)
{
    var cacheKey = $"dashboard:summary:{workspaceId}";
    
    // Try cache first
    var cached = await _redis.StringGetAsync(cacheKey);
    if (cached.HasValue)
    {
        _logger.LogInformation("Dashboard cache hit for workspace {WorkspaceId}", workspaceId);
        return JsonSerializer.Deserialize<DashboardSummary>(cached);
    }
    
    // Cache miss → query database
    var summary = await _db.QueryFirstAsync<DashboardSummary>(
        "EXEC usp_GetDashboardSummary @workspaceId",
        new { workspaceId }
    );
    
    // Store in cache with 60s TTL
    await _redis.StringSetAsync(
        cacheKey,
        JsonSerializer.Serialize(summary),
        TimeSpan.FromSeconds(60)
    );
    
    return summary;
}
```

**Invalidation strategy:**
- **Time-based (TTL):** 60 seconds (acceptable staleness for dashboard counts)
- **Event-based (optional Phase 2):** Purge on workspace-level writes (task create/update/delete, project create)
  - Subscribe to domain events: `TaskCreated`, `TaskUpdated`, `TaskDeleted`, `ProjectCreated`
  - On event: `await _redis.KeyDeleteAsync($"dashboard:summary:{workspaceId}")`

**Cache warming (optional):**
- Pre-populate cache on user login: `POST /api/auth/login` → async fire-and-forget dashboard query
- Benefit: First dashboard load after login is always a cache hit

**Metrics:**
- Expected cache hit ratio: **90%+** (users refresh dashboard frequently within 60s window)
- Impact: p95 dashboard latency **400ms → 100ms** (75% improvement)

### 2.3 Phase 2: GraphQL response caching

**Implementation:**
```csharp
// backend/Griot.Api/Program.cs
services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddQueryCachePipeline(options =>
    {
        options.UseRedis = true;
        options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    })
    .AddRedisQueryStorage(sp => sp.GetRequiredService<IConnectionMultiplexer>());

// Opt-in per resolver
[UseQueryCache(Duration = 300)] // 5 minutes
public async Task<Board> GetBoardAsync(Guid id, [Service] IBoardService service)
{
    return await service.GetByIdAsync(id);
}
```

**Cache key strategy:**
```
graphql:board:{boardId}:{userId}:{timestamp_rounded_to_minute}
```
- `boardId` → different boards cached separately
- `userId` → user-specific (respects workspace membership)
- `timestamp_rounded_to_minute` → cache entries grouped by minute for efficient TTL management

**Invalidation strategy:**
- **Time-based (TTL):** 5 minutes (hot boards stay cached)
- **Event-based (Phase 2 refinement):** Purge on board writes
  - On `TaskCreated`, `TaskUpdated`, `TaskMoved`: `await _redis.KeyDeleteAsync($"graphql:board:{boardId}:*")`
  - Use wildcard delete or maintain a set of active cache keys per board

**Metrics:**
- Expected cache hit ratio: **70–80%** (users viewing same board within 5-min window)
- Impact: p95 board read latency **200ms → 10ms** (95% improvement on cache hit)
- Redis memory: 100 hot boards × 2 MB each = **200 MB** (Phase 2 capacity estimate)

### 2.4 Redis failure handling

**Policy: Fail-open for caching, fail-closed for rate limiting**

```csharp
// Caching (fail-open — degrade gracefully)
try
{
    var cached = await _redis.StringGetAsync(cacheKey);
    if (cached.HasValue) return Deserialize(cached);
}
catch (RedisException ex)
{
    _logger.LogWarning(ex, "Redis cache read failed, falling back to database");
    // Continue to database query (cache miss behavior)
}

// Rate limiting (fail-closed — deny on Redis failure)
try
{
    var allowed = await _rateLimiter.IsAllowedAsync(ip, email);
    if (!allowed) return StatusCode(429, "Too many requests");
}
catch (RedisException ex)
{
    _logger.LogError(ex, "Redis rate limiter unavailable");
    return StatusCode(503, "Service temporarily unavailable");
}
```

**Rationale:**
- **Caching failure:** Degrade to slower performance (acceptable)
- **Rate limiting failure:** Deny requests (security > availability)

See `docs/planning/RISK-REGISTER.md` Risk #1 for full policy.

---

## 3. Web Client Caching (React 18 + Apollo + TanStack Query)

### 3.1 Apollo InMemoryCache (GraphQL reads)

**Configuration:**
```typescript
// web/src/lib/apollo-client.ts
const cache = new InMemoryCache({
  typePolicies: {
    Query: {
      fields: {
        board: {
          // Cache by boardId, separate entry per board
          keyArgs: ['id'],
        },
        tasks: {
          // Cache by filters (status, priority, assignee)
          keyArgs: ['boardId', 'status', 'priority', 'assigneeId'],
          // Merge strategy: replace (don't append pagination results)
          merge: false,
        },
      },
    },
    Task: {
      fields: {
        // Prevent reorder bugs: always replace position
        position: { merge: false },
      },
    },
  },
});

const client = new ApolloClient({
  uri: `${API_URL}/graphql`,
  cache,
  defaultOptions: {
    watchQuery: {
      fetchPolicy: 'cache-and-network', // Show cached, then update
      errorPolicy: 'all',
    },
    query: {
      fetchPolicy: 'cache-first', // Prefer cache for one-time queries
      errorPolicy: 'all',
    },
  },
});
```

**Cache invalidation (mutations):**
```typescript
// web/src/features/tasks/mutations/useCreateTask.ts
const [createTask] = useMutation(CREATE_TASK, {
  update(cache, { data }) {
    // Invalidate board query to refetch tasks
    cache.evict({ fieldName: 'board', args: { id: boardId } });
    cache.gc(); // Garbage collect evicted entries
  },
  onCompleted() {
    toast.success('Task created');
  },
});
```

**Optimistic updates (drag-and-drop):**
```typescript
// web/src/features/boards/mutations/useMoveTask.ts
const [moveTask] = useMutation(MOVE_TASK, {
  optimisticResponse: {
    moveTask: {
      __typename: 'Task',
      id: taskId,
      columnId: targetColumnId,
      position: targetPosition,
    },
  },
  update(cache, { data }) {
    // Update local cache immediately (before server confirms)
    cache.modify({
      id: cache.identify({ __typename: 'Task', id: taskId }),
      fields: {
        columnId: () => targetColumnId,
        position: () => targetPosition,
      },
    });
  },
});
```

**Offline behavior (v1 — online-first):**
- Apollo cache persists **in memory only** (no localStorage)
- If offline → GraphQL queries fail → show cached data + error toast
- Writes fail → user sees error, must retry manually when online

**Phase 3 enhancement (apollo-cache-persist):**
```typescript
// Optional: Persist Apollo cache to localStorage for offline reads
import { persistCache } from 'apollo3-cache-persist';

await persistCache({
  cache,
  storage: window.localStorage,
  maxSize: 5 * 1024 * 1024, // 5 MB limit
});
```
- Benefit: Dashboard/boards visible offline (read-only)
- Deferred to Phase 3 (web is online-first for v1)

### 3.2 TanStack Query (REST data)

**Configuration:**
```typescript
// web/src/lib/query-client.ts
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,        // Data fresh for 30 seconds
      cacheTime: 5 * 60 * 1000, // Keep in cache for 5 minutes
      retry: 1,                 // Retry once on failure
      refetchOnWindowFocus: true, // Refetch on tab focus
    },
    mutations: {
      retry: 0, // Don't retry mutations (avoid duplicates)
    },
  },
});
```

**Auth refresh interceptor:**
```typescript
// web/src/lib/api-client.ts
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    
    // If 401 and not already retried, try refresh
    if (error.response?.status === 401 && !originalRequest._retried) {
      originalRequest._retried = true;
      
      try {
        // POST /api/auth/refresh (httpOnly cookie sent automatically)
        await axios.post(`${API_URL}/api/auth/refresh`);
        
        // Retry original request with new access token
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed → redirect to login
        authStore.clearAuth();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    
    return Promise.reject(error);
  }
);
```

**Cache invalidation (mutations):**
```typescript
// web/src/features/tasks/mutations/useUpdateTask.ts
const updateTask = useMutation(
  (data) => apiClient.patch(`/api/tasks/${data.id}`, data),
  {
    onSuccess: (_, variables) => {
      // Invalidate affected queries
      queryClient.invalidateQueries(['board', variables.boardId]);
      queryClient.invalidateQueries(['task', variables.id]);
      toast.success('Task updated');
    },
  }
);
```

### 3.3 Zustand (client-only UI state)

**Purpose:** Ephemeral state that should NEVER mirror server data

```typescript
// web/src/stores/auth-store.ts
interface AuthState {
  accessToken: string | null;
  setAccessToken: (token: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  setAccessToken: (token) => set({ accessToken: token }),
  clearAuth: () => {
    set({ accessToken: null });
    
    // Security: Clear all cached data on logout/account switch
    // Apollo client cache
    import('@/lib/apollo-client').then(({ apolloClient }) => {
      apolloClient.clearStore(); // Clears cache + refetches active queries
    });
    
    // TanStack Query cache
    import('@tanstack/react-query').then(({ useQueryClient }) => {
      const queryClient = useQueryClient();
      queryClient.clear(); // Clears all queries and mutations
    });
  },
}));

// web/src/stores/ui-store.ts
interface UiState {
  isSidebarOpen: boolean;
  activeFilters: TaskFilters;
  dragState: DragState | null;
}

export const useUiStore = create<UiState>((set) => ({
  isSidebarOpen: true,
  activeFilters: { status: null, priority: null },
  dragState: null,
  // ... actions
}));
```

**Contract violation check:**
```typescript
// ❌ WRONG — mirroring server data in Zustand
const useBoardStore = create((set) => ({
  boards: [], // Server data should be in Apollo/TanStack only
}));

// ✅ RIGHT — client-only UI state
const useUiStore = create((set) => ({
  selectedBoardId: null, // UI selection state, not board data
}));
```

---

## 4. Mobile Client Caching + Offline Sync (Flutter 3.19+)

### 4.1 GraphQL cache (graphql_flutter)

**Configuration:**
```dart
// mobile/lib/core/network/graphql_client.dart
final cache = GraphQLCache(store: InMemoryStore());

final client = GraphQLClient(
  link: HttpLink('$apiUrl/graphql'),
  cache: cache,
  defaultPolicies: DefaultPolicies(
    query: Policies(
      fetch: FetchPolicy.cacheAndNetwork, // Show cached, then update
      error: ErrorPolicy.all,
    ),
    mutate: Policies(
      fetch: FetchPolicy.networkOnly, // Always hit server for writes
    ),
  ),
);
```

**Optimistic updates:**
```dart
// mobile/lib/features/tasks/mutations/move_task_mutation.dart
final result = await client.mutate(
  MutationOptions(
    document: gql(moveTaskMutation),
    variables: {
      'taskId': taskId,
      'columnId': targetColumnId,
      'position': targetPosition,
    },
    optimisticResult: {
      'moveTask': {
        '__typename': 'Task',
        'id': taskId,
        'columnId': targetColumnId,
        'position': targetPosition,
      },
    },
  ),
);
```

### 4.2 Dio cache (REST + attachments)

**Configuration:**
```dart
// mobile/lib/core/network/dio_client.dart
final dio = Dio(BaseOptions(
  baseUrl: apiUrl,
  connectTimeout: const Duration(seconds: 10),
  receiveTimeout: const Duration(seconds: 10),
));

// Add auth interceptor
dio.interceptors.add(
  InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await _secureStorage.read(key: 'accessToken');
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
      handler.next(options);
    },
    onError: (error, handler) async {
      // Handle 401 → refresh → retry
      if (error.response?.statusCode == 401) {
        final refreshed = await _refreshToken();
        if (refreshed) {
          return handler.resolve(await _retry(error.requestOptions));
        } else {
          // Refresh failed → logout
          await _authProvider.logout();
        }
      }
      handler.next(error);
    },
  ),
);
```

### 4.3 Offline queue persistence (Phase 3 — sqflite)

**Purpose:** Queue writes when offline, sync when online

**Database schema (SQLite local):**
```sql
CREATE TABLE IF NOT EXISTS offline_queue (
  idempotency_key TEXT PRIMARY KEY,
  operation_type TEXT NOT NULL,  -- 'createTask', 'updateTask', 'moveTask', etc.
  operation_data TEXT NOT NULL,  -- JSON payload
  workspace_id TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  synced_at INTEGER,
  retry_count INTEGER DEFAULT 0,
  last_error TEXT
);

CREATE INDEX idx_offline_queue_synced 
  ON offline_queue(synced_at) 
  WHERE synced_at IS NULL;
```

**Queue write (offline):**
```dart
// mobile/lib/core/storage/offline_queue.dart
class OfflineQueue {
  final Database _db;
  
  Future<void> enqueue(OfflineOperation operation) async {
    await _db.insert(
      'offline_queue',
      {
        'idempotency_key': operation.idempotencyKey, // UUID v4
        'operation_type': operation.type,
        'operation_data': jsonEncode(operation.data),
        'workspace_id': operation.workspaceId,
        'created_at': DateTime.now().millisecondsSinceEpoch,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }
  
  Future<List<OfflineOperation>> getPending() async {
    final result = await _db.query(
      'offline_queue',
      where: 'synced_at IS NULL',
      orderBy: 'created_at ASC',
    );
    return result.map((row) => OfflineOperation.fromMap(row)).toList();
  }
}
```

**Sync on reconnect:**
```dart
// mobile/lib/core/network/sync_service.dart
class SyncService {
  Future<void> syncPendingOperations() async {
    final pending = await _offlineQueue.getPending();
    
    for (final operation in pending) {
      try {
        // Send to backend with idempotency key header
        final response = await _dio.post(
          operation.endpoint,
          data: operation.data,
          options: Options(
            headers: {'X-Idempotency-Key': operation.idempotencyKey},
          ),
        );
        
        if (response.statusCode == 200 || response.statusCode == 201) {
          // Success → mark synced
          await _offlineQueue.markSynced(operation.idempotencyKey);
        }
      } on DioException catch (e) {
        if (e.response?.statusCode == 409) {
          // Conflict → server already processed (idempotent)
          await _offlineQueue.markSynced(operation.idempotencyKey);
        } else if (e.response?.statusCode == 400) {
          // Validation error → surface to user for resolution
          await _offlineQueue.markFailed(
            operation.idempotencyKey,
            e.response?.data['message'],
          );
        } else {
          // Network error → retry later
          await _offlineQueue.incrementRetry(operation.idempotencyKey);
        }
      }
    }
  }
}
```

**Connectivity monitoring:**
```dart
// mobile/lib/core/network/connectivity_service.dart
class ConnectivityService {
  final connectivity = Connectivity();
  
  void init() {
    connectivity.onConnectivityChanged.listen((result) {
      if (result != ConnectivityResult.none) {
        // Came online → trigger sync
        _syncService.syncPendingOperations();
      }
    });
  }
}
```

**Conflict resolution UI:**
```dart
// mobile/lib/features/tasks/widgets/conflict_dialog.dart
class ConflictDialog extends StatelessWidget {
  final OfflineOperation localOperation;
  final dynamic serverState;
  
  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Sync Conflict'),
      content: Column(
        children: [
          Text('Your change: ${localOperation.summary}'),
          Text('Server state: ${serverState.summary}'),
          Text('This task was modified by another user while you were offline.'),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => _resolveKeepLocal(),
          child: Text('Keep My Version'),
        ),
        TextButton(
          onPressed: () => _resolveAcceptServer(),
          child: Text('Accept Server Version'),
        ),
        TextButton(
          onPressed: () => _resolveDiscard(),
          child: Text('Discard My Change'),
        ),
      ],
    );
  }
}
```

**User-facing offline indicator:**
```dart
// mobile/lib/shared/widgets/offline_banner.dart
class OfflineBanner extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return StreamBuilder<ConnectivityResult>(
      stream: Connectivity().onConnectivityChanged,
      builder: (context, snapshot) {
        if (snapshot.data == ConnectivityResult.none) {
          return Container(
            color: Colors.orange,
            padding: EdgeInsets.all(8),
            child: Row(
              children: [
                Icon(Icons.wifi_off),
                SizedBox(width: 8),
                Text('Offline - Changes will sync when connection restores'),
              ],
            ),
          );
        }
        return SizedBox.shrink();
      },
    );
  }
}
```

### 4.4 Offline NFR requirements (from NFR.md §9)

**Persistence contract:**
- ✅ Queued writes persist until operation succeeds or user explicitly discards
- ✅ Never silently clear on failure
- ✅ Each write carries client-generated `idempotencyKey` (UUID v4)
- ✅ Backend deduplicates on this key (replayed requests are safe)

**Conflict handling:**
- ✅ On 409 conflict, surface resolution prompt with 3 choices:
  1. Keep local version
  2. Accept server version
  3. Discard local change
- ✅ User decides (no automatic merge)

**Battery awareness:**
- ❌ No always-on WebSocket (drains battery)
- ✅ Refresh on app focus (check for updates when user returns to app)
- ✅ Pull-to-refresh gesture (manual sync trigger)
- ✅ Sync on connectivity restore (automatic background sync)

---

## 5. Refresh Token Rotation & Session Management

### 5.1 Refresh flow (backend)

**Token storage:**
```
Access token:  JWT (15-min expiry), stored in memory (web: Zustand, mobile: Riverpod)
Refresh token: Opaque 256-bit token, SHA-256 hash stored in SQL Server RefreshTokens table
               Sent as httpOnly cookie (web) or secure storage (mobile)
```

**Rotation on refresh:**
```csharp
// backend/Griot.Application/Services/AuthService.cs
public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct)
{
    // 1. Hash incoming token
    var tokenHash = ComputeSHA256(refreshToken);
    
    // 2. Find in database (check not revoked)
    var existingToken = await _db.RefreshTokens
        .Where(t => t.TokenHash == tokenHash && t.RevokedAt == null)
        .FirstOrDefaultAsync(ct);
    
    if (existingToken == null || existingToken.ExpiresAt < DateTime.UtcNow)
    {
        throw new UnauthorizedException("Invalid or expired refresh token");
    }
    
    // 3. Check for reuse (replay attack detection)
    if (existingToken.ReplacedByTokenId != null)
    {
        // This token was already rotated → revoke entire family
        await RevokeTokenFamilyAsync(existingToken.FamilyId, ct);
        throw new SecurityException("Token reuse detected");
    }
    
    // 4. Generate new token pair
    var newAccessToken = GenerateJWT(existingToken.UserId);
    var newRefreshToken = GenerateSecureToken();
    var newTokenHash = ComputeSHA256(newRefreshToken);
    
    // 5. Store new refresh token (same family)
    var newTokenRow = new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = existingToken.UserId,
        TokenHash = newTokenHash,
        FamilyId = existingToken.FamilyId, // Preserve family
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
    };
    await _db.RefreshTokens.AddAsync(newTokenRow, ct);
    
    // 6. Mark old token as replaced (not revoked)
    existingToken.ReplacedByTokenId = newTokenRow.Id;
    await _db.SaveChangesAsync(ct);
    
    return new AuthResult(newAccessToken, newRefreshToken);
}
```

**Family-based revocation (on reuse detection):**
```csharp
private async Task RevokeTokenFamilyAsync(Guid familyId, CancellationToken ct)
{
    var familyTokens = await _db.RefreshTokens
        .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
        .ToListAsync(ct);
    
    foreach (var token in familyTokens)
    {
        token.RevokedAt = DateTime.UtcNow;
    }
    
    await _db.SaveChangesAsync(ct);
    _logger.LogWarning("Revoked token family {FamilyId} due to reuse detection", familyId);
}
```

### 5.2 Silent refresh (web)

**On app boot:**
```typescript
// web/src/App.tsx
useEffect(() => {
  const silentRefresh = async () => {
    try {
      // POST /api/auth/refresh (httpOnly cookie sent automatically)
      const response = await axios.post(`${API_URL}/api/auth/refresh`);
      const { accessToken } = response.data;
      authStore.setAccessToken(accessToken);
    } catch (error) {
      // Refresh failed → user must log in
      authStore.clearAuth();
      navigate('/login');
    }
  };
  
  silentRefresh();
}, []);
```

**Proactive refresh (15 min → 14.5 min):**
```typescript
// web/src/hooks/useProactiveRefresh.ts
useEffect(() => {
  if (!accessToken) return;
  
  // Decode JWT to get expiry
  const decoded = jwtDecode(accessToken);
  const expiresIn = decoded.exp * 1000 - Date.now();
  const refreshAt = expiresIn - 30_000; // 30 seconds before expiry
  
  const timer = setTimeout(async () => {
    try {
      const response = await axios.post(`${API_URL}/api/auth/refresh`);
      authStore.setAccessToken(response.data.accessToken);
    } catch (error) {
      authStore.clearAuth();
      navigate('/login');
    }
  }, refreshAt);
  
  return () => clearTimeout(timer);
}, [accessToken]);
```

### 5.3 Refresh on mobile (flutter_secure_storage)

**Storage:**
```dart
// mobile/lib/core/storage/secure_storage.dart
class SecureStorage {
  final storage = FlutterSecureStorage();
  
  Future<void> saveTokens(String accessToken, String refreshToken) async {
    await storage.write(key: 'accessToken', value: accessToken);
    await storage.write(key: 'refreshToken', value: refreshToken);
  }
  
  Future<String?> getAccessToken() => storage.read(key: 'accessToken');
  Future<String?> getRefreshToken() => storage.read(key: 'refreshToken');
}
```

**Refresh interceptor:**
```dart
// mobile/lib/core/network/dio_client.dart
onError: (error, handler) async {
  if (error.response?.statusCode == 401) {
    final refreshToken = await _secureStorage.getRefreshToken();
    if (refreshToken == null) {
      await _authProvider.logout();
      return handler.next(error);
    }
    
    try {
      final response = await _dio.post('/api/auth/refresh', data: {
        'refreshToken': refreshToken,
      });
      
      final newAccessToken = response.data['accessToken'];
      final newRefreshToken = response.data['refreshToken'];
      await _secureStorage.saveTokens(newAccessToken, newRefreshToken);
      
      // Retry original request with new token
      error.requestOptions.headers['Authorization'] = 'Bearer $newAccessToken';
      return handler.resolve(await _dio.fetch(error.requestOptions));
    } catch (e) {
      // Refresh failed → logout
      await _authProvider.logout();
      return handler.next(error);
    }
  }
  handler.next(error);
},
```

---

## 6. Implementation Checklist

### 6.1 Phase 1 (production blockers)
- [ ] **Backend Redis dashboard caching:**
  - [ ] Add `DashboardService.GetSummaryAsync` with Redis cache (60s TTL)
  - [ ] Unit tests: cache hit/miss, invalidation, Redis failure (fall back to DB)
  - [ ] Integration tests: verify p95 <100ms on cache hit

- [ ] **Web Apollo configuration:**
  - [ ] Configure `typePolicies` (Task.position merge: false)
  - [ ] Add optimistic updates for drag-and-drop
  - [ ] Auth refresh interceptor with `_retried` guard

- [ ] **Mobile GraphQL + Dio setup:**
  - [ ] Configure GraphQL cache (cache-and-network policy)
  - [ ] Dio auth interceptor with refresh retry
  - [ ] flutter_secure_storage for token persistence

### 6.2 Phase 2 (post-k6)
- [ ] **Backend GraphQL response caching:**
  - [ ] Add `.AddQueryCachePipeline().AddRedisQueryStorage()`
  - [ ] Board query caching (5-min TTL)
  - [ ] Event-based invalidation on task writes
  - [ ] Measure cache hit ratio (expect 70–80%)

### 6.3 Phase 3 (post-bootcamp)
- [ ] **Mobile offline queue (sqflite):**
  - [ ] Create offline_queue table schema
  - [ ] `OfflineQueue` service (enqueue, getPending, markSynced)
  - [ ] `SyncService` (sync on reconnect)
  - [ ] Connectivity monitoring (trigger sync on online)
  - [ ] Conflict resolution UI (3 choices: keep local, accept server, discard)
  - [ ] Offline banner component

- [ ] **Web apollo-cache-persist (optional):**
  - [ ] Persist Apollo cache to localStorage (5 MB limit)
  - [ ] Read-only offline mode (dashboard/boards visible offline)

---

## 7. Testing Strategy

### 7.1 Backend caching tests
```csharp
[Fact]
public async Task GetSummaryAsync_CacheHit_ReturnsFromRedis()
{
    // Arrange: pre-populate Redis cache
    var cached = JsonSerializer.Serialize(new DashboardSummary { ... });
    await _redis.StringSetAsync("dashboard:summary:workspace1", cached);
    
    // Act
    var result = await _service.GetSummaryAsync(workspace1Id, ct);
    
    // Assert
    Assert.NotNull(result);
    _dbMock.Verify(db => db.QueryFirstAsync(...), Times.Never); // DB not hit
}

[Fact]
public async Task GetSummaryAsync_CacheMiss_QueriesDatabaseAndCaches()
{
    // Arrange: empty Redis cache
    _redisMock.Setup(r => r.StringGetAsync(...)).ReturnsAsync(RedisValue.Null);
    
    // Act
    var result = await _service.GetSummaryAsync(workspace1Id, ct);
    
    // Assert
    _dbMock.Verify(db => db.QueryFirstAsync(...), Times.Once);
    _redisMock.Verify(r => r.StringSetAsync(..., ..., TimeSpan.FromSeconds(60)), Times.Once);
}
```

### 7.2 Web Apollo tests
```typescript
// web/src/features/boards/__tests__/useBoardQuery.test.ts
it('shows cached board data immediately, then updates from network', async () => {
  const cache = new InMemoryCache();
  cache.writeQuery({
    query: GET_BOARD,
    data: { board: mockBoard },
  });
  
  const { result } = renderHook(() => useBoardQuery(boardId), {
    wrapper: ({ children }) => (
      <ApolloProvider client={client}>{children}</ApolloProvider>
    ),
  });
  
  // First render: cache hit
  expect(result.current.data).toEqual(mockBoard);
  expect(result.current.loading).toBe(false);
  
  // Network response arrives (cache-and-network policy)
  await waitFor(() => {
    expect(result.current.data).toEqual(updatedBoard);
  });
});
```

### 7.3 Mobile offline queue tests
```dart
// mobile/test/core/storage/offline_queue_test.dart
test('enqueue and sync pending operations', () async {
  final queue = OfflineQueue(db);
  
  // Enqueue operation while offline
  await queue.enqueue(OfflineOperation(
    idempotencyKey: 'uuid-1',
    type: 'createTask',
    data: {'title': 'Test task'},
    workspaceId: 'workspace-1',
  ));
  
  // Verify pending
  final pending = await queue.getPending();
  expect(pending.length, 1);
  expect(pending.first.idempotencyKey, 'uuid-1');
  
  // Mark synced
  await queue.markSynced('uuid-1');
  
  // Verify no longer pending
  final remaining = await queue.getPending();
  expect(remaining.length, 0);
});
```

---

## 8. Monitoring & Metrics

### 8.1 Backend Redis metrics (Netdata + custom)
| Metric | Target | Alert threshold |
|---|---|---|
| Redis cache hit ratio (dashboard) | >90% | <70% |
| Redis cache hit ratio (GraphQL) | >70% | <50% |
| Redis memory usage | <500 MB | >1 GB |
| Dashboard p95 latency (cache hit) | <100ms | >200ms |
| Board read p95 latency (cache hit) | <10ms | >50ms |

### 8.2 Web client metrics (Vercel Analytics)
| Metric | Target | Alert threshold |
|---|---|---|
| Apollo cache size | <10 MB | >50 MB |
| Cache eviction rate | <10% | >30% |
| 401 → refresh → retry success rate | >95% | <80% |

### 8.3 Mobile offline metrics (custom tracking)
| Metric | Target | Alert threshold |
|---|---|---|
| Offline queue size | <10 pending | >100 pending |
| Sync success rate | >95% | <80% |
| Conflict resolution rate | <5% | >20% |

---

## 9. Open Questions & Future Enhancements

### Q1: Should web persist Apollo cache to localStorage for offline reads?
**Answer:** Not for v1 (web is online-first). Defer to Phase 3 if user requests offline dashboard access.

### Q2: Should mobile queue have size limits (prevent unbounded growth)?
**Answer:** Yes. Add max 1,000 pending operations limit. If limit reached, show warning: "Too many offline changes. Please connect to sync."

### Q3: Should backend cache dashboard summary per user or per workspace?
**Answer:** Per workspace (shared cache). Dashboard shows workspace-level counts (not user-specific data).

### Q4: What happens if user makes conflicting changes on two devices while offline?
**Answer:** Last-write-wins on server. Conflict resolution UI only shows when server rejects due to state mismatch (e.g., task already moved by another user).

---

## 10. References

- **NFR offline requirements:** `docs/planning/NFR.md` §9
- **Optimization roadmap:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §4 (caching), §6 (mobile offline)
- **Capacity planning:** `docs/planning/CAPACITY-PLAN.md` §4 (Redis memory estimates)
- **Web state management:** `web/project-kit/context/state-and-data.md`
- **Mobile architecture:** `mobile/project-kit/context/architecture.md`
- **Apollo cache docs:** https://www.apollographql.com/docs/react/caching/cache-configuration/
- **TanStack Query docs:** https://tanstack.com/query/latest/docs/react/guides/caching
- **sqflite docs:** https://pub.dev/packages/sqflite

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
_Comprehensive caching, refresh, and offline/online sync strategy across all three tiers: backend Redis, web Apollo/TanStack, mobile GraphQL/sqflite._
