namespace ACTApi.DTOs;

/// <summary>DTO for a single door plan entry.</summary>
public class DoorPlanEntryDto
{
    /// <summary>Timezone number.</summary>
    public byte Timezone { get; set; }

    /// <summary>Door numbers in this plan.</summary>
    public List<int> Doors { get; set; } = new();
}

/// <summary>DTO for ACT door plan (timezone-based door access).</summary>
public class DoorPlanDto
{
    /// <summary>User number this plan belongs to.</summary>
    public int UserNumber { get; set; }

    /// <summary>List of door plan entries.</summary>
    public List<DoorPlanEntryDto> Plans { get; set; } = new();
}
