using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlValueType : IIdlType
    {
        public string TypeName { get; set; }

        public string GenerateTypeDeclaration()
        => TypeName switch
        {
            "i8" => "sbyte",
            "u8" => "byte",
            "i16" => "short",
            "u16" => "ushort",
            "i32" => "int",
            "u32" => "uint",
            "i64" => "long",
            "u64" => "ulong",
            "bool" => "bool",
                _ => throw new Exception("Something wrong occurred")
        };

    }
}
