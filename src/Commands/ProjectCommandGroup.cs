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
public class ProjectCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI) : CommandGroup
{
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

            var result = await interactionAPI.CreateInteractionResponseAsync
            (
                interactionContext.Interaction.ID,
                interactionContext.Interaction.Token,
                response,
                ct: this.CancellationToken
            );

            if (!result.IsSuccess)
            {
                await feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{result.Error}");
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            await feedbackService.SendContextualErrorAsync($"An error occurred:\n\n{ex}");
            return (Result)new UnhandledExceptionError(ex);
        }
    }
}
