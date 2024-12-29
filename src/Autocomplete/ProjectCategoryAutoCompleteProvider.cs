using FuzzySharp;
using Humanizer;
using Polly;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;

namespace WindowsAppCommunity.Discord.ServerCompanion.Autocomplete
{
    /// <summary>
    /// Provides autocompletion against a dictionary of words.
    /// </summary>
    public class ProjectCategoryAutoCompleteProvider : IAutocompleteProvider
    {
        private readonly IInteractionContext _interactionContext;
        private readonly FeedbackService _feedbackService;
        private readonly IDiscordRestInteractionAPI _interactionAPI;

        public ProjectCategoryAutoCompleteProvider(IInteractionContext interactionContext, FeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI)
        {
            _interactionContext = interactionContext;
            _feedbackService = feedbackService;
            _interactionAPI = interactionAPI;
        }

        private readonly IReadOnlySet<string> _dictionary = new SortedSet<string>
        {
           "Books & reference",
           "Business",
            "Developer tools",
            "Education",
            "Entertainment",
            "Food & dining",
            "Government & politics",
            "Kids & family",
            "Lifestyle",
            "Medical",
            "Multimedia design",
            "Music",
            "Navigation & maps",
            "News & weather",
            "Personal finance",
            "Personalization",
            "Photo & video",
            "Productivity",
            "Security",
            "Shopping",
            "Social",
            "Sports",
            "Travel",
            "Utilities & tools"
        };

        /// <inheritdoc />
        public string Identity => "autocomplete::categoryDictionary";

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(IReadOnlyList<IApplicationCommandInteractionDataOption> options, string userInput, CancellationToken ct = default)
        {
            return new ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>>
            (
                _dictionary
                    .OrderByDescending(n => Fuzz.Ratio(userInput, n))
                    .Take(25)
                    .Select(n => new ApplicationCommandOptionChoice(n.Humanize().Transform(To.TitleCase), n))
                    .ToList()
            );
        }
    }
}
