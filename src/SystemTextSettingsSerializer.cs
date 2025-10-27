using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using OwlCore.ComponentModel;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// An <see cref="IAsyncSerializer{TSerialized}"/> implementation for serializing and deserializing streams using System.Text.Json.
/// </summary>
public class SystemTextSettingsSerializer : IAsyncSerializer<Stream>
{
    private readonly JsonSerializerContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextSettingsSerializer"/> class.
    /// </summary>
    /// <param name="context">The JSON serializer context providing type information.</param>
    public SystemTextSettingsSerializer(JsonSerializerContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a singleton instance of <see cref="SystemTextSettingsSerializer"/> with <see cref="RateLimitSerializerContext"/>.
    /// </summary>
    public static SystemTextSettingsSerializer Singleton { get; } = new(RateLimitSerializerContext.Default);

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync<T>(T data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, _context.GetTypeInfo(typeof(T))!, cancellationToken ?? CancellationToken.None);
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync(Type inputType, object data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, _context.GetTypeInfo(inputType)!, cancellationToken ?? CancellationToken.None);
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public async Task<TResult> DeserializeAsync<TResult>(Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, _context.GetTypeInfo(typeof(TResult))!, cancellationToken ?? CancellationToken.None);
        Guard.IsNotNull(result);
        return (TResult)result;
    }

    /// <inheritdoc />
    public async Task<object> DeserializeAsync(Type returnType, Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, _context.GetTypeInfo(returnType)!, cancellationToken ?? CancellationToken.None);
        Guard.IsNotNull(result);
        return result;
    }
}
