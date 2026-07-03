using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for mapping between <see cref="DoorValueExt"/> and <see cref="DoorDto"/>.</summary>
public static class DoorMapper
{
    /// <summary>Converts a WCF DoorValueExt to a DoorDto.</summary>
    public static DoorDto ToDto(this DoorValueExt source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new DoorDto
        {
            GlobalDoorNumber = source.GlobalDoorNumber,
            Name = source.Name ?? "",
            ControllerAddress = source.ControllerAddress,
            LocalDoorNumber = source.LocalDoorNumber,
            Enabled = source.Enabled
        };
    }
}
