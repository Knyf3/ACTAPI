namespace ACTApi.DTOs;

/// <summary>Search/filter parameters for user queries.</summary>
public class UserSearchRequest
{
    /// <summary>Filter by forename (partial match).</summary>
    public string? Forename { get; set; }

    /// <summary>Filter by surname (partial match).</summary>
    public string? Surname { get; set; }

    /// <summary>Filter by user group number.</summary>
    public int? Group { get; set; }

    /// <summary>Filter by card number.</summary>
    public uint? CardNumber { get; set; }

    /// <summary>Filter by enabled status.</summary>
    public bool? Enabled { get; set; }

    /// <summary>Page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Page size.</summary>
    public int PageSize { get; set; } = 200;
}
