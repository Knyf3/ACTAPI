namespace ACTApi.DTOs;

/// <summary>DTO for ACT user tracking / muster data.</summary>
public class UserTrackDto
{
    /// <summary>User number.</summary>
    public int UserNumber { get; set; }

    /// <summary>User display name.</summary>
    public string UserName { get; set; } = "";

    /// <summary>Event descriptor (e.g. location status).</summary>
    public string EventDescriptor { get; set; } = "";

    /// <summary>Global door number where the event occurred.</summary>
    public int GlobalDoor { get; set; }

    /// <summary>Timestamp of the event.</summary>
    public DateTime When { get; set; }
}
