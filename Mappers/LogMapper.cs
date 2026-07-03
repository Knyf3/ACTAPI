using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for mapping between <see cref="LogValueExt"/> and <see cref="LogEventDto"/>.</summary>
public static class LogMapper
{
    /// <summary>Converts a WCF LogValueExt to a LogEventDto.</summary>
    public static LogEventDto ToDto(this LogValueExt source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return new LogEventDto
        {
            EventId = source.EventID,
            EventType = source.Event,
            Description = source.DisplayEventDescription ?? source.EventDescriptor ?? "",
            UserNumber = source.UserNumber,
            When = source.When,
            Door = source.Door,
            Controller = source.Controller
        };
    }
}
