using System;
using System.Collections.Generic;
using System.Drawing;
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
using WindowsAppCommunity.Sdk.Nomad;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.Project
{

    public class ProjectCommandGroup(INomadRepoService nomadRepoService, IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("createProject")]
        public async Task<IResult> CreateProjectAsync(string name, string description)
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
            return await feedbackService.SendContextualSuccessAsync($"Project created successfully with id '{createdProject.Id}' name '{name}' and description '{description}'");
        }

        [Command("getProject")]
        public async Task<IResult> GetProjectAsync(string projectId)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");
            var repoId = string.Empty;
            var knownId = repoId = userId.Value.ToString();
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);

            var initialMessage = await channelApi.CreateMessageAsync(channelId, $"Getting project {projectId}");

            var project = await repositoryContainer.ProjectRepository.GetAsync(projectId, Config.CancellationToken);
            var responseBuilder = new StringBuilder();

            responseBuilder.AppendLine($"Project ID: {project.Id}");
            responseBuilder.AppendLine($"Name: {project.Name}");
            responseBuilder.AppendLine($"Description: {project.Description}");
            responseBuilder.AppendLine($"Extended Description: {project.ExtendedDescription}");
            responseBuilder.AppendLine($"Accent Color: {project.AccentColor}");
            responseBuilder.AppendLine($"Category: {project.Category}");

            responseBuilder.AppendLine("Features:");
            foreach (var feature in project.Features)
            {
                responseBuilder.AppendLine($"  - {feature}");
            }

            responseBuilder.AppendLine("Links:");
            foreach (var link in project.Links)
            {
                responseBuilder.AppendLine($"  - ID: {link.Id}");
                responseBuilder.AppendLine($"    Name: {link.Name}");
                responseBuilder.AppendLine($"    Description: {link.Description}");
                responseBuilder.AppendLine($"    URL: {link.Url}");
            }

            responseBuilder.AppendLine("Images:");
            await foreach (var image in project.GetImageFilesAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {image.Id}");
                responseBuilder.AppendLine($"    Name: {image.Name}");

                var cid = await image.GetCidAsync(Config.Client, new AddFileOptions { Pin = Config.KuboOptions.ShouldPin }, Config.CancellationToken);
                responseBuilder.AppendLine($"    CID: {cid}");
                responseBuilder.AppendLine($"    Type: {image.GetType()}");
            }

            responseBuilder.AppendLine("Connections:");
            await foreach (var connection in project.GetConnectionsAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {connection.Id}");
                responseBuilder.AppendLine($"    Value: {await connection.GetValueAsync(Config.CancellationToken)}");
            }

            responseBuilder.AppendLine("Users:");
            await foreach (var user in project.GetUsersAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {user.Id}");
                responseBuilder.AppendLine($"    Name: {user.Name}");
                responseBuilder.AppendLine($"    Role:");
                responseBuilder.AppendLine($"      ID: {user.Role.Id}");
                responseBuilder.AppendLine($"      Name: {user.Role.Name}");
                responseBuilder.AppendLine($"      Description: {user.Role.Description}");
            }

            responseBuilder.AppendLine("Publisher:");
            var publisher = await project.GetPublisherAsync(Config.CancellationToken);
            if (publisher is not null)
            {
                responseBuilder.AppendLine($"  - ID: {publisher.Id}");
                responseBuilder.AppendLine($"    Name: {publisher.Name}");
            }

            responseBuilder.AppendLine("Dependencies:");
            await foreach (var dependency in project.Dependencies.GetProjectsAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"  - ID: {dependency.Id}");
                responseBuilder.AppendLine($"    Name: {dependency.Name}");
            }

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);

            return await feedbackService.SendContextualSuccessAsync(responseBuilder.ToString());
        }

        [Command("listProject")]
        public async Task<IResult> ListProjectAsync()
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            if (!context.TryGetChannelID(out var channelId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the channel ID.");
            var repoId = string.Empty;
            var knownId = repoId = userId.Value.ToString();
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepoAsync(repoId, knownId, CancellationToken.None);

            var initialMessage = await channelApi.CreateMessageAsync(channelId, $"Listing projects...");

            var responseBuilder = new StringBuilder();
            await foreach (var project in repositoryContainer.ProjectRepository.GetAsync(Config.CancellationToken))
            {
                responseBuilder.AppendLine($"{nameof(project.Id)}: {project.Id}");
                responseBuilder.AppendLine($"{nameof(project.Name)}: {project.Name}");
            }
            responseBuilder.AppendLine($"Finished listing projects for repository {repoId}");

            await channelApi.DeleteMessageAsync(channelId, initialMessage.Entity.ID);
            return await feedbackService.SendContextualSuccessAsync(responseBuilder.ToString());
        }
    }
}
