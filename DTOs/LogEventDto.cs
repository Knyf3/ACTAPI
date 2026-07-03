namespace ACTApi.DTOs;

/// <summary>DTO for ACT log events.</summary>
public class LogEventDto
{
    /// <summary>Unique event ID.</summary>
    public int EventId { get; set; }

    /// <summary>Event type code.</summary>
    public uint EventType { get; set; }

    /// <summary>Human-readable event description.</summary>
    public string Description { get; set; } = "";

    /// <summary>Associated user number.</summary>
    public int UserNumber { get; set; }

    /// <summary>Timestamp of the event.</summary>
    public DateTime When { get; set; }

    /// <summary>Door number associated with the event.</summary>
    public int Door { get; set; }

    /// <summary>Controller number associated with the event.</summary>
    public int Controller { get; set; }
}
