namespace ACTApi.Infrastructure;

/// <summary>
/// Generic pagination helper for WCF list operations that return arrays.
/// Supports two modes:
///   1. Simple index-based: cursor advances by page count (e.g., GetUsers)
///   2. Cursor-based: cursor advances by a domain key (e.g., GetDoors by GlobalDoorNumber)
/// </summary>
public static class WcfPaginationHelper
{
    /// <summary>
    /// Fetch all results from a paged WCF operation, advancing the cursor
    /// by the page size. Use this for methods where start is a sequential index.
    /// </summary>
    public static async Task<List<T>> GetAllAsync<T>(
        Func<int, int, Task<T[]>> fetchPage,
        int pageSize = 200)
    {
        var results = new List<T>();
        int startIndex = 0;
        T[] page;

        do
        {
            page = await fetchPage(startIndex, pageSize);
            if (page == null || page.Length == 0)
                break;

            results.AddRange(page);
            startIndex += page.Length;
        } while (page.Length == pageSize);

        return results;
    }

    /// <summary>
    /// Fetch all results from a paged WCF operation, advancing the cursor
    /// using a user-defined selector function applied to the last item in each page.
    /// Use this for methods where the WCF API uses a domain key as cursor
    /// (e.g., GetDoorsAsync advances by GlobalDoorNumber).
    /// </summary>
    public static async Task<List<T>> GetAllAsync<T>(
        Func<int, int, Task<T[]>> fetchPage,
        Func<T, int> cursorSelector,
        int pageSize = 200)
    {
        var results = new List<T>();
        int cursor = 0;
        T[] page;

        do
        {
            page = await fetchPage(cursor, pageSize);
            if (page == null || page.Length == 0)
                break;

            results.AddRange(page);
            cursor = cursorSelector(page[^1]) + 1;
        } while (page.Length == pageSize);

        return results;
    }

    /// <summary>
    /// Fetch a single page and return a paginated response.
    /// NOTE: This uses index-based pagination (page * pageSize).
    /// For cursor-based WCF methods (like GetDoorsAsync), use
    /// GetAllAsync + in-memory Skip/Take instead.
    /// </summary>
    public static async Task<DTOs.PaginatedResponse<TDto>> GetPageAsync<T, TDto>(
        Func<int, int, Task<T[]>> fetchPage,
        int page,
        int pageSize,
        Func<T, TDto> mapper)
    {
        int startIndex = (page - 1) * pageSize;

        T[] pageResults = await fetchPage(startIndex, pageSize + 1);

        if (pageResults == null || pageResults.Length == 0)
        {
            return new DTOs.PaginatedResponse<TDto>
            {
                Data = new List<TDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                HasMore = false
            };
        }

        bool hasMore = pageResults.Length > pageSize;
        var items = pageResults.Take(pageSize).Select(mapper).ToList();

        return new DTOs.PaginatedResponse<TDto>
        {
            Data = items,
            TotalCount = startIndex + items.Count,
            Page = page,
            PageSize = pageSize,
            HasMore = hasMore
        };
    }
}
