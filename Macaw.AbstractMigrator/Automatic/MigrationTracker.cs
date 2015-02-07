using System;

namespace Macaw.AbstractMigrator.Automatic
{
    internal class MigrationTracker
    {
        public MigrationTracker()
        {
            Updated = DateTime.UtcNow;
        }
        public string SchemaName { get; set; }
        public string MigrationVersion { get; set; }
        public DateTime Updated { get; set; } 
    }
}