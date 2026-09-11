using System;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Unit tests for the attachment limits enforced by
/// <see cref="DomainService.CreateAttachmentAsync"/> (spec 11 / api-surface):
/// SizeBytes must be within the inclusive 0–25 MB range and MimeType must match
/// the API's allowlist (images jpeg/png/gif/webp, PDF, .docx, .xlsx) before any
/// attachment record is persisted.
/// </summary>
public class AttachmentValidationTests
{
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private static (DomainService Service, Mock<IGenericRepository<Attachment>> Attachments) BuildService()
    {
        var task = new TaskItem { Id = TaskId, ColumnId = Guid.NewGuid() };
        var column = new Column { Id = task.ColumnId, BoardId = Guid.NewGuid() };
        var board = new Board { Id = column.BoardId, ProjectId = Guid.NewGuid() };
        var project = new Project { Id = board.ProjectId, WorkspaceId = WorkspaceId };
        var workspace = new Workspace { Id = WorkspaceId, OwnerId = UserId };

        var users = new Mock<IGenericRepository<User>>();
        var workspaces = new Mock<IGenericRepository<Workspace>>();
        workspaces.Setup(r => r.GetByIdAsync(WorkspaceId)).ReturnsAsync(workspace);
        var members = new Mock<IGenericRepository<WorkspaceMember>>();
        var invites = new Mock<IGenericRepository<Invite>>();
        var projects = new Mock<IGenericRepository<Project>>();
        projects.Setup(r => r.GetByIdAsync(board.ProjectId)).ReturnsAsync(project);
        var boards = new Mock<IGenericRepository<Board>>();
        boards.Setup(r => r.GetByIdAsync(column.BoardId)).ReturnsAsync(board);
        var columns = new Mock<IGenericRepository<Column>>();
        columns.Setup(r => r.GetByIdAsync(task.ColumnId)).ReturnsAsync(column);
        var tasks = new Mock<IGenericRepository<TaskItem>>();
        tasks.Setup(r => r.GetByIdAsync(TaskId)).ReturnsAsync(task);
        var comments = new Mock<IGenericRepository<Comment>>();
        var attachments = new Mock<IGenericRepository<Attachment>>();
        var notifications = new Mock<IGenericRepository<Notification>>();
        var activity = new Mock<IGenericRepository<ActivityLog>>();
        var errors = new Mock<IGenericRepository<ErrorLog>>();
        var audit = new Mock<IGenericRepository<AuditLog>>();

        var service = new DomainService(
            users.Object, workspaces.Object, members.Object, invites.Object,
            projects.Object, boards.Object, columns.Object, tasks.Object,
            comments.Object, attachments.Object, notifications.Object,
            activity.Object, errors.Object, audit.Object,
            Mock.Of<IAuditService>());

        return (service, attachments);
    }

    private static CreateAttachmentRequest Request(long sizeBytes = 1024, string mimeType = "application/pdf") =>
        new() { FileName = "report.pdf", MimeType = mimeType, SizeBytes = sizeBytes };

    [Fact]
    public async Task CreateAsync_WithValidMetadata_PersistsAttachment()
    {
        var (service, attachments) = BuildService();

        var dto = await service.CreateAttachmentAsync(TaskId, Request(), UserId);

        Assert.NotNull(dto);
        Assert.Equal(1024, dto!.SizeBytes);
        attachments.Verify(r => r.AddAsync(It.IsAny<Attachment>()), Times.Once);
        attachments.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(26_214_401)] // 25 MB + 1 byte
    public async Task CreateAsync_WithOutOfRangeSize_ThrowsValidation(long sizeBytes)
    {
        var (service, attachments) = BuildService();

        var ex = await Assert.ThrowsAsync<DomainError>(
            () => service.CreateAttachmentAsync(TaskId, Request(sizeBytes), UserId));

        Assert.Equal(DomainErrorKind.Validation, ex.Kind);
        attachments.Verify(r => r.AddAsync(It.IsAny<Attachment>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]              // lower bound inclusive
    [InlineData(26_214_400)]     // upper bound inclusive (25 MB)
    public async Task CreateAsync_WithBoundarySize_PersistsAttachment(long sizeBytes)
    {
        var (service, attachments) = BuildService();

        var dto = await service.CreateAttachmentAsync(TaskId, Request(sizeBytes), UserId);

        Assert.Equal(sizeBytes, dto!.SizeBytes);
        attachments.Verify(r => r.AddAsync(It.IsAny<Attachment>()), Times.Once);
    }

    [Theory]
    [InlineData("application/x-msdownload")]   // .exe
    [InlineData("application/x-bat")]          // .bat
    [InlineData("text/javascript")]
    [InlineData("video/mp4")]
    [InlineData("image/svg+xml")]              // script-capable vector format
    public async Task CreateAsync_WithDisallowedMimeType_ThrowsValidation(string mimeType)
    {
        var (service, attachments) = BuildService();

        var ex = await Assert.ThrowsAsync<DomainError>(
            () => service.CreateAttachmentAsync(TaskId, Request(mimeType: mimeType), UserId));

        Assert.Equal(DomainErrorKind.Validation, ex.Kind);
        attachments.Verify(r => r.AddAsync(It.IsAny<Attachment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MimeTypeIsCaseInsensitive()
    {
        var (service, _) = BuildService();

        var dto = await service.CreateAttachmentAsync(TaskId, Request(mimeType: "APPLICATION/PDF"), UserId);

        Assert.NotNull(dto);
    }

    [Fact]
    public void AttachmentLimits_MatchDocumentedAllowlist()
    {
        var expected = new[]
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        Assert.Equal(expected.OrderBy(x => x), DomainService.AllowedAttachmentMimeTypes.OrderBy(x => x));
        Assert.Equal(26_214_400, DomainService.MaxAttachmentSizeBytes);
    }
}