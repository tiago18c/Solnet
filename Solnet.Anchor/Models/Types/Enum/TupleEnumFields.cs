using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class TupleEnumFields : IEnumVariants
    {

        [JsonConverter(typeof(IIdlTypeArrayConverter))]
        public IIdlType[] Types { get; set; }
    }
}
