using System.Collections.Generic;

namespace Griot.Application.DTOs;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 || TotalCount <= 0
        ? 0
        : (int)(((long)TotalCount + PageSize - 1) / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
