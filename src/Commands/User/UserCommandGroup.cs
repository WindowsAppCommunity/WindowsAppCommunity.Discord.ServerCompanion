using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Org.BouncyCastle.Ocsp;
using OwlCore.Diagnostics;
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

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands.User
{
    public class UserCommandGroup(INomadRepoService nomadRepoService, IInteractionContext interactionContext, IFeedbackService feedbackService, IDiscordRestInteractionAPI interactionAPI, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ICommandContext context) : Remora.Commands.Groups.CommandGroup
    {
        [Command("createUser")]
        public async Task<IResult> CreateUser(string name, string description)
        {
            if (!context.TryGetUserID(out var userId))
                return await feedbackService.SendContextualErrorAsync("Could not determine the user ID.");

            var knownId = userId.Value.ToString();
            var repoId = knownId;
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepo(repoId, knownId);

            var createdUser = await repositoryContainer.UserRepository.CreateAsync(new(KnownId: knownId), Config.CancellationToken);

            Logger.LogInformation($"Setting name and description");

            if (!string.IsNullOrWhiteSpace(name))
                await createdUser.UpdateNameAsync(name, Config.CancellationToken);

            if (!string.IsNullOrWhiteSpace(description))
                await createdUser.UpdateDescriptionAsync(description, Config.CancellationToken);

            Logger.LogInformation($"Publishing local event stream to ipns");
            await createdUser.PublishLocalAsync<ModifiableUser, ValueUpdateEvent>(Config.CancellationToken);

            Logger.LogInformation($"Publishing roaming value to ipns");
            await createdUser.PublishRoamingAsync<ModifiableUser, ValueUpdateEvent, WindowsAppCommunity.Sdk.Models.User>(Config.CancellationToken);

            Logger.LogInformation($"Saving repository keys");
            await repoSettings.SaveAsync(Config.CancellationToken);

            return await feedbackService.SendContextualSuccessAsync($"User created with id {createdUser.Id}");
        }

        [Command("getUser")]
        public async Task<IResult> GetUser(string userId)
        {
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepo(string.Empty, string.Empty);

            Logger.LogInformation($"Getting user {userId}");
            var user = await repositoryContainer.UserRepository.GetAsync(userId, Config.CancellationToken);
            {
                Logger.LogInformation($"{nameof(user.Id)}: {user.Id}");
                Logger.LogInformation($"{nameof(user.Name)}: {user.Name}");
                Logger.LogInformation($"{nameof(user.Description)}: {user.Description}");
                Logger.LogInformation($"{nameof(user.ExtendedDescription)}: {user.ExtendedDescription}");

                Logger.LogInformation($"{nameof(user.Links)}:");
                foreach (var link in user.Links)
                {
                    Logger.LogInformation($"- {nameof(link.Id)}: {link.Id}");
                    Logger.LogInformation($"  {nameof(link.Name)}: {link.Name}");
                    Logger.LogInformation($"  {nameof(link.Description)}: {link.Description}");
                    Logger.LogInformation($"  {nameof(link.Url)}: {link.Url}");
                }

                Logger.LogInformation($"{nameof(user.GetImageFilesAsync)}:");
                await foreach (var image in user.GetImageFilesAsync(Config.CancellationToken))
                {
                    Logger.LogInformation($"- {nameof(image.Id)}: {image.Id}");
                    Logger.LogInformation($"  {nameof(image.Name)}: {image.Name}");

                    var cid = await image.GetCidAsync(Config.Client, new AddFileOptions { Pin = Config.KuboOptions.ShouldPin }, Config.CancellationToken);
                    Logger.LogInformation($"  {nameof(StorableKuboExtensions.GetCidAsync)}: {cid}");
                    Logger.LogInformation($"  Type: {image.GetType()}");
                }

                Logger.LogInformation($"{nameof(user.GetConnectionsAsync)}:");
                await foreach (var connection in user.GetConnectionsAsync(Config.CancellationToken))
                {
                    Logger.LogInformation($"- {nameof(connection.Id)}: {connection.Id}");
                    Logger.LogInformation($"  {nameof(connection.GetValueAsync)}: {await connection.GetValueAsync(Config.CancellationToken)}");
                }

                Logger.LogInformation($"{nameof(user.GetPublishersAsync)}:");
                await foreach (var publisher in user.GetPublishersAsync(Config.CancellationToken))
                {
                    Logger.LogInformation($"- {nameof(publisher.Id)}: {publisher.Id}");
                    Logger.LogInformation($"  {nameof(publisher.Name)}: {publisher.Name}");
                    Logger.LogInformation($"  {nameof(publisher.Role)}:");
                    Logger.LogInformation($"    {nameof(publisher.Role.Id)}: {publisher.Role.Id}");
                    Logger.LogInformation($"    {nameof(publisher.Role.Name)}: {publisher.Role.Name}");
                    Logger.LogInformation($"    {nameof(publisher.Role.Description)}: {publisher.Role.Description}");
                }

                Logger.LogInformation($"{nameof(user.GetProjectsAsync)}:");
                await foreach (var project in user.GetProjectsAsync(Config.CancellationToken))
                {
                    Logger.LogInformation($"- {nameof(project.Id)}: {project.Id}");
                    Logger.LogInformation($"  {nameof(project.Name)}: {project.Name}");
                    Logger.LogInformation($"  {nameof(project.Role)}:");
                    Logger.LogInformation($"    {nameof(project.Role.Id)}: {project.Role.Id}");
                    Logger.LogInformation($"    {nameof(project.Role.Name)}: {project.Role.Name}");
                    Logger.LogInformation($"    {nameof(project.Role.Description)}: {project.Role.Description}");
                }
            }
            return await feedbackService.SendContextualSuccessAsync($"User {user.Id}, Username {user.Name}, User Description {user.Description}");
        }

        [Command("listUser")]
        public async Task<IResult> ListUser(string repoId)
        {
            var (repositoryContainer, Config, repoSettings) = await nomadRepoService.GetNomadRepo(string.Empty, string.Empty);
            var response = new StringBuilder($"Listing users for repository {repoId}\n");
            Logger.LogInformation($"Listing users for repository {repoId}");
            await foreach (var user in repositoryContainer.UserRepository.GetAsync(Config.CancellationToken))
            {
                response.Append($"{nameof(user.Id)}: {user.Id}\n");
                response.Append($"{nameof(user.Name)}: {user.Name}\n");
            }
            response.Append($"Finished listing users for repository {repoId}");

            return await feedbackService.SendContextualSuccessAsync(response.ToString());
        }
    }
}
