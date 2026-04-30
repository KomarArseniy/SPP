using System;
using TestFramework;
using TestFramework.Attributes;

namespace Tests.NewFeatures
{
    [TestClass]
    [Category("Filter")]
    [Author("AuthorA")]
    public class AuthorAFilterTests
    {
        [TestMethod] [Author("AuthorA")] [Priority(1)] public void A_Priority1_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(1)] public void A_Priority1_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(2)] public void A_Priority2_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(2)] public void A_Priority2_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(3)] public void A_Priority3_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(3)] public void A_Priority3_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(4)] public void A_Priority4_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(4)] public void A_Priority4_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(5)] public void A_Priority5_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorA")] [Priority(5)] public void A_Priority5_Test2() => Assert.IsTrue(true);
    }

    [TestClass]
    [Category("Filter")]
    [Author("AuthorB")]
    public class AuthorBFilterTests
    {
        [TestMethod] [Author("AuthorB")] [Priority(1)] public void B_Priority1_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(1)] public void B_Priority1_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(2)] public void B_Priority2_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(2)] public void B_Priority2_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(3)] public void B_Priority3_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(3)] public void B_Priority3_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(4)] public void B_Priority4_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(4)] public void B_Priority4_Test2() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(5)] public void B_Priority5_Test1() => Assert.IsTrue(true);
        [TestMethod] [Author("AuthorB")] [Priority(5)] public void B_Priority5_Test2() => Assert.IsTrue(true);
    }
}
