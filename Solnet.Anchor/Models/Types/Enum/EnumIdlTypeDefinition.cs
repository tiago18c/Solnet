using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types.Enum;
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
        public IEnumVariant[] Variants { get; set; }

        public string GenerateCode()
        {
            StringBuilder sb = new();

            bool isPure = IsPureEnum();

            sb.Append(Utilities.Lvl1Ident);
            sb.Append("public enum ");
            sb.Append(Utilities.FixName(Name));
            if (isPure)
                sb.AppendLine(" {");
            else
                sb.AppendLine("Type {");

            foreach(var variant in Variants)
            {
                sb.Append(Utilities.Lvl2Ident);
                sb.Append(Utilities.FixName(variant.Name));
                sb.AppendLine(",");
            }

            sb.AppendLine("}");

            if(!isPure)
            {
                sb.Append(Utilities.Lvl1Ident);
                sb.Append("public class ");
                sb.Append(Utilities.FixName(Name));
                sb.AppendLine(" {");

                sb.Append(Utilities.Lvl2Ident);
                sb.Append("public ");
                sb.Append(Utilities.FixName(Name));
                sb.AppendLine("Type Type { get; set; }");

                foreach(var variant in Variants)
                {
                    if (variant is SimpleEnumVariant) continue;

                    sb.Append(Utilities.Lvl2Ident);
                    sb.Append("public ");

                    if(variant is NamedFieldsEnumVariant nf)
                    {
                        sb.Append(Utilities.FixName(nf.Name));
                        sb.Append("Type ");
                        sb.Append(Utilities.FixName(nf.Name));
                        sb.AppendLine("Value { get; set; }");
                    }
                    else if (variant is TupleFieldsEnumVariant tupleVariant)
                    {
                        sb.Append("Tuple");

                        // generate tuple types

                        sb.Append("/* missing tuple types */ ");

                        sb.Append(Utilities.FixName(tupleVariant.Name));
                        sb.AppendLine("Value { get; set; }");
                    }
                }



                sb.AppendLine("}");


                foreach (var variant in Variants)
                {
                    if (variant is NamedFieldsEnumVariant namedFieldsEnumVariant)
                        sb.AppendLine(" //NEED TO GENERATE TYPES FROM STRUCT ENUM VARIANT      " +
                             namedFieldsEnumVariant.Name);
                }
            }


            return sb.ToString();
        }

        public bool IsPureEnum()
        {
            return Variants.All(x => x is SimpleEnumVariant);
        }
    }
}
