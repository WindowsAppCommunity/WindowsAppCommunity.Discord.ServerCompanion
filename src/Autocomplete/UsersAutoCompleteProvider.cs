using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using WinAppCommunity.Discord.ServerCompanion.Keystore;
using WinAppCommunity.Sdk.Models;

namespace WinAppCommunity.Discord.ServerCompanion.Autocomplete
{
    /// <inheritdoc />
    public class UserAutoCompleteProvider : IAutocompleteProvider
    {
        private readonly UserKeystore _users;

        public UserAutoCompleteProvider(UserKeystore users)
        {
            _users = users;
        }

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

        public string Identity { get; } = "autocomplete::users";
    }
}
