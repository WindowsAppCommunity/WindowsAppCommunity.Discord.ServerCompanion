using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Drawing;

public class PingPongResponder : IResponder<IMessageCreate>
{
    private readonly IDiscordRestChannelAPI _channelAPI;

    public PingPongResponder(IDiscordRestChannelAPI channelAPI)
    {
        _channelAPI = channelAPI;
    }

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Content != "!ping")
            return Result.FromSuccess();

        var embed = new Embed(Description: "Pong!", Colour: Color.LawnGreen);

        return (Result)await _channelAPI.CreateMessageAsync
        (
            gatewayEvent.ChannelID,
            embeds: new[] { embed },
            ct: ct
        );
    }
}