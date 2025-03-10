﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands
{
    [Group("commands")]
    public class SampleCommandGroup(IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("sample-command")]
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

}
