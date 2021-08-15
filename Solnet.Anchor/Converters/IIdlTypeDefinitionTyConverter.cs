using Solnet.Anchor.Models;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Converters
{
    public class IIdlTypeDefinitionTyConverter : JsonConverter<IIdlTypeDefinitionTy[]>
    {
        public override IIdlTypeDefinitionTy[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType != JsonTokenType.StartArray) return null;
            reader.Read();

            List<IIdlTypeDefinitionTy> types = new();

            while (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                string propertyName = reader.GetString();
                if ("name" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                string typeName = reader.GetString();


                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                if ("type" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                if ("kind" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                string typeType = reader.GetString();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                if ("fields" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();

                if ("struct" == typeType)
                {
                    var res = JsonSerializer.Deserialize<IdlField[]>(ref reader, options);

                    types.Add(new StructIdlTypeDefinition() { Name = typeName, Fields = res });
                    reader.Read(); //end array
                }
                else
                {
                    throw new NotImplementedException();
                }

                // end type inner property
                reader.Read();
                // end type 
                reader.Read();
            }
            return types.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, IIdlTypeDefinitionTy[] value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
