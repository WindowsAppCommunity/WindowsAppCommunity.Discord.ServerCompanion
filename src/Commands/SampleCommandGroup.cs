using Polly;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
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

            await _feedbackService.SendContextualPaginatedMessageAsync(discordId, embeds, new());
            return Result.FromSuccess();
        }
        catch (Exception ex)
        {
            await _feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
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
