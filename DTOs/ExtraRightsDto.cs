namespace ACTApi.DTOs;

/// <summary>DTO for a single extra right entry.</summary>
public class ExtraRightEntryDto
{
    /// <summary>Door group number.</summary>
    public int DoorGroup { get; set; }

    /// <summary>Timezone number.</summary>
    public int Timezone { get; set; }

    /// <summary>Validity start date.</summary>
    public DateTime ValidityFrom { get; set; }

    /// <summary>Validity end date.</summary>
    public DateTime ValidityTo { get; set; }
}

/// <summary>DTO for ACT extra rights (door group / timezone overrides).</summary>
public class ExtraRightsDto
{
    /// <summary>User number these rights belong to.</summary>
    public int UserNumber { get; set; }

    /// <summary>List of extra rights (up to 16 slots).</summary>
    public List<ExtraRightEntryDto> Rights { get; set; } = new();
}
