using BasicWeaving.Target;
using NUnit.Framework;

namespace BasicWeaving.Tests
{
    [TestFixture]
    public class BasicWeavingTets
    {
        [Test]
        public void Test1()
        {
            var target = new TargetClass();
            target.TargetMethod();
        }
    }
}
