using System.Collections.Generic;

namespace Macaw.AbstractMigrator
{
    public interface IManageMigrations<TDatabase> : IRunMigrations<TDatabase> where TDatabase : class
    {
        IMigrateSchema<TDatabase> GetSchemaMigrator(string schemaName = DatabaseMigration<TDatabase>.DefaultSchemaName);
        void InstallAllSchemas();
        void Add(IMigrateSchema<TDatabase> schema);
        IEnumerable<IMigrateSchema<TDatabase>> Schemas { get; }
    }
}