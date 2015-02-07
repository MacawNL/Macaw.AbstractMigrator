using System.Reflection;
using System.IO;
using Macaw.AbstractMigrator.Automatic;

namespace Macaw.AbstractMigrator
{
    public interface IConfigureMigrationsRunner<TDatabase> where TDatabase : class
    {
        IConfigureMigrationsRunner<TDatabase> SearchAssembly(params Assembly[] asm);
        IConfigureMigrationsRunner<TDatabase> SearchAssemblyOf<T>();
        IConfigureMigrationsRunner<TDatabase> WithLogger(TextWriter logger);
        IConfigureMigrationsRunner<TDatabase> WithResolver(IResolveDependencies resolver);
        IConfigureMigrationsRunner<TDatabase> WithAutomaticMigrationRepository(IAutomaticMigrationRepository<TDatabase> repository);
        IConfigureMigrationsRunner<TDatabase> WithUnitOfWorkCreator(IUnitOfWorkCreator<TDatabase> unitOfWorkCreator);
        IManageMigrations<TDatabase> Build();

        IAutomaticMigration BuildAutomaticMigrator();

        /// <summary>
        /// Tries to install/update all the specified schemas.
        /// If no schema is specified it tries to process all schemas found
        /// </summary>
        /// <param name="schemas"></param>
        void PerformAutomaticMigrations(params string[] schemas);
    }
}