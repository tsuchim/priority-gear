using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public static class PipeJsonProtocol
{
    public const int MaxRequestLineBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ServiceRequest?> ReadRequestAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        string? line = await ReadRequestLineAsync(pipe, cancellationToken);
        return DeserializeRequest(line);
    }

    public static ServiceRequest? DeserializeRequest(string? line)
    {
        return string.IsNullOrWhiteSpace(line)
            ? null
            : JsonSerializer.Deserialize<ServiceRequest>(line, JsonOptions);
    }

    public static async Task<string?> ReadRequestLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream bytes = new();
        byte[] buffer = new byte[4096];

        while (true)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return bytes.Length == 0 ? null : Decode(bytes);
            }

            int newlineIndex = Array.IndexOf(buffer, (byte)'\n', 0, read);
            int bytesToWrite = newlineIndex >= 0 ? newlineIndex : read;
            if (bytes.Length + bytesToWrite > MaxRequestLineBytes)
            {
                throw new InvalidDataException($"Request line exceeds maximum size of {MaxRequestLineBytes} bytes.");
            }

            if (bytesToWrite > 0)
            {
                bytes.Write(buffer, 0, bytesToWrite);
            }

            if (newlineIndex >= 0)
            {
                if (bytes.Length > 0 && bytes.GetBuffer()[bytes.Length - 1] == (byte)'\r')
                {
                    bytes.SetLength(bytes.Length - 1);
                }

                return Decode(bytes);
            }
        }
    }

    public static async Task WriteResponseAsync(NamedPipeServerStream pipe, ServiceResponse response, CancellationToken cancellationToken)
    {
        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        string responseJson = JsonSerializer.Serialize(response, JsonOptions);
        await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    private static string Decode(MemoryStream bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
    }
}
