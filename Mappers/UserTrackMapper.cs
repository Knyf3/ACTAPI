using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for mapping between <see cref="UserTrackValueExt"/> and <see cref="UserTrackDto"/>.</summary>
public static class UserTrackMapper
{
    /// <summary>Converts a WCF UserTrackValueExt to a UserTrackDto.</summary>
    public static UserTrackDto ToDto(this UserTrackValueExt source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new UserTrackDto
        {
            UserNumber = source.UserNumber,
            UserName = source.UserName ?? "",
            EventDescriptor = source.EventDescriptor ?? "",
            GlobalDoor = source.GlobalDoor,
            When = source.When
        };
    }
}
