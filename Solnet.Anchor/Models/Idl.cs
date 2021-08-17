using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{
    public class Idl
    {
        public string Version { get; set; }
        public string Name { get; set; }

        public IdlInstruction[] Instructions { get; set; }


        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Accounts { get; set; }

        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Types { get; set; }

        public IdlErrorCode[] Errors { get; set; }

        public IdlEvent[] Events { get; set; }


        public string GenerateCode()
        {
            var code = "using Solnet.Rpc;" + Environment.NewLine;

            code += "namespace " + Name + "{" + Environment.NewLine;

            if (Accounts != null)
            {
                code += "#region Accounts" + Environment.NewLine;

                foreach (var acc in Accounts)
                {
                    code += acc.GenerateCode();
                }

                code += "#endregion" + Environment.NewLine;
            }

            if (Types != null)
            {

                code += "#region Types" + Environment.NewLine;


                foreach (var type in Types)
                {
                    code += type.GenerateCode();
                }

                code += "#endregion" + Environment.NewLine;

            }

            code += "}";


            return code;
        }
    }






}
