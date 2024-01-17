using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

[Group("commands")]
public class SampleCommandGroup : CommandGroup
{
    private readonly IInteractionContext _interactionContext;
    private readonly IFeedbackService _feedbackService;
    private readonly IDiscordRestInteractionAPI _interactionAPI;

    public SampleCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI)
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
