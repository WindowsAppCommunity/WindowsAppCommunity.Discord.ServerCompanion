using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using WindowsAppCommunity.Discord.ServerCompanion.Settings;

namespace WindowsAppCommunity.Discord.ServerCompanion.Commands;

/// <summary>
/// Spam filter configuration commands.
/// </summary>
[Group("spam-filter")]
[RequireDiscordPermission(DiscordPermission.ManageGuild | DiscordPermission.ModerateMembers)]
public partial class SpamFilterCommandGroup : CommandGroup
{
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
    public class TtlGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public TtlGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> GetAsync()
        {
            return await _feedbackService.SendContextualSuccessAsync(
                $"Current TTL window: {_settings.TimeWindowMinutes} minutes");
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> SetAsync(int minutes)
        {
            if (minutes < 1)
                return await _feedbackService.SendContextualErrorAsync("TTL must be at least 1 minute.");

            _settings.TimeWindowMinutes = minutes;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync(
                $"TTL window set to {minutes} minutes.");
        }
    }

    // Crosspost Threshold Configuration
    [Group("crosspost-threshold")]
    public class ThresholdGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public ThresholdGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> GetAsync()
        {
            return await _feedbackService.SendContextualSuccessAsync(
                $"Current duplicate threshold: {_settings.DuplicateThreshold} messages");
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> SetAsync(int count)
        {
            if (count < 1)
                return await _feedbackService.SendContextualErrorAsync("Threshold must be at least 1 message.");

            _settings.DuplicateThreshold = count;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync(
                $"Duplicate threshold set to {count} messages.");
        }
    }

    // Crosspost Mute Duration Configuration
    [Group("crosspost-mute-duration")]
    public class MuteDurationGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public MuteDurationGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("get")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> GetAsync()
        {
            return await _feedbackService.SendContextualSuccessAsync(
                $"Current mute duration: {_settings.MuteDurationMinutes} minutes");
        }

        [Command("set")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> SetAsync(int minutes)
        {
            if (minutes < 1)
                return await _feedbackService.SendContextualErrorAsync("Mute duration must be at least 1 minute.");

            _settings.MuteDurationMinutes = minutes;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync(
                $"Mute duration set to {minutes} minutes.");
        }
    }

    // Crosspost Notification Management
    [Group("crosspost-notifications")]
    public class NotificationsGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public NotificationsGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> ListAsync()
        {
            var channels = _settings.NotificationChannelIds;
            if (channels.Count == 0)
                return await _feedbackService.SendContextualSuccessAsync("No notification channels configured.");

            var mentions = string.Join(", ", channels.Take(25).Select(id => $"<#{id}>"));
            var message = channels.Count > 25
                ? $"Notification channels (showing 25 of {channels.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                : $"Notification channels: {mentions}";

