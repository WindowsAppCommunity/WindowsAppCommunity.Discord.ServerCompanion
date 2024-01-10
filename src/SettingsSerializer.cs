using CommunityToolkit.Diagnostics;
using OwlCore.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinAppCommunity.Discord.ServerCompanion.Commands;

namespace WinAppCommunity.Discord.ServerCompanion;

/// <summary>
/// Supplies type information for settings values.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Ipfs.Cid))]
[JsonSerializable(typeof(Dictionary<string, Ipfs.Cid[]>))]
[JsonSerializable(typeof(ObservableCollection<string>))]
[JsonSerializable(typeof(List<ManagedUserMap>))]
public partial class SettingsSerializerContext : JsonSerializerContext
{
}


/// <summary>
/// An <see cref="IAsyncSerializer{TSerialized}"/> and implementation for serializing and deserializing streams using System.Text.Json.
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
        await JsonSerializer.SerializeAsync(stream, data, typeof(T), context: SettingsSerializerContext.Default, cancellationToken: cancellationToken ?? CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public async Task<Stream> SerializeAsync(Type inputType, object data, CancellationToken? cancellationToken = null)
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, inputType, context: SettingsSerializerContext.Default, cancellationToken: cancellationToken ?? CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public async Task<TResult> DeserializeAsync<TResult>(Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, typeof(TResult), SettingsSerializerContext.Default);
        Guard.IsNotNull(result);
        return (TResult)result;
    }

    /// <inheritdoc />
    public async Task<object> DeserializeAsync(Type returnType, Stream serialized, CancellationToken? cancellationToken = null)
    {
        var result = await JsonSerializer.DeserializeAsync(serialized, returnType, SettingsSerializerContext.Default);
        Guard.IsNotNull(result);
        return result;
    }

    /// <inheritdoc />
    public Stream Serialize<T>(T data)
    {
        var stream = new MemoryStream();
        JsonSerializer.SerializeAsync(stream, data, typeof(T), context: SettingsSerializerContext.Default, cancellationToken: CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public Stream Serialize(Type type, object data)
    {
        var stream = new MemoryStream();
        JsonSerializer.SerializeAsync(stream, data, type, context: SettingsSerializerContext.Default, cancellationToken: CancellationToken.None);
        return stream;
    }

    /// <inheritdoc />
    public TResult Deserialize<TResult>(Stream serialized)
    {
        var result = JsonSerializer.Deserialize(serialized, typeof(TResult), SettingsSerializerContext.Default);
        Guard.IsNotNull(result);
        return (TResult)result;
    }

    /// <inheritdoc />
    public object Deserialize(Type type, Stream serialized)
    {
        var result = JsonSerializer.Deserialize(serialized, type, SettingsSerializerContext.Default);
        Guard.IsNotNull(result);
        return result;
    }
}
