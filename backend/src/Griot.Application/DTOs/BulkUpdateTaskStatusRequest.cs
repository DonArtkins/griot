using System;
using System.Collections.Generic;

namespace Griot.Application.DTOs;

public class BulkUpdateTaskStatusRequest
{
    public Guid WorkspaceId { get; set; }
    public List<Guid> TaskIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
