using Polly;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Embeds;
using Remora.Discord.Interactivity;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

[Group("commands")]
public class SampleCommandGroup : CommandGroup
{
    private readonly IInteractionContext _interactionContext;
    private readonly FeedbackService _feedbackService;
    private readonly IDiscordRestInteractionAPI _interactionAPI;

    public SampleCommandGroup(IInteractionContext interactionContext, FeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI)
    {
        _interactionContext = interactionContext;
        _feedbackService = feedbackService;
        _interactionAPI = interactionAPI;
    }

    /// <summary>
    /// Displays an embed with a user-supplied word.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("display-word")]
    public async Task<Result> DisplayWord([AutocompleteProvider("autocomplete::dictionary")] string word)
    {
        return (Result)await _feedbackService.SendContextualNeutralAsync
        (
            $"Your word is \"{word}\".",
            ct: this.CancellationToken
        );
    }

    [Command("do-thing")]
    public async Task<IResult> MyCommand()
    {
        return Result.FromSuccess();
    }

    [Command("test-pages")]
    public async Task<IResult> MyCommand(int numberOfPages)
    {
        try
        {
            var discordId = _interactionContext.Interaction.Member.Value.User.Value.ID;
            var embeds = new List<Embed>();

            for (var i = 0; i < numberOfPages; i++)
            {
                var builder = new EmbedBuilder().WithTitle($"Page {i}");
                embeds.Add(builder.Build().Entity);
            }

            await _feedbackService.SendContextualPaginatedMessageAsync(discordId, embeds);
            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }

    /// <summary>
    /// Sends an embed with a dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("dropdown")]
    public async Task<IResult> SendDropdownAsync()
    {
        var options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
        {
            new ActionRowComponent(new IMessageComponent[]
            {
                new StringSelectComponent
                (
                    CustomIDHelpers.CreateSelectMenuID("colour-dropdown"),
                    new ISelectOption[]
                    {
                        new SelectOption("Red", "#FF0000"),
                        new SelectOption("Green", "#00FF00"),
                        new SelectOption("Blue", "#0000FF"),
                        new SelectOption("Cyan", "#00FFFF"),
                        new SelectOption("Magenta", "#FF00FF"),
                        new SelectOption("Yellow", "#FFFF00"),
                        new SelectOption("Black", "#000000"),
                        new SelectOption("White", "#FFFFFF")
                    },
                    "Colours...",
                    1,
                    1
                )
            }),

            new ActionRowComponent(new IMessageComponent[]
            {
                new ButtonComponent(Label: "Test", Style: ButtonComponentStyle.Primary, CustomID: CustomIDHelpers.CreateButtonID("test-button"))
            }),

            new ActionRowComponent(new IMessageComponent[]
            {
                new TextInputComponent(
                        "text-input",
                        TextInputStyle.Short,
                        "Short Text",
                        1,
                        32,
                        true,
                        default,
                        "Short Text here")
            })
        });

        var res = await _feedbackService.SendContextualAsync(options: options, ct: this.CancellationToken);

        return res;
    }

    [Command("test-interactivity")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> TestInteractivity()
    {
        try
        {
            var response = new InteractionResponse
           (
               InteractionCallbackType.Modal,
               new
               (
                   new InteractionModalCallbackData
                   (
                       CustomIDHelpers.CreateModalID("modal"),
                       "Test Modal",
                       new[]
                       {
                        new ActionRowComponent
                        (
                            new[]
                            {
                                new TextInputComponent
                                (
                                    "modal-text-input",
                                    TextInputStyle.Short,
                                    "Short Text",
                                    1,
                                    32,
                                    true,
                                    default,
                                    "Short Text here"
                                )
                            }
                        )
                       }
                   )
               )
           );

            var result = await _interactionAPI.CreateInteractionResponseAsync
            (
                _interactionContext.Interaction.ID,
                _interactionContext.Interaction.Token,
                response,
                ct: this.CancellationToken
            );

            if (!result.IsSuccess)
            {
                await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{result.Error}");
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}
