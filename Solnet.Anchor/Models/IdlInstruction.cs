﻿using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{

    public class IdlInstruction
    {
        public string Name { get; set; }

        public ulong InstructionSignatureHash { get; set; }

        [JsonConverter(typeof(IIdlAccountItemConverter))]
        public IIdlAccountItem[] Accounts { get; set; }

        public IdlField[] Args { get; set; }

        internal void PreProcess(string baseNamespace, string funcNamespace)
        {
            InstructionSignatureHash = SigHash.GetSigHash(Name, funcNamespace);

            Name = Name.ToPascalCase();

            foreach (var account in Accounts)
            {
                account.PreProcess(baseNamespace, Name);
            }
        }
    }
}