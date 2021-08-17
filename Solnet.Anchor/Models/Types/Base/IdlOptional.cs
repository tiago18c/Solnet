﻿using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlOptional : IIdlType
    {

        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType ValuesType { get; set; }

        public string GenerateTypeDeclaration()
        {
            string typeDecl = ValuesType.GenerateTypeDeclaration();

            if (ValuesType is IdlValueType)
                typeDecl += "?";

            return typeDecl;
        }
    }
}
