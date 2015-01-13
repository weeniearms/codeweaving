using NUnit.Framework;
using PostSharpWeaving.Target;

namespace PostSharpWeaving.Tests
{
    [TestFixture]
    public class PostSharpWeavingTests
    {
        [SetUp]
        public void SetUp()
        {
            PluginFactory.Reset();
        }

        [Test]
        public void Test1()
        {
            var target = new TargetClass();
            target.TargetMethod();
        }

        [Test]
        public void Test2()
        {
            var target = new Target.Target();
            var result = target.Method(2);

            Assert.AreEqual(4, result);
        }

        [Test]
        public void Test3()
        {
            PluginFactory.RegisterPlugin<TargetPluginBase, FirstTargetPlugin>();

            var target = new Target.Target();
            var result = target.Method(2);

            Assert.AreEqual(8, result);
        }

        [Test]
        public void Test4()
        {
            PluginFactory.RegisterPlugin<TargetPluginBase, SecondTargetPlugin>();

            var target = new Target.Target();
            var result = target.Method(2);

            Assert.AreEqual(16, result);
        }

        public class FirstTargetPlugin : TargetPluginBase
        {
            public override bool BeforeMethod(int p1, ref int result)
            {
                result = p1*p1*p1;
                return true;
            }
        }

        public class SecondTargetPlugin : TargetPluginBase
        {
            public override void AfterMethod(int p1, ref int result)
            {
                result = p1*p1*p1*p1;
            }
        }
    }
}