            return await _feedbackService.SendContextualSuccessAsync(message);
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> AddAsync(IChannel channel)
        {
            var channels = _settings.NotificationChannelIds;
            if (channels.Contains(channel.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is already a notification channel.");

            channels.Add(channel.ID.Value);
            _settings.NotificationChannelIds = channels;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Added <#{channel.ID}> as notification channel.");
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> RemoveAsync(IChannel channel)
        {
            var channels = _settings.NotificationChannelIds;
            if (!channels.Contains(channel.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is not a notification channel.");

            channels.Remove(channel.ID.Value);
            _settings.NotificationChannelIds = channels;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Removed <#{channel.ID}> from notification channels.");
        }
    }

    // Crosspost Exemption Management - Channels
    [Group("crosspost-exempt-channels")]
    public class ExemptChannelsGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public ExemptChannelsGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> ListAsync()
        {
            var channels = _settings.ExemptChannelIds;
            if (channels.Count == 0)
                return await _feedbackService.SendContextualSuccessAsync("No exempt channels configured.");

            var mentions = string.Join(", ", channels.Take(25).Select(id => $"<#{id}>"));
            var message = channels.Count > 25
                ? $"Exempt channels (showing 25 of {channels.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                : $"Exempt channels: {mentions}";

            return await _feedbackService.SendContextualSuccessAsync(message);
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> AddAsync(IChannel channel)
        {
            var channels = _settings.ExemptChannelIds;
            if (channels.Contains(channel.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is already exempt.");

            channels.Add(channel.ID.Value);
            _settings.ExemptChannelIds = channels;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Added <#{channel.ID}> to exempt channels.");
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> RemoveAsync(IChannel channel)
        {
            var channels = _settings.ExemptChannelIds;
            if (!channels.Contains(channel.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<#{channel.ID}> is not exempt.");

            channels.Remove(channel.ID.Value);
            _settings.ExemptChannelIds = channels;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Removed <#{channel.ID}> from exempt channels.");
        }
    }

    // Crosspost Exemption Management - Roles
    [Group("crosspost-exempt-roles")]
    public class ExemptRolesGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public ExemptRolesGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> ListAsync()
        {
            var roles = _settings.ExemptRoleIds;
            if (roles.Count == 0)
                return await _feedbackService.SendContextualSuccessAsync("No exempt roles configured.");

            var mentions = string.Join(", ", roles.Take(25).Select(id => $"<@&{id}>"));
            var message = roles.Count > 25
                ? $"Exempt roles (showing 25 of {roles.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                : $"Exempt roles: {mentions}";

            return await _feedbackService.SendContextualSuccessAsync(message);
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> AddAsync(IRole role)
        {
            var roles = _settings.ExemptRoleIds;
            if (roles.Contains(role.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<@&{role.ID}> is already exempt.");

            roles.Add(role.ID.Value);
            _settings.ExemptRoleIds = roles;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Added <@&{role.ID}> to exempt roles.");
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> RemoveAsync(IRole role)
        {
            var roles = _settings.ExemptRoleIds;
            if (!roles.Contains(role.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<@&{role.ID}> is not exempt.");

            roles.Remove(role.ID.Value);
            _settings.ExemptRoleIds = roles;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Removed <@&{role.ID}> from exempt roles.");
        }
    }

    // Crosspost Exemption Management - Users
    [Group("crosspost-exempt-users")]
    public class ExemptUsersGroup : CommandGroup
    {
        private readonly RateLimitSettings _settings;
        private readonly IFeedbackService _feedbackService;

        public ExemptUsersGroup(RateLimitSettings settings, IFeedbackService feedbackService)
        {
            _settings = settings;
            _feedbackService = feedbackService;
        }

        [Command("list")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> ListAsync()
        {
            var users = _settings.ExemptUserIds;
            if (users.Count == 0)
                return await _feedbackService.SendContextualSuccessAsync("No exempt users configured.");

            var mentions = string.Join(", ", users.Take(25).Select(id => $"<@{id}>"));
            var message = users.Count > 25
                ? $"Exempt users (showing 25 of {users.Count}): {mentions}\n\n⚠️ List truncated. Pagination needs implementation."
                : $"Exempt users: {mentions}";

            return await _feedbackService.SendContextualSuccessAsync(message);
        }

        [Command("add")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> AddAsync(IUser user)
        {
            var users = _settings.ExemptUserIds;
            if (users.Contains(user.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<@{user.ID}> is already exempt.");

            users.Add(user.ID.Value);
            _settings.ExemptUserIds = users;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Added <@{user.ID}> to exempt users.");
        }

        [Command("remove")]
        [CommandType(ApplicationCommandType.ChatInput)]
        public async Task<IResult> RemoveAsync(IUser user)
        {
            var users = _settings.ExemptUserIds;
            if (!users.Contains(user.ID.Value))
                return await _feedbackService.SendContextualErrorAsync($"<@{user.ID}> is not exempt.");

            users.Remove(user.ID.Value);
            _settings.ExemptUserIds = users;
            await _settings.SaveAsync();
            return await _feedbackService.SendContextualSuccessAsync($"Removed <@{user.ID}> from exempt users.");
        }
    }
}
