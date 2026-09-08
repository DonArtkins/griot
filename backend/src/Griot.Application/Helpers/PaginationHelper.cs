namespace Griot.Application.Helpers;

public static class PaginationHelper
{
    public const int MaxPageSize = 1000;
    public const int DefaultPageSize = 100;

    public static int ValidatePageSize(int? pageSize)
    {
        if (pageSize == null || pageSize <= 0)
            return DefaultPageSize;

        if (pageSize > MaxPageSize)
            return MaxPageSize;

        return pageSize.Value;
    }

    public static int ValidatePageNumber(int? pageNumber)
    {
        if (pageNumber == null || pageNumber < 1)
            return 1;

        return pageNumber.Value;
    }
}
