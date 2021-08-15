using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class NamedEnumFields : IEnumVariants
    {
        public IdlField[] Fields { get; set; }
    }
}
