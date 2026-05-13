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
        List<byte> bytes = [];
        byte[] buffer = new byte[1];

        while (true)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return bytes.Count == 0 ? null : Decode(bytes);
            }

            if (buffer[0] == (byte)'\n')
            {
                if (bytes.Count > 0 && bytes[^1] == (byte)'\r')
                {
                    bytes.RemoveAt(bytes.Count - 1);
                }

                return Decode(bytes);
            }

            bytes.Add(buffer[0]);
            if (bytes.Count > MaxRequestLineBytes)
            {
                throw new InvalidDataException($"Request line exceeds maximum size of {MaxRequestLineBytes} bytes.");
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

    private static string Decode(List<byte> bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
}
