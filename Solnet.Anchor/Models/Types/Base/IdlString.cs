﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlString : IIdlType
    {
        public string GenerateTypeDeclaration()
        {
            return "string";
        }
    }
}
