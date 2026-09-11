using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Griot.Application.Interfaces.Repositories;
using Griot.Infrastructure.Persistence;

namespace Griot.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly GriotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(GriotDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).ToListAsync();
    public async Task AddAsync(T entity)
    {
        // Spec 29, layer 3 (repository guard): tenant-scoped writes fail closed at
        // the save path even when a service forgets the check. Entities carrying
        // OrganizationId must match the active scope; observability rows with a
        // null org (platform-level events) are allowed through.
        AssertTenantOnWrite(entity, _context);
        await _dbSet.AddAsync(entity);
    }
    public void Update(T entity)
    {
        // Spec 29: same guard on updates — a tracked foreign-org row cannot be
        // persisted even if it was materialized before the scope was set.
        AssertTenantOnWrite(entity, _context);
        _dbSet.Update(entity);
    }
    public void Remove(T entity) => _dbSet.Remove(entity);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    private static void AssertTenantOnWrite(T entity, GriotDbContext context)
    {
        // SuperAdmin platform sessions bypass tenant scoping (spec 32/33).
        if (context.IsSuperAdmin)
            return;
        var property = typeof(T).GetProperty("OrganizationId");
        if (property is null || property.PropertyType != typeof(Guid))
        {
            // Nullable-tenant entities (ApiLogs/ErrorLogs/AuditLogs/ActivityLogs)
            // allow null = platform-level event; a stamped org must still match.
            if (property is not null && property.PropertyType == typeof(Guid?))
            {
                var stamped = (Guid?)property.GetValue(entity);
                if (stamped is not null && stamped != context.TenantId)
                    throw new Griot.Application.Services.DomainError(
                        Griot.Application.Services.DomainErrorKind.Forbidden, "cross-tenant");
            }
            return;
        }
        var organizationId = (Guid)property.GetValue(entity)!;
        if (context.TenantId is null || organizationId != context.TenantId)
            throw new Griot.Application.Services.DomainError(
                Griot.Application.Services.DomainErrorKind.Forbidden, "cross-tenant");
    }
}
