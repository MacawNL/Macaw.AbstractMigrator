using Macaw.AbstractMigrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macaw.AbstractMigrator
{
    public interface IAutomaticMigrationRepository<TDatabase> where TDatabase : class
    {
        bool IsMigrationsInitialized();
        void UntrackSchemas(IEnumerable<string> schemas);
        void AppendVersion(string schema, string version);
        string GetInstalledVersion(string schema);
    }
}
