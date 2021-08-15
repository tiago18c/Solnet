using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class EnumIdlTypeDefinition : IIdlTypeDefinitionTy
    {
        public string Name { get; set; }


        [JsonConverter(typeof(IEnumVariantsConverter))]
        public IEnumVariants[] Variants { get; set; }
    }
}
