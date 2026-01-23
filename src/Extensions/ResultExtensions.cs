using System.Drawing;
using Remora.Discord.API.Objects;
using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion;

public static class ErrorExtensions
{
    public static Embed ToEmbed(this IResultError resultError)
    {
        return new Embed
        {
            Colour = Color.Crimson,
            Description = resultError.Message,
        };
    }

    public static Embed ToMessageEmbed(this Exception exception)
    {
        return new Embed
        {
            Title = exception.GetType().Name,
            Description = exception.Message,
            Colour = Color.Crimson
        };
    }

    public static Embed ToStackTraceEmbed(this Exception exception)
    {
        return new Embed
        {
            Title = "Stack Trace",
            Description = $"```{exception.StackTrace}```",
            Colour = Color.PaleVioletRed,
        };
    }
}

internal static class ResultExtensions
{
    internal static T GetEntityOrThrowError<T>(this Result<T> result, string? message = null)
    {
        if (!result.IsSuccess)
            throw new Exception(message ?? result.Error.Message);

        return result.Entity;
    }
}
