using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo;
using OwlCore.Nomad.Kubo.Events;
using OwlCore.Storage;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Services;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Publisher
{
    public class PublisherCommandGroup(INomadRepoService nomadRepoService, IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("createPublisher")]
        public async Task<IResult> CreatePublisherAsync(string repoId, string name, string description)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");

            var knownId = userId.Value.ToString();

            var initialMessage = await channelApi.CreateMessageAsync(channelId, $"Starting publisher creation for repository {repoId}...");
            if (!initialMessage.IsSuccess)
                return initialMessage;

            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);


            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Creating publisher...");
            var createdPublisher = await repositoryContainer.PublisherRepository.CreateAsync(new(KnownId: knownId), Config.CancellationToken);
            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, $"Created publisher with ID {createdPublisher.Id}");

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Setting name and description...");
            await createdPublisher.UpdateNameAsync(name, Config.CancellationToken);
            await createdPublisher.UpdateDescriptionAsync(description, Config.CancellationToken);

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Publishing local event stream...");
            await createdPublisher.PublishLocalAsync<ModifiablePublisher, ValueUpdateEvent>(Config.CancellationToken);

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Publishing roaming value...");
            await createdPublisher.PublishRoamingAsync<ModifiablePublisher, ValueUpdateEvent, WindowsAppCommunity.Sdk.Models.Publisher>(Config.CancellationToken);

            await channelApi.EditMessageAsync(channelId, initialMessage.Entity.ID, "Saving repository keys...");
            await repoSettings.SaveAsync(Config.CancellationToken);

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);
            return await feedbackService.SendContextualSuccessAsync(
                $"Publisher created successfully with ID '{createdPublisher.Id}', name '{name}', and description '{description}'.");
        }


        [Command("getPublisher")]
        public async Task<IResult> GetPublisherAsync(string publisherId)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");

            var repoId = userId.Value.ToString();
            var knownId = repoId;
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);

            var initialMessage = await channelApi.CreateMessageAsync(channelId, $"Getting publisher {publisherId}");

            var publisher = await repositoryContainer.PublisherRepository.GetAsync(publisherId, Config.CancellationToken);
            var responseBuilder = new StringBuilder();

            responseBuilder.AppendLine($"Publisher ID: {publisher.Id}");
            responseBuilder.AppendLine($"Name: {publisher.Name}");
            responseBuilder.AppendLine($"Description: {publisher.Description}");
            responseBuilder.AppendLine($"Extended Description: {publisher.ExtendedDescription}");
            responseBuilder.AppendLine($"Accent Color: {publisher.AccentColor}");

            responseBuilder.AppendLine("Links:");
            foreach (var link in publisher.Links)
            {
                responseBuilder.AppendLine($"  - ID: {link.Id}");
                responseBuilder.AppendLine($"    Name: {link.Name}");
                responseBuilder.AppendLine($"    Description: {link.Description}");
                responseBuilder.AppendLine($"    URL: {link.Url}");
            }

            responseBuilder.AppendLine("Images:");
            await foreach (var image in publisher.GetImageFilesAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {image.Id}");
                responseBuilder.AppendLine($"    Name: {image.Name}");

                var cid = await image.GetCidAsync(Config.Client, new AddFileOptions { Pin = Config.KuboOptions.ShouldPin }, Config.CancellationToken);
                responseBuilder.AppendLine($"    CID: {cid}");
                responseBuilder.AppendLine($"    Type: {image.GetType()}");
            }

            responseBuilder.AppendLine("Connections:");
            await foreach (var connection in publisher.GetConnectionsAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {connection.Id}");
                responseBuilder.AppendLine($"    Value: {await connection.GetValueAsync(Config.CancellationToken)}");
            }

            responseBuilder.AppendLine("Projects:");
            await foreach (var project in publisher.GetProjectsAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {project.Id}");
                responseBuilder.AppendLine($"    Name: {project.Name}");
            }

            responseBuilder.AppendLine("Users:");
            await foreach (var user in publisher.GetUsersAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {user.Id}");
                responseBuilder.AppendLine($"    Name: {user.Name}");
                responseBuilder.AppendLine($"    Role:");
                responseBuilder.AppendLine($"      ID: {user.Role.Id}");
                responseBuilder.AppendLine($"      Name: {user.Role.Name}");
                responseBuilder.AppendLine($"      Description: {user.Role.Description}");
            }

            responseBuilder.AppendLine("Parent Publishers:");
            await foreach (var parentPublisher in publisher.ParentPublishers.GetPublishersAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {parentPublisher.Id}");
                responseBuilder.AppendLine($"    Name: {parentPublisher.Name}");
            }

            responseBuilder.AppendLine("Child Publishers:");
            await foreach (var childPublisher in publisher.ChildPublishers.GetPublishersAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {childPublisher.Id}");
                responseBuilder.AppendLine($"    Name: {childPublisher.Name}");
            }

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);

            return await feedbackService.SendContextualSuccessAsync(responseBuilder.ToString());
        }


        [Command("listPublisher")]
        public async Task<IResult> ListPublisherAsync()
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");

            var repoId = userId.Value.ToString();
            var knownId = repoId;
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);

            var initialMessage = await channelApi.CreateMessageAsync(channelId, "Listing publishers...");

            var responseBuilder = new StringBuilder();
            await foreach (var publisher in repositoryContainer.PublisherRepository.GetAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"{nameof(publisher.Id)}: {publisher.Id}");
                responseBuilder.AppendLine($"{nameof(publisher.Name)}: {publisher.Name}");
            }
            responseBuilder.AppendLine($"Finished listing publishers for repository {repoId}");

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);
            return await feedbackService.SendContextualSuccessAsync(responseBuilder.ToString());
        }


    }
}
