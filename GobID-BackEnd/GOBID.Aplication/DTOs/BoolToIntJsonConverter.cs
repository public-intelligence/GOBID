using Newtonsoft.Json;

namespace GOBID.Application.DTOs;

public class BoolToIntJsonConverter : JsonConverter<bool>
{
    public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
    {
        writer.WriteValue(value ? 1 : 0);
    }

    public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return Convert.ToInt32(reader.Value) == 1;
    }
}
