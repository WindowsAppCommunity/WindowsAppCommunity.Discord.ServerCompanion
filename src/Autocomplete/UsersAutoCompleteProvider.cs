using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using WindowsAppCommunity.Discord.ServerCompanion.Keystore;
using WindowsAppCommunity.Sdk.Models;

namespace WindowsAppCommunity.Discord.ServerCompanion.Autocomplete;

/// <inheritdoc />
public class UserAutoCompleteProvider(UserKeystore users) : IAutocompleteProvider
{
    private readonly UserKeystore _users = users;

    public string Identity { get; } = "autocomplete::users";

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = default)
    {
        var users = _users.ManagedUsers
            .Where(x => x.User.Name?.Contains(userInput) ?? false)
            .Select(x => 
                new ApplicationCommandOptionChoice(
                    Name: x.User.Name ?? throw new InvalidDataException(),
                    Value: x.IpnsCid.ToString())
            )
            .ToList();

        return new ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>>(users);
    }
}
