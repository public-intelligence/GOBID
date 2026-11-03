using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GOBID.Application.DTOs.ApplicationAccess;

public class ConfigurationJsonConverter: Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var json = (string)reader.Value!;
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<object>(json);
            }
            catch
            {
                return json;
            }
        }
        if (reader.TokenType == JsonToken.StartObject)
        {
            return JObject.Load(reader);
        }
        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is JObject jObject)
        {
            jObject.WriteTo(writer);
        }
        else if (value != null)
        {
            serializer.Serialize(writer, value);
        }
        else
        {
            writer.WriteNull();
        }
    }
}