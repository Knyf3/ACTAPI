namespace ACTApi.Infrastructure;

/// <summary>
/// Generic pagination helper for WCF list operations that return arrays.
/// Handles the common "fetch page by page" pattern used by ACT's WCF API.
/// </summary>
public static class WcfPaginationHelper
{
    /// <summary>
    /// Fetch all results from a paged WCF operation by iterating start index.
    /// </summary>
    /// <typeparam name="T">The WCF value type (e.g. UserValueExt, DoorValueExt).</typeparam>
    /// <param name="fetchPage">Function that takes (startIndex, pageSize) and returns an array.</param>
    /// <param name="pageSize">Number of items per page (default 200).</param>
    /// <returns>Aggregated list of all items.</returns>
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
    /// Fetch a single page of results and return a paginated response.
    /// </summary>
    /// <typeparam name="T">The WCF value type.</typeparam>
    /// <typeparam name="TDto">The DTO type to map to.</typeparam>
    /// <param name="fetchPage">Function that takes (startIndex, pageSize) and returns an array.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="mapper">Function to map from T to TDto.</param>
    /// <returns>A paginated response with mapped data.</returns>
    public static async Task<DTOs.PaginatedResponse<TDto>> GetPageAsync<T, TDto>(
        Func<int, int, Task<T[]>> fetchPage,
        int page,
        int pageSize,
        Func<T, TDto> mapper)
    {
        int startIndex = (page - 1) * pageSize;

        // Fetch one extra to determine HasMore
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
