namespace Macaw.AbstractMigrator
{
    public interface IMigrationTask<TDatabase> where TDatabase: class
    {
        /// <summary>
        /// Gets semantic version to upgrade from
        /// </summary>
        SemanticVersion CurrentVersion { get; }

        /// <summary>
        /// Gets semantic version to upgrade to
        /// </summary>
        SemanticVersion NextVersion { get; }

        string SchemaName { get; }

        /// <summary>
        /// Task is executed automatically in a transaction
        /// </summary>
        /// <param name="db"></param>
        void Execute(TDatabase db);

        int Priority { get; }
    }
}