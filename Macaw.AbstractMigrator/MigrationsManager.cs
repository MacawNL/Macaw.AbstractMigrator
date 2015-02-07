using System;
using System.Collections.Generic;
using System.Linq;

namespace Macaw.AbstractMigrator
{
    public class MigrationsManager<TDatabase> : IManageMigrations<TDatabase> where TDatabase : class
    {
        private List<IMigrateSchema<TDatabase>> _schemas;
        private readonly IRunMigrations<TDatabase> _runner;

        public MigrationsManager(IEnumerable<IMigrateSchema<TDatabase>> schemas, IRunMigrations<TDatabase> runner)
        {
            schemas.MustNotBeNull();
            schemas.ForEach(s => s.Runner = runner);
            _schemas = Sort(schemas);
            _runner = runner;
        }

        private static List<IMigrateSchema<TDatabase>> Sort(IEnumerable<IMigrateSchema<TDatabase>> data)
        {
            return data.OrderByDescending(s => s.Priority).ToList();
        }

        public IMigrateSchema<TDatabase> GetSchemaMigrator(string schemaName = DatabaseMigration<TDatabase>.DefaultSchemaName)
        {
            schemaName.MustNotBeEmpty();
            return _schemas.FirstOrDefault(s => s.SchemaName == schemaName);
        }

        public void Run(params IMigrationTask<TDatabase>[] tasks)
        {
            _runner.Run(tasks);
        }

        //public IUnitOfWork StartUnitOfWork()
        //{
        //    //runner is shared by the schemas, the transaction applies to all schemas
        //    return _runner.StartUnitOfWork();
        //}

        public void InstallAllSchemas()
        {
            foreach (var schema in _schemas) schema.InstallSchema();
        }

        public void Add(IMigrateSchema<TDatabase> schema)
        {
            schema.MustNotBeNull();
            schema.Runner = _runner;
            _schemas.Add(schema);
            _schemas = Sort(_schemas);
        }

        public IEnumerable<IMigrateSchema<TDatabase>> Schemas
        {
            get { return _schemas; }
        }
    }
}