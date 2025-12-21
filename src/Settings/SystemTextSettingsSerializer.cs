using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using OwlCore.ComponentModel;

namespace WindowsAppCommunity.Discord.ServerCompanion;

/// <summary>
/// An <see cref="IAsyncSerializer{TSerialized}"/> implementation for serializing and deserializing streams using System.Text.Json.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemTextSettingsSerializer"/> class.
/// </remarks>
/// <param name="context">The JSON serializer context providing type information.</param>
public class SystemTextSettingsSerializer(JsonSerializerContext context) : IAsyncSerializer<Stream>
{
    private readonly JsonSerializerContext _context = context;

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
