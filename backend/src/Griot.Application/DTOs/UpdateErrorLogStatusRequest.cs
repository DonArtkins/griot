using System;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs;

/// <summary>Spec 20 FixStatus lifecycle request for PATCH /api/logs/errors/{id}/fix-status.</summary>
public class UpdateErrorLogStatusRequest
{
    public ErrorFixStatus FixStatus { get; set; }
}
