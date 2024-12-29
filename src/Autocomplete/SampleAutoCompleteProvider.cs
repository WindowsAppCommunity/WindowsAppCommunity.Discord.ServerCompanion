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
    public class SampleAutoCompleteProvider : IAutocompleteProvider
    {
        private readonly IInteractionContext _interactionContext;
        private readonly FeedbackService _feedbackService;
        private readonly IDiscordRestInteractionAPI _interactionAPI;

        public SampleAutoCompleteProvider(IInteractionContext interactionContext, FeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI)
        {
            _interactionContext = interactionContext;
            _feedbackService = feedbackService;
            _interactionAPI = interactionAPI;
        }

        private readonly IReadOnlySet<string> _dictionary = new SortedSet<string>
        {
            "a",
            "adipiscing",
            "aliquam",
            "amet",
            "at",
            "condimentum",
            "congue",
            "consectetur",
            "curabitur",
            "dapibus",
            "diam",
            "dolor",
            "egestas",
            "eget",
            "eleifend",
            "elit",
            "et",
            "finibus",
            "iaculis",
            "in",
            "ipsum",
            "lectus",
            "libero",
            "lorem",
            "nam",
            "nec",
            "neque",
            "nisl",
            "nullam",
            "nunc",
            "odio",
            "orci",
            "porta",
            "posuere",
            "quam",
            "quis",
            "semper",
            "sit",
            "sollicitudin",
            "tempor",
            "tempus",
            "ultricies",
            "velit",
            "venenatis",
            "vestibulum",
            "vitae"
        };

        /// <inheritdoc />
        public string Identity => "autocomplete::dictionary";

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
        (
            IReadOnlyList<IApplicationCommandInteractionDataOption> options,
            string userInput,
            CancellationToken ct = default
        )
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
