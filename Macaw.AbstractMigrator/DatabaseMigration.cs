using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Macaw.AbstractMigrator.Automatic;

namespace Macaw.AbstractMigrator
{
    public class DatabaseMigration<TDatabase> : IConfigureMigrationsRunner<TDatabase> where TDatabase : class
    {
        private readonly TDatabase _db;
        public const string DefaultSchemaName = "_GlobalSchema";
        public const string AutomaticMigrationSchemaName = "AutomaticMigration";
        private TextWriter _log = TextWriter.Null;
        private readonly List<Assembly> _asm = new List<Assembly>();
        private IResolveDependencies _resolver = ActivatorContainer.Instance;
        private IAutomaticMigrationRepository<TDatabase> _repository = null;
        private IUnitOfWorkCreator<TDatabase> _unitOfWorkCreator = NoTransactionSupport<TDatabase>.Instance;
        public DatabaseMigration(TDatabase db)
        {
            _db = db;
        }

        public static IConfigureMigrationsRunner<TDatabase> ConfigureFor(TDatabase db)
        {
            return new DatabaseMigration<TDatabase>(db);
        }

        public IConfigureMigrationsRunner<TDatabase> SearchAssembly(params Assembly[] asm)
        {
            _asm.AddRange(asm);
            return this;
        }

        public IConfigureMigrationsRunner<TDatabase> SearchAssemblyOf<T>()
        {
            _asm.Add(typeof(T).Assembly);
            return this;
        }

        public IConfigureMigrationsRunner<TDatabase> WithLogger(TextWriter logger)
        {
            logger.MustNotBeNull();
            _log = logger;
            return this;
        }

        public IConfigureMigrationsRunner<TDatabase> WithResolver(IResolveDependencies resolver)
        {
            resolver.MustNotBeNull();
            _resolver = resolver;
            return this;
        }

        public IConfigureMigrationsRunner<TDatabase> WithAutomaticMigrationRepository(IAutomaticMigrationRepository<TDatabase> repository)
        {
            repository.MustNotBeNull();
            _repository = repository;
            return this;
        }
        public IConfigureMigrationsRunner<TDatabase> WithUnitOfWorkCreator(IUnitOfWorkCreator<TDatabase> unitOfWorkCreator)
        {
            unitOfWorkCreator.MustNotBeNull();
            _unitOfWorkCreator = unitOfWorkCreator;
            return this;
        }

        public IManageMigrations<TDatabase> Build()
        {
            if (_resolver == null) throw new InvalidOperationException("Missing dependency resolver");
            var types = _asm
                .SelectMany(a => AssemblyExtensions.GetTypesDerivedFrom<IMigrationTask<TDatabase>>(a, true)
                                                   .Select(t => (IMigrationTask<TDatabase>)_resolver.Resolve(t)))
                .Where(t => t.CurrentVersion != null)
                .ToArray();
            if (types.Length == 0)
            {
                throw new MigrationNotFoundException("None of the provided assemblies contained SqlFu migrations");
            }

            var runner = new MigrationTaskRunner<TDatabase>(_db, _log);

            return new MigrationsManager<TDatabase>(GetSchemaExecutors(types, runner), runner);
        }


        public IAutomaticMigration BuildAutomaticMigrator()
        {
            _repository.MustNotBeNull();
            var manageMigrations = Build();
            return new AutomaticMigration<TDatabase>(_db, _repository, _unitOfWorkCreator, manageMigrations, _log);
        }

        private IEnumerable<IMigrateSchema<TDatabase>> GetSchemaExecutors(IEnumerable<IMigrationTask<TDatabase>> tasks, IRunMigrations<TDatabase> runner)
        {
            var groups = tasks.GroupBy(t => t.SchemaName);
            foreach (var group in groups)
            {
                yield return new SchemaMigrationExecutor<TDatabase>(runner, group, group.Key);
            }
        }

        public void PerformAutomaticMigrations(params string[] schemas)
        {
            var automaticMigrator = BuildAutomaticMigrator();
            automaticMigrator.Execute(schemas.Where(s => !s.Equals(DatabaseMigration<TDatabase>.AutomaticMigrationSchemaName, StringComparison.OrdinalIgnoreCase)).ToArray());
        }
    }
}