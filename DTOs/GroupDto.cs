namespace ACTApi.DTOs;

/// <summary>DTO for ACT door or user group.</summary>
public class GroupDto
{
    /// <summary>Group index/number.</summary>
    public int Index { get; set; }

    /// <summary>Group name.</summary>
    public string Name { get; set; } = "";
}
