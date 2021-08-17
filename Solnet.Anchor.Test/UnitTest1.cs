using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solnet.Anchor;

namespace Solnet.Anchor.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //var res = IdlParser.ParseFile("Resources/ChatExample.json");
            var res = IdlParser.ParseFile("Resources/SwapEdited.json");
            Assert.IsNotNull(res);

            var code = res.GenerateCode();

            Assert.IsNotNull(code);

        }
    }
}
