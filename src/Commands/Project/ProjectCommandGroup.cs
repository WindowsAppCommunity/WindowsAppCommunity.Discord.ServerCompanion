using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OwlCore.Nomad.Kubo;
using OwlCore.Nomad.Kubo.Events;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Services;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Project
{

    public class ProjectCommandGroup(INomadRepoService nomadRepoService, IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("createProject")]
        public async Task<IResult> CreateUser(string name, string description)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");

            var knownId = userId.Value.ToString();
            var repoId = knownId;
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);


            var initialMessage = await channelApi.CreateMessageAsync(channelId, "Starting project creation process...");

            if (!initialMessage.IsSuccess)
                return initialMessage;

            var createdProject = await repositoryContainer.ProjectRepository.CreateAsync(new(KnownId: knownId), Config.CancellationToken);
            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, $"Created project with ID {createdProject.Id} via known ID {knownId}");

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Setting name and description...");
            await createdProject.UpdateNameAsync(name, Config.CancellationToken);
            await createdProject.UpdateDescriptionAsync(description, Config.CancellationToken);

            //  await initialMessage.EditMessageAsync(initialMessage.Entity.ID, "Publishing local event stream to ipns...");
            await createdProject.FlushAsync(Config.CancellationToken);

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Saving repository keys...");
            await repoSettings.SaveAsync(Config.CancellationToken);

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);
            return await feedbackService.SendContextualSuccessAsync($"Project created successfully with name '{name}' and description '{description}'");
        }
    }
}
