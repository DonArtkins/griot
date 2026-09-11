using System;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Spec 20 (pipeline §3): AuditService queues AuditLogs/ActivityLogs rows correlated
/// to the producing request via IRequestContext — Before/After JSON, RequestId equal
/// to the client's X-Request-Id, ActivityId linking the user-facing activity row.
/// RecordAsync (auth events) commits its own transaction.
/// </summary>
public class ObservabilityAuditServiceTests
{
    private static readonly Guid RequestId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private static (AuditService Service, Mock<IGenericRepository<AuditLog>> Audit, Mock<IGenericRepository<ActivityLog>> Activity) Build()
    {
        var audit = new Mock<IGenericRepository<AuditLog>>();
        var activity = new Mock<IGenericRepository<ActivityLog>>();
        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupGet(c => c.RequestId).Returns(RequestId);
        return (new AuditService(audit.Object, activity.Object, requestContext.Object), audit, activity);
    }

    [Fact]
    public void QueueActivity_AddsRow_AndReturnsRowId()
    {
        var (service, _, activity) = Build();

        var rowId = service.QueueActivity(new ActivityEntry(WorkspaceId, ActorId, "Task", Guid.NewGuid(), "Created", null));

        Assert.NotEqual(Guid.Empty, rowId);
        activity.Verify(r => r.AddAsync(It.Is<ActivityLog>(a =>
            a.Id == rowId &&
            a.WorkspaceId == WorkspaceId &&
            a.ActorId == ActorId &&
            a.Action == "Created")), Times.Once);
    }

    [Fact]
    public void QueueAudit_StampsRequestId_AndActivityLink()
    {
        var (service, audit, _) = Build();
        var activityId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        service.QueueAudit(new AuditEntry(ActorId, "Task.Updated", "Task", entityId,
            Before: "{\"title\":\"Old\"}", After: "{\"title\":\"New\"}", ActivityId: activityId));

        audit.Verify(r => r.AddAsync(It.Is<AuditLog>(a =>
            a.RequestId == RequestId &&                       // ERD amendment: same X-Request-Id the client saw
            a.ActivityId == activityId &&
            a.ActorId == ActorId &&
            a.Action == "Task.Updated" &&
            a.EntityType == "Task" &&
            a.EntityId == entityId &&
            a.Before == "{\"title\":\"Old\"}" &&
            a.After == "{\"title\":\"New\"}" &&
            a.CreatedAt <= DateTime.UtcNow)), Times.Once);
        audit.Verify(r => r.SaveChangesAsync(), Times.Never); // commits with the caller's SaveChanges
    }

    [Fact]
    public async Task RecordAsync_AuthEvent_CommitsItsOwnTransaction()
    {
        var (service, audit, _) = Build();

        await service.RecordAsync(new AuditEntry(ActorId, "Auth.Login", "User", ActorId, null, null));

        audit.Verify(r => r.SaveChangesAsync(), Times.Once); // durable without a domain write
    }

    [Fact]
    public void Snapshot_CycleSafeEntity_ReturnsJson()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Old" };

        var json = AuditService.Snapshot(task);

        Assert.NotNull(json);
        Assert.Contains("\"Title\":\"Old\"", json);
    }

    [Fact]
    public void Snapshot_Failure_ReturnsNull_NeverThrows()
    {
        var poison = new PoisonEntity(); // property getter throws

        Assert.Null(AuditService.Snapshot(poison));
    }

    private sealed class PoisonEntity
    {
        public string Title => throw new InvalidOperationException("boom");
    }
}
