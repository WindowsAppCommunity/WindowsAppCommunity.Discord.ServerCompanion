using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using WinAppCommunity.Discord.ServerCompanion.Keystore;

namespace WinAppCommunity.Discord.ServerCompanion.Autocomplete;

public class ProjectsAutoCompleteProvider(ProjectKeystore projects) : IAutocompleteProvider
{
    private readonly ProjectKeystore _projects = projects;

    public string Identity => "autocomplete::projects";

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = default)
    {
        var projects = _projects.ManagedProjects
            .Where(x => x.Project.Name?.Contains(userInput) ?? false)
            .Select(x => 
                new ApplicationCommandOptionChoice(
                    Name: x.Project.Name ?? throw new InvalidDataException(),
                    Value: x.IpnsCid.ToString())
            )
            .ToList();

        return new ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>>(projects);
    }
}