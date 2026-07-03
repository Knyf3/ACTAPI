using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Mappers;

/// <summary>Extension methods for mapping between <see cref="UserValueExt"/> and <see cref="UserDto"/>.</summary>
public static class UserMapper
{
    /// <summary>Converts a WCF UserValueExt to a UserDto.</summary>
    public static UserDto ToDto(this UserValueExt source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var cards = new List<uint>();
        if (source.LearnedCard != 0) cards.Add(source.LearnedCard);
        if (source.SiteCodedCard != 0) cards.Add(source.SiteCodedCard);
        if (source.OneToOneCard != 0) cards.Add(source.OneToOneCard);
        if (source.BatchCards is { Length: > 0 })
            cards.AddRange(source.BatchCards);
        if (source.Cards is { Length: > 0 })
            cards.AddRange(source.Cards);

        return new UserDto
        {
            UserNumber = source.UserNumber,
            Forename = source.Forename ?? "",
            Surname = source.Surname ?? "",
            Group = source.Group,
            GroupName = source.GroupName,
            Pin = source.Pin,
            Enabled = source.Enabled,
            CardType = ResolveCardType(source.CardType),
            Cards = cards,
            UserFields = source.UserFields?.ToList() ?? new List<string>(),
            EndValid = source.EndValid > DateTime.MinValue ? source.EndValid : null,
            HasPhoto = source.HasPhotograph,
            RecordCreated = source.RecordCreated > DateTime.MinValue ? source.RecordCreated : null,
            IsValid = source.IsValid
        };
    }

    /// <summary>Converts a UserDto to a WCF UserValueExt for create/update operations.</summary>
    public static UserValueExt ToWcf(this UserDto source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var userFields = source.UserFields.ToArray();
        if (userFields.Length < 10)
        {
            var padded = new string[10];
            for (int i = 0; i < 10; i++)
                padded[i] = i < userFields.Length ? userFields[i] : "";
            userFields = padded;
        }

        return new UserValueExt
        {
            UserNumber = source.UserNumber,
            Forename = source.Forename ?? "",
            Surname = source.Surname ?? "",
            Group = source.Group,
            GroupName = source.GroupName ?? "",
            Pin = source.Pin,
            Enabled = source.Enabled,
            UserFields = userFields,
            EndValid = source.EndValid ?? DateTime.MinValue,
            StartValid = DateTime.MinValue,
            IsValid = true,
            ExternallyModified = 0
        };
    }

    private static string ResolveCardType(uint cardTypeCode)
    {
        return cardTypeCode switch
        {
            0 => "Unknown",
            1 => "LearnedCards",
            2 => "OneToOneCards",
            3 => "SiteCodedCards",
            4 => "BatchCards",
            _ => $"Custom ({cardTypeCode})"
        };
    }
}
