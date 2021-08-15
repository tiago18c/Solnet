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
    public class IEnumVariantsConverter : JsonConverter<IEnumVariants>
    {
        public override IEnumVariants Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IEnumVariants value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
