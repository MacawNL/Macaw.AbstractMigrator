using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Macaw.AbstractMigrator.Automatic
{
    internal class AutomaticMigration<TDatabase> : IAutomaticMigration where TDatabase : class
    {
        private readonly TDatabase _db;
        private readonly IManageMigrations<TDatabase> _migrations;
        private readonly IRunMigrations<TDatabase> _runner;
        private readonly TextWriter _log;
        IAutomaticMigrationRepository<TDatabase> _repository;
        IUnitOfWorkCreator<TDatabase> _unitOfWorkCreator;
        // internal const string TableName = "MigrationTracker";
        // internal const string SchemaName = "AutomaticMigration";

        public AutomaticMigration(TDatabase db, IAutomaticMigrationRepository<TDatabase> repository, IUnitOfWorkCreator<TDatabase> unitOfWorkCreator, IManageMigrations<TDatabase> migrations, TextWriter logger)
        {
            _db = db;
            _repository = repository;
            _migrations = migrations;
            _unitOfWorkCreator = unitOfWorkCreator;
            _log = logger;
            _runner = new MigrationTaskRunner<TDatabase>(db, logger);

            UpdateSelf();
        }

        public void Execute(params string[] schemas)
        {
            IEnumerable<IMigrateSchema<TDatabase>> tasks = _migrations.Schemas.Where(s => s.SchemaName != DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName);
            if (schemas.Length > 0)
            {
                tasks = tasks.Where(s => schemas.Any(d => d == s.SchemaName));
            }
            using (var t = _unitOfWorkCreator.StartUnitOfWork(_db))
            {
                foreach (var schema in tasks)
                {
                    var version = GetInstalledVersion(schema.SchemaName);
                    if (version.IsNullOrEmpty())
                    {
                        schema.InstallSchema();
                        AppendVersion(schema.SchemaName, schema.LatestVersionAvailable);
                    }
                    else
                    {
                        if (version != schema.LatestVersionAvailable)
                        {
                            schema.MigrateToLatestFrom(version);
                            AppendVersion(schema.SchemaName, schema.LatestVersionAvailable);
                        }
                    }
                }
                t.Commit();
            }
        }

        public void Untrack(params string[] schemas)
        {
            if (schemas.Length == 0) return;
            _repository.UntrackSchemas(schemas);
        }

        private void AppendVersion(string schema, string version)
        {
            _repository.AppendVersion(schema, version);
        }

        private void UpdateSelf()
        {
            IMigrateSchema<TDatabase> automaticMigrationSchema = _migrations.Schemas.SingleOrDefault(s => s.SchemaName == DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName);
            automaticMigrationSchema.MustNotBeNull("Required schema '" + DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName + "' not found");

            using (var t = _unitOfWorkCreator.StartUnitOfWork(_db))
            {
                if (!_repository.IsMigrationsInitialized())
                {
                    automaticMigrationSchema.InstallSchema();
                    AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, automaticMigrationSchema.LatestVersionAvailable);
                    // migrator.InstallSchema();
                    // AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, migrator.LatestVersionAvailable);
                }
                else
                {
                    var latest = GetInstalledVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName);
                    if (latest.IsNullOrEmpty())
                    {
                        automaticMigrationSchema.InstallSchema();
                        AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, automaticMigrationSchema.LatestVersionAvailable);
                        //migrator.InstallSchema();
                        //AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, migrator.LatestVersionAvailable);
                    }
                    else
                    {
                        if (latest != automaticMigrationSchema.LatestVersionAvailable)
                        {
                            automaticMigrationSchema.MigrateToLatestFrom(latest);
                            AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, automaticMigrationSchema.LatestVersionAvailable);
                            //migrator.MigrateToLatestFrom(latest);
                            //AppendVersion(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, migrator.LatestVersionAvailable);
                        }
                    }
                }

                t.Commit();
            }
        }

        // works on current assembly!
        private IEnumerable<IMigrationTask<TDatabase>> GetMigratorTasks()
        {
            IEnumerable<IMigrationTask<TDatabase>> tasks = GetType().Assembly.GetTypesDerivedFrom<IMigrationTask<TDatabase>>(true)
                                 .Select(t => (IMigrationTask<TDatabase>)Activator.CreateInstance(t));
            return tasks.Where(t => t.SchemaName == DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName);
        }


        private string GetInstalledVersion(string schema)
        {   
            return _repository.GetInstalledVersion(schema);
            // return _db.GetValue<string>("select " + _db.EscapeIdentifier("Version") + " from " + _db.EscapeIdentifier(TableName) + " where {0}=@0 order by {1} desc".ToFormat(_db.EscapeIdentifier("SchemaName"),_db.EscapeIdentifier("Id")), schema);
        }
    }
}