using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using WinAppCommunity.Discord.ServerCompanion.Commands.Errors;

namespace WinAppCommunity.Discord.ServerCompanion;

[Group("commands")]
public class ProjectCommandGroup : CommandGroup
{
    private readonly IInteractionContext _interactionContext;
    private readonly IFeedbackService _feedbackService;

    private readonly IDiscordRestInteractionAPI _interactionAPI;

    public ProjectCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI)
    {
        _interactionContext = interactionContext;
        _feedbackService = feedbackService;
        _interactionAPI = interactionAPI;
    }

    [Command("register-project")]
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
                       "Register User",
                       new[]
                       {
                        new ActionRowComponent
                        (
                            new[]
                            {
                                new TextInputComponent
                                (
                                    "project-name",
                                    TextInputStyle.Short,
                                    "Project Name",
                                    1,
                                    32,
                                    true,
                                    default,
                                    "Project name here"
                                ),
                            }
                        ),
                        new ActionRowComponent(
                            new []{

                                 new TextInputComponent
                                (
                                    "project-description",
                                    TextInputStyle.Paragraph,
                                    "Project Description",
                                    1,
                                    2000,
                                    true,
                                    default,
                                    "Project description here"
                                ),
                            }
                        ),
                        new ActionRowComponent(
                            new[]
                                {
                              new TextInputComponent
                                (
                                    "project-description2",
                                    TextInputStyle.Paragraph,
                                    "Project Description",
                                    1,
                                    2000,
                                    true,
                                    default,
                                    "Project description here"
                                )
                             })
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
    [Command("dropdown")]
    public async Task<IResult> SendDropdownAsync()
    {
        var embed = new Embed(Description: "Select a colour below.");
        var options = new FeedbackMessageOptions(MessageComponents: new IMessageComponent[]
        {
            new ActionRowComponent(new[]
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
            })
        });

        return await _feedbackService.SendContextualEmbedAsync(embed, options, this.CancellationToken);
    }


}
