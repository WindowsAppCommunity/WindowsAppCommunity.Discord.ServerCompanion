using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OwlCore.Nomad.Kubo.Events;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Services;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Repo
{
    public class RepoCommandGroup(INomadRepoService nomadRepoService, IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("createRepo")]
        public async Task<IResult> CreateRepoAsync()
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            var knownId = Guid.NewGuid().ToString();
            var repoId = knownId;
            var repo = await nomadRepoService.CreateRepoAsync(knownId, CancellationToken.None);


            return await feedbackService.SendContextualSuccessAsync($"Repository created with id {repo}");
        }

        [Command("getRepo")]
        public async Task<IResult> GetRepoAsync(string repoId)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");

            var knownId = repoId;
            //  var repoId = knownId;

            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);

            var initialMessage = await channelApi.CreateMessageAsync(channelId, $"Getting repository {userId}");

            var savedMissingFromRepoConfigs = repositoryContainer.PublisherRepository.ManagedConfigs
                .Where(x => repoSettings.ManagedPublisherConfigs.All(y => x.RoamingId != y.RoamingId))
                .ToList();

            var repoMissingFromSavedConfigs = repoSettings.ManagedPublisherConfigs
                .Where(x => repositoryContainer.PublisherRepository.ManagedConfigs.All(y => x.RoamingId != y.RoamingId))
                .ToList();

            foreach (var item in savedMissingFromRepoConfigs)
                repositoryContainer.PublisherRepository.ManagedConfigs.Add(item);

            var responseBuilder = new StringBuilder();
            responseBuilder.AppendLine($"Repository ID: {userId}");
            responseBuilder.AppendLine($"Synced missing configs: {savedMissingFromRepoConfigs.Count}");
            responseBuilder.AppendLine($"Configs missing from repo: {repoMissingFromSavedConfigs.Count}");

            responseBuilder.AppendLine("Users:");
            await foreach (var user in repositoryContainer.UserRepository.GetAsync(CancellationToken.None))
            {
                responseBuilder.AppendLine($"  - ID: {user.Id}");
            }

            responseBuilder.AppendLine("Projects:");
            await foreach (var project in repositoryContainer.ProjectRepository.GetAsync(CancellationToken.None))
            {
                responseBuilder.AppendLine($"  - ID: {project.Id}");
            }

            responseBuilder.AppendLine("Publishers:");
            await foreach (var publisher in repositoryContainer.PublisherRepository.GetAsync(CancellationToken.None))
            {
                responseBuilder.AppendLine($"  - ID: {publisher.Id}");
            }

            foreach (var item in repoMissingFromSavedConfigs)
                repoSettings.ManagedPublisherConfigs = [.. repoSettings.ManagedPublisherConfigs, item];

            await repoSettings.SaveAsync();
            responseBuilder.AppendLine("Repository store saved successfully.");

            // Clean up initial message
            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);

            return await feedbackService.SendContextualSuccessAsync(responseBuilder.ToString());
        }


        [Command("listRepo")]
        public async Task<IResult> ListRepoAsync()
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            var knownId = Guid.NewGuid().ToString();
            var repoId = knownId;
            var repo = await nomadRepoService.ListRepoAsync(CancellationToken.None);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Repositories:");
            foreach (var item in repo)
            {
                stringBuilder.AppendLine($" - {item}");
            }

            return await feedbackService.SendContextualSuccessAsync($"Repository list {stringBuilder.ToString()}");
        }

        [Command("deleteRepo")]
        public async Task<IResult> DeleteRepoAsync(string repoId)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            var isDeleted = await nomadRepoService.DeleteRepoAsync(repoId, CancellationToken.None);

            if(isDeleted)
                return await feedbackService.SendContextualSuccessAsync($"Repository {repoId} deleted successfully.");
            else
                return await feedbackService.SendContextualErrorAsync($"Failed to delete repository {repoId}.");
        }
    }
}
