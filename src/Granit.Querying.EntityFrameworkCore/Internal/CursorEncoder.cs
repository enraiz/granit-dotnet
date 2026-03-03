using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Encodes and decodes opaque cursors for keyset pagination.
/// Uses Base64Url-encoded JSON.
/// </summary>
internal static class CursorEncoder
{
    /// <summary>
    /// Encodes a cursor value to a Base64Url string.
    /// </summary>
    public static string Encode(object value)
    {
        string json = JsonSerializer.Serialize(value);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Decodes a Base64Url cursor string back to the target type.
    /// </summary>
    public static T? Decode<T>(string cursor)
    {
        try
        {
            string padded = cursor.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(padded);
            string json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }
}
