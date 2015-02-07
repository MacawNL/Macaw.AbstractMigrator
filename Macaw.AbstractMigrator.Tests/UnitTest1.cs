using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macaw.AbstractMigrator.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            MySqlFuMigrator.RunMigrations("EigenZorg");
        }
    }
}
