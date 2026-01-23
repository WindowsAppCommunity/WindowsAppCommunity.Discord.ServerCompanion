using System.ComponentModel;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Commands.Errors;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands;

/// <summary>
/// Spam filter configuration commands.
/// </summary>
[Group("spam-filter")]
public partial class SpamFilterCommandGroup : CommandGroup
{
    /// <summary>
    /// The role names that are allowed to execute spam filter commands.
    /// Users must have at least one of these roles.
    /// </summary>
    public static readonly string[] RequiredRoleNames = ["Moderator", "Admin", "Administrator"];

    private readonly RateLimitSettings _settings;
    private readonly IFeedbackService _feedbackService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpamFilterCommandGroup"/> class.
    /// </summary>
    public SpamFilterCommandGroup(RateLimitSettings settings, IFeedbackService feedbackService)
    {
        _settings = settings;
        _feedbackService = feedbackService;
    }

    // Crosspost TTL Configuration
    [Group("crosspost-ttl")]
    [Description("Configure time window for duplicate message tracking")]
    public class TtlGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public TtlGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Get current duplicate tracking time window")]
        public async Task<IResult> GetAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                return await _feedbackService.SendContextualSuccessAsync(
                    $"Current TTL window: {_settings.TimeWindowMinutes} minutes");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Set duplicate tracking time window")]
        public async Task<IResult> SetAsync([Description("Time window in minutes")] int minutes)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                if (minutes < 1)
                    return await _feedbackService.SendContextualErrorAsync("TTL must be at least 1 minute.");

                _settings.TimeWindowMinutes = minutes;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync(
                    $"TTL window set to {minutes} minutes.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Threshold Configuration
    [Group("crosspost-threshold")]
    [Description("Configure how many duplicate messages trigger action")]
    public class ThresholdGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public ThresholdGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Get current duplicate message threshold")]
        public async Task<IResult> GetAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                return await _feedbackService.SendContextualSuccessAsync(
                    $"Current duplicate threshold: {_settings.DuplicateThreshold} messages");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Set duplicate message threshold")]
        public async Task<IResult> SetAsync([Description("Number of duplicates that trigger action")] int count)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                if (count < 1)
                    return await _feedbackService.SendContextualErrorAsync("Threshold must be at least 1 message.");

                _settings.DuplicateThreshold = count;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync(
                    $"Duplicate threshold set to {count} messages.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Mute Duration Configuration
    [Group("crosspost-mute-duration")]
    [Description("Configure timeout duration for spam violations")]
    public class MuteDurationGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public MuteDurationGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Get current mute duration for spam violations")]
        public async Task<IResult> GetAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                return await _feedbackService.SendContextualSuccessAsync(
                    $"Current mute duration: {_settings.MuteDurationMinutes} minutes");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Set mute duration for spam violations")]
        public async Task<IResult> SetAsync([Description("Duration in minutes")] int minutes)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                if (minutes < 1)
                    return await _feedbackService.SendContextualErrorAsync("Mute duration must be at least 1 minute.");

                _settings.MuteDurationMinutes = minutes;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync(
                    $"Mute duration set to {minutes} minutes.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Notification Management
    [Group("crosspost-notifications")]
    [Description("Manage channels that receive spam violation alerts")]
    public class NotificationsGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public NotificationsGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("List channels that receive spam violation alerts")]
        public async Task<IResult> ListAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.NotificationChannelIds;
                if (channels.Count == 0)
                    return await _feedbackService.SendContextualSuccessAsync("No notification channels configured.");

                var mentions = string.Join(", ", channels.Take(25).Select(id => $"<#{id}>"));
                var message = channels.Count > 25
                    ? $"Notification channels (showing 25 of {channels.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                    : $"Notification channels: {mentions}";

                return await _feedbackService.SendContextualSuccessAsync(message);
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Add a channel to receive spam violation alerts")]
        public async Task<IResult> AddAsync([Description("Channel to add")] IChannel channel)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.NotificationChannelIds;
                if (channels.Contains(channel.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is already a notification channel.");

                channels.Add(channel.ID.Value);
                _settings.NotificationChannelIds = channels;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Added <#{channel.ID}> as notification channel.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Remove a channel from spam violation alerts")]
        public async Task<IResult> RemoveAsync([Description("Channel to remove")] IChannel channel)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.NotificationChannelIds;
                if (!channels.Contains(channel.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is not a notification channel.");

                channels.Remove(channel.ID.Value);
                _settings.NotificationChannelIds = channels;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Removed <#{channel.ID}> from notification channels.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Exemption Management - Channels
    [Group("crosspost-exempt-channels")]
    [Description("Manage channels where spam detection is disabled")]
    public class ExemptChannelsGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public ExemptChannelsGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("List channels where spam detection is disabled")]
        public async Task<IResult> ListAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.ExemptChannelIds;
                if (channels.Count == 0)
                    return await _feedbackService.SendContextualSuccessAsync("No exempt channels configured.");

                var mentions = string.Join(", ", channels.Take(25).Select(id => $"<#{id}>"));
                var message = channels.Count > 25
                    ? $"Exempt channels (showing 25 of {channels.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                    : $"Exempt channels: {mentions}";

                return await _feedbackService.SendContextualSuccessAsync(message);
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Disable spam detection in a channel")]
        public async Task<IResult> AddAsync([Description("Channel to exempt")] IChannel channel)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.ExemptChannelIds;
                if (channels.Contains(channel.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is already exempt.");

                channels.Add(channel.ID.Value);
                _settings.ExemptChannelIds = channels;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Added <#{channel.ID}> to exempt channels.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Re-enable spam detection in a channel")]
        public async Task<IResult> RemoveAsync([Description("Channel to un-exempt")] IChannel channel)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var channels = _settings.ExemptChannelIds;
                if (!channels.Contains(channel.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is not exempt.");

                channels.Remove(channel.ID.Value);
                _settings.ExemptChannelIds = channels;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Removed <#{channel.ID}> from exempt channels.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Exemption Management - Roles
    [Group("crosspost-exempt-roles")]
    [Description("Manage roles that can crosspost without spam detection")]
    public class ExemptRolesGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public ExemptRolesGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("List roles that can crosspost without spam detection")]
        public async Task<IResult> ListAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var roles = _settings.ExemptRoleIds;
                if (roles.Count == 0)
                    return await _feedbackService.SendContextualSuccessAsync("No exempt roles configured.");

                var mentions = string.Join(", ", roles.Take(25).Select(id => $"<@&{id}>"));
                var message = roles.Count > 25
                    ? $"Exempt roles (showing 25 of {roles.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                    : $"Exempt roles: {mentions}";

                return await _feedbackService.SendContextualSuccessAsync(message);
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Allow a role to crosspost without spam detection")]
        public async Task<IResult> AddAsync([Description("Role to exempt")] IRole role)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var roles = _settings.ExemptRoleIds;
                if (roles.Contains(role.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<@&{role.ID}> is already exempt.");

                roles.Add(role.ID.Value);
                _settings.ExemptRoleIds = roles;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Added <@&{role.ID}> to exempt roles.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Remove crosspost exemption from a role")]
        public async Task<IResult> RemoveAsync([Description("Role to un-exempt")] IRole role)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var roles = _settings.ExemptRoleIds;
                if (!roles.Contains(role.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<@&{role.ID}> is not exempt.");

                roles.Remove(role.ID.Value);
                _settings.ExemptRoleIds = roles;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Removed <@&{role.ID}> from exempt roles.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }

    // Crosspost Exemption Management - Users
    [Group("crosspost-exempt-users")]
    [Description("Manage users that can crosspost without spam detection")]
    public class ExemptUsersGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;

        public ExemptUsersGroup(RateLimitSettings settings, IFeedbackService feedbackService, ICommandContext commandContext, IDiscordRestGuildAPI guildApi)
        {
            _settings = settings;
            _feedbackService = feedbackService;
            _commandContext = commandContext;
            _guildApi = guildApi;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("List users that can crosspost without spam detection")]
        public async Task<IResult> ListAsync()
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var users = _settings.ExemptUserIds;
                if (users.Count == 0)
                    return await _feedbackService.SendContextualSuccessAsync("No exempt users configured.");

                var mentions = string.Join(", ", users.Take(25).Select(id => $"<@{id}>"));
                var message = users.Count > 25
                    ? $"Exempt users (showing 25 of {users.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                    : $"Exempt users: {mentions}";

                return await _feedbackService.SendContextualSuccessAsync(message);
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Allow a user to crosspost without spam detection")]
        public async Task<IResult> AddAsync([Description("User to exempt")] IUser user)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var users = _settings.ExemptUserIds;
                if (users.Contains(user.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<@{user.ID}> is already exempt.");

                users.Add(user.ID.Value);
                _settings.ExemptUserIds = users;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Added <@{user.ID}> to exempt users.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Remove crosspost exemption from a user")]
        public async Task<IResult> RemoveAsync([Description("User to un-exempt")] IUser user)
        {
            try
            {
                if (!_commandContext.TryGetUserID(out var userId) || !_commandContext.TryGetGuildID(out var guildId))
                    return await _feedbackService.SendContextualErrorAsync("Could not determine user or guild context.");

                await userId.RequireRolesAsync(guildId, _guildApi, RequiredRoleNames, this.CancellationToken);

                var users = _settings.ExemptUserIds;
                if (!users.Contains(user.ID.Value))
                    return await _feedbackService.SendContextualErrorAsync($"<@{user.ID}> is not exempt.");

                users.Remove(user.ID.Value);
                _settings.ExemptUserIds = users;
                await _settings.SaveAsync();
                return await _feedbackService.SendContextualSuccessAsync($"Removed <@{user.ID}> from exempt users.");
            }
            catch (Exception ex)
            {
                return await _feedbackService.SendContextualErrorAsync(ex.Message);
            }
        }
    }
}
