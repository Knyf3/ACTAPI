namespace ACTApi.DTOs;

/// <summary>DTO for ACT door data.</summary>
public class DoorDto
{
    /// <summary>Global door number (unique across all controllers).</summary>
    public int GlobalDoorNumber { get; set; }

    /// <summary>Door name/description.</summary>
    public string Name { get; set; } = "";

    /// <summary>Controller address this door belongs to.</summary>
    public int ControllerAddress { get; set; }

    /// <summary>Local door number on the controller.</summary>
    public int LocalDoorNumber { get; set; }

    /// <summary>Whether the door is enabled.</summary>
    public bool Enabled { get; set; }
}
