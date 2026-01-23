using System.Text;
using Ipfs.CoreApi;
using OwlCore.Diagnostics;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo;
using OwlCore.Nomad.Kubo.Events;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Sdk.Nomad;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands;

[Group("user")]
public partial class UserCommandGroup(
    ICommandContext CommandContext,
    IFeedbackService Feedback,
    IDiscordServerHostedUserSettingsRepository HostedUserSettingsRepo,
    ICoreApi Client,
    IKuboOptions KuboOptions,
    IWacNomadRepoGroupRepository EntityNomadRepoGroupRepository
) : Remora.Commands.Groups.CommandGroup
{
    [Command("get")]
    public async Task<IResult> GetUser(string userId)
    {
        // TODO: This gets by discord ID, not by user ipns id.
        // Nomad repo should always represent the calling user, not the user being requested.
        var nomadRepoItem = await EntityNomadRepoGroupRepository.GetAsync(userId, CancellationToken.None);

        Logger.LogInformation($"Getting user {userId}");
        var user = await nomadRepoItem.RepositoryGroup.UserRepository.GetAsync(userId, CancellationToken.None);
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
            await foreach (var image in user.GetImageFilesAsync(CancellationToken.None))
            {
                Logger.LogInformation($"- {nameof(image.Id)}: {image.Id}");
                Logger.LogInformation($"  {nameof(image.Name)}: {image.Name}");

                var cid = await image.GetCidAsync(Client, new AddFileOptions { Pin = KuboOptions.ShouldPin }, CancellationToken.None);
                Logger.LogInformation($"  {nameof(StorableKuboExtensions.GetCidAsync)}: {cid}");
                Logger.LogInformation($"  Type: {image.GetType()}");
            }

            Logger.LogInformation($"{nameof(user.GetConnectionsAsync)}:");
            await foreach (var connection in user.GetConnectionsAsync(CancellationToken.None))
            {
                Logger.LogInformation($"- {nameof(connection.Id)}: {connection.Id}");
                Logger.LogInformation($"  {nameof(connection.GetValueAsync)}: {await connection.GetValueAsync(CancellationToken.None)}");
            }

            Logger.LogInformation($"{nameof(user.GetPublishersAsync)}:");
            await foreach (var publisher in user.GetPublishersAsync(CancellationToken.None))
            {
                Logger.LogInformation($"- {nameof(publisher.Id)}: {publisher.Id}");
                Logger.LogInformation($"  {nameof(publisher.Name)}: {publisher.Name}");
                Logger.LogInformation($"  {nameof(publisher.Role)}:");
                Logger.LogInformation($"    {nameof(publisher.Role.Id)}: {publisher.Role.Id}");
                Logger.LogInformation($"    {nameof(publisher.Role.Name)}: {publisher.Role.Name}");
                Logger.LogInformation($"    {nameof(publisher.Role.Description)}: {publisher.Role.Description}");
            }

            Logger.LogInformation($"{nameof(user.GetProjectsAsync)}:");
            await foreach (var project in user.GetProjectsAsync(CancellationToken.None))
            {
                Logger.LogInformation($"- {nameof(project.Id)}: {project.Id}");
                Logger.LogInformation($"  {nameof(project.Name)}: {project.Name}");
                Logger.LogInformation($"  {nameof(project.Role)}:");
                Logger.LogInformation($"    {nameof(project.Role.Id)}: {project.Role.Id}");
                Logger.LogInformation($"    {nameof(project.Role.Name)}: {project.Role.Name}");
                Logger.LogInformation($"    {nameof(project.Role.Description)}: {project.Role.Description}");
            }
        }
        return await Feedback.SendContextualSuccessAsync($"User {user.Id}, Username {user.Name}, User Description {user.Description}");
    }

    [Command("list")]
    public async Task<IResult> ListUser(string repoId)
    {
        var nomadRepoItem = await EntityNomadRepoGroupRepository.GetAsync(repoId, CancellationToken.None);
        var response = new StringBuilder($"Listing users for repository {repoId}\n");
        Logger.LogInformation($"Listing users for repository {repoId}");
        await foreach (var user in nomadRepoItem.RepositoryGroup.UserRepository.GetAsync(CancellationToken.None))
        {
            response.Append($"{nameof(user.Id)}: {user.Id}\n");
            response.Append($"{nameof(user.Name)}: {user.Name}\n");
        }
        response.Append($"Finished listing users for repository {repoId}");

        return await Feedback.SendContextualSuccessAsync(response.ToString());
    }
}
