using System.Reflection;

namespace Macaw.AbstractMigrator
{
    public abstract class AbstractMigrationTask<TDatabase> : IMigrationTask<TDatabase> where TDatabase : class
    {
        public AbstractMigrationTask()
        {
            var attr = GetType().GetSingleAttribute<MigrationAttribute>();
            if (attr != null)
            {
                CurrentVersion = attr.From;
                NextVersion = attr.To;
                SchemaName = attr.SchemaName;
                Priority = attr.Priority;
            }
        }

        public SemanticVersion CurrentVersion { get; private set; }
        public SemanticVersion NextVersion { get; private set; }
        public string SchemaName { get; private set; }
        public abstract void Execute(TDatabase db);
        public int Priority { get; private set; }
    }
}