using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOBID.Application.DTOs;

public class ByteArrayToHexJsonConverter : JsonConverter<byte[]?>
{
    public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteValue("0x" + BitConverter.ToString(value).Replace("-", ""));
    }

    public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var hex = reader.Value?.ToString();
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("0x"))
            return null;
        int NumberChars = hex.Length - 2;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(2 + i, 2), 16);
        return bytes;
    }
}
