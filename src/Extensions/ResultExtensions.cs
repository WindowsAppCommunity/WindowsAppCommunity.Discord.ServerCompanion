using Remora.Results;

namespace WindowsAppCommunity.Discord.ServerCompanion.Extensions
{
    internal static class ResultExtensions
    {
        internal static T GetEntityOrThrowError<T>(this Result<T> result, string? message = null)
        {
            if (!result.IsSuccess)
                throw new Exception(message ?? result.Error.Message);

            return result.Entity;
        }
    }
}
