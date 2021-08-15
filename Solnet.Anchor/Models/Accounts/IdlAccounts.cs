using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public class IdlAccounts : IIdlAccountItem
    {
        public string Name { get; set; }


        [JsonConverter(typeof(IIdlAccountItemConverter))]
        public IIdlAccountItem[] Accounts { get; set; }
    }
}
