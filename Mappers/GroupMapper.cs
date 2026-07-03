using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for mapping between WCF group types and <see cref="GroupDto"/>.</summary>
public static class GroupMapper
{
    /// <summary>Converts a WCF DoorGroupValue to a GroupDto.</summary>
    public static GroupDto ToDto(this DoorGroupValue source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new GroupDto
        {
            Index = source.DoorGroupNumber,
            Name = source.Name ?? ""
        };
    }

    /// <summary>Converts a WCF UserGroupValue to a GroupDto.</summary>
    public static GroupDto ToDto(this UserGroupValue source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new GroupDto
        {
            Index = source.UserGroupNumber,
            Name = source.UserGroupName ?? ""
        };
    }
}
