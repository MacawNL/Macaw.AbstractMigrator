using System;

namespace Macaw.AbstractMigrator
{
    public class MigrationNotFoundException : Exception
    {
        public MigrationNotFoundException(string message) : base(message)
        {
        }
    }
}