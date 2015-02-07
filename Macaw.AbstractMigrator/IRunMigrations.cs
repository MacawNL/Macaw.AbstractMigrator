
namespace Macaw.AbstractMigrator
{
    public interface IRunMigrations<TDatabase> where TDatabase : class
    {
        void Run(params IMigrationTask<TDatabase>[] tasks);
    }
}