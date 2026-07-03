namespace ACTApi.DTOs;

/// <summary>Generic paginated response wrapper.</summary>
public class PaginatedResponse<T>
{
    /// <summary>The items for this page.</summary>
    public List<T> Data { get; set; } = new();

    /// <summary>Total number of items matching the query.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Whether there are more pages available.</summary>
    public bool HasMore { get; set; }
}
