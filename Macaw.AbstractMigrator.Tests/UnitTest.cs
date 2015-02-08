using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macaw.AbstractMigrator.Tests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestSqlFuMigration()
        {
            MySqlFuMigrator.RunMigrations("DefaultConnection");
        }

        [TestMethod]
        public void TestAzureSearchMigration()
        {
            AzureSearchMigrator.RunMigrations("AzureSearchConnection");
        }
    }
}
