﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlPublicKey : IIdlType
    {
        public string GenerateTypeDeclaration()
        {
            return "PublicKey";
        }
    }
}
