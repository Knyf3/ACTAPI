namespace ACTApi.DTOs;

/// <summary>Request model for issuing door commands.</summary>
public class DoorCommandRequest
{
    /// <summary>Single global door number to issue the command on.</summary>
    public int? GlobalDoorNumber { get; set; }

    /// <summary>List of global door numbers to issue the command on.</summary>
    public List<int>? GlobalDoorNumbers { get; set; }
}
