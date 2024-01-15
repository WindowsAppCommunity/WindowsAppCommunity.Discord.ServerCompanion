using CommunityToolkit.Diagnostics;
using Newtonsoft.Json;
using OwlCore.ComponentModel;

/// <summary>
/// An <see cref="IAsyncSerializer{TSerialized}"/> and implementation for serializing and deserializing streams using Newtonsoft.Json.
/// </summary>
public class SettingsSerializer : IAsyncSerializer<Stream>, ISerializer<Stream>
{
    /// <summary>
    /// A singleton instance for <see cref="SettingsSerializer"/>.
    /// </summary>
    public static SettingsSerializer Singleton { get; } = new();

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync<T>(T data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        var json = JsonConvert.SerializeObject(data);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync(Type inputType, object data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        var json = JsonConvert.SerializeObject(data, inputType, new JsonSerializerSettings());
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public async Task<TResult> DeserializeAsync<TResult>(Stream serialized, CancellationToken? cancellationToken = null)
    {
        var reader = new StreamReader(serialized);
        var json = await reader.ReadToEndAsync();
        var result = JsonConvert.DeserializeObject<TResult>(json);
        Guard.IsNotNull(result);
        return result;
    }

    /// <inheritdoc />
    public async Task<object> DeserializeAsync(Type returnType, Stream serialized, CancellationToken? cancellationToken = null)
    {
        var reader = new StreamReader(serialized);
        var json = await reader.ReadToEndAsync();
        var result = JsonConvert.DeserializeObject(json, returnType);
        Guard.IsNotNull(result);
        return result;
    }

    /// <inheritdoc />
    public Stream Serialize<T>(T data)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        var json = JsonConvert.SerializeObject(data);
        writer.Write(json);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public Stream Serialize(Type type, object data)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        var json = JsonConvert.SerializeObject(data, type, new JsonSerializerSettings());
        writer.Write(json);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc />
    public TResult Deserialize<TResult>(Stream serialized)
    {
        var reader = new StreamReader(serialized);
        var json = reader.ReadToEnd();
        var result = JsonConvert.DeserializeObject<TResult>(json);
        Guard.IsNotNull(result);
        return result;
    }

    /// <inheritdoc />
    public object Deserialize(Type type, Stream serialized)
    {
        var reader = new StreamReader(serialized);
        var json = reader.ReadToEnd();
        var result = JsonConvert.DeserializeObject(json, type);
        Guard.IsNotNull(result);
        return result;
    }
}
