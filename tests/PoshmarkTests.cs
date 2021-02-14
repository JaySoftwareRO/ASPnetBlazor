using lib.poshmark_client;
using Xunit;

namespace tests
{
    public class PoshmarkTests
    {
        [Fact]
        public void List()
        {
            var PoshmarkClient = new PoshmarkClient();
            var foo2 = PoshmarkClient.List("ckingsings"); // Random poshmark account name
        }
    }
}
