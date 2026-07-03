namespace ACTApi.DTOs;

/// <summary>DTO for ACT user data.</summary>
public class UserDto
{
    /// <summary>The ACT user number (primary key).</summary>
    public int UserNumber { get; set; }

    /// <summary>User's first name.</summary>
    public string Forename { get; set; } = "";

    /// <summary>User's last name.</summary>
    public string Surname { get; set; } = "";

    /// <summary>User group number.</summary>
    public int Group { get; set; }

    /// <summary>User group name.</summary>
    public string? GroupName { get; set; }

    /// <summary>User PIN code.</summary>
    public int Pin { get; set; }

    /// <summary>Whether the user is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Card type descriptor (resolved from uint).</summary>
    public string CardType { get; set; } = "";

    /// <summary>List of card numbers assigned to this user.</summary>
    public List<uint> Cards { get; set; } = new();

    /// <summary>Custom user-defined fields (up to 10).</summary>
    public List<string> UserFields { get; set; } = new();

    /// <summary>Date when the user record becomes invalid (null if valid indefinitely).</summary>
    public DateTime? EndValid { get; set; }

    /// <summary>Whether the user has a photo on file.</summary>
    public bool HasPhoto { get; set; }

    /// <summary>Date when the record was created.</summary>
    public DateTime? RecordCreated { get; set; }

    /// <summary>Whether the underlying WCF record is valid.</summary>
    public bool IsValid { get; set; }
}
