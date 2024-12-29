using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using WindowsAppCommunity.Discord.ServerCompanion.Keystore;

namespace WindowsAppCommunity.Discord.ServerCompanion.Autocomplete;

public class PublishersAutoCompleteProvider(PublisherKeystore publishers) : IAutocompleteProvider
{
    private readonly PublisherKeystore _publishers = publishers;

    public string Identity => "autocomplete::publishers";

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = default)
    {
        var publishers = _publishers.ManagedPublishers
            .Where(x => x.Publisher.Name?.Contains(userInput) ?? false)
            .Select(x => 
                new ApplicationCommandOptionChoice(
                    Name: x.Publisher.Name ?? throw new InvalidDataException(),
                    Value: x.IpnsCid.ToString())
            )
            .ToList();

        return new ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>>(publishers);
    }
}
