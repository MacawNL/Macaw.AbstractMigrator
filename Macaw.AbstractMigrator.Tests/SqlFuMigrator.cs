using Macaw.AbstractMigrator;
using SqlFu;
using SqlFu.DDL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Macaw.AbstractMigrator.Tests
{
    [Table("SqlFuAutomaticMigration")]
    internal class MigrationTrack
    {
        public MigrationTrack()
        {
            TimeOfUpdate = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public string SchemaName { get; set; }
        public string Version { get; set; }
        public DateTime TimeOfUpdate { get; set; }
    }

    [Migration("1.0.0", SchemaName = "AutomaticMigration")]
    public class SqlFuAutomaticMigrationSetupTask : AbstractMigrationTask<SqlFu.SqlFuConnection>
    {
        /// <summary>
        /// Task is executed automatically in a transaction
        /// </summary>
        /// <param name="db"/>
        public override void Execute(SqlFu.SqlFuConnection db)
        {
            var tbl = db.DatabaseTools.GetCreateTableBuilder("SqlFuAutomaticMigration", IfTableExists.Ignore);
            tbl.Columns
               .Add("Id", DbType.Int32, isNullable: false, autoIncrement: true).AsPrimaryKey()
               .Add("SchemaName", DbType.String, "50")
               .Add("Version", DbType.AnsiString, size: "25", isNullable: false)
               .Add("TimeOfUpdate", DbType.DateTime, isNullable: false);
            tbl.ExecuteDDL();
        }
    }


    [Migration("1.0.0", SchemaName = "PlopSchema")]
    public class SqlFuMigration1 : AbstractMigrationTask<SqlFu.SqlFuConnection>
    {
        public override void Execute(SqlFuConnection db)
        {
            var tbl = db.DatabaseTools.GetCreateTableBuilder("PlopTable", IfTableExists.Ignore);
            tbl.Columns
               .Add("Id", DbType.Int32, isNullable: false, autoIncrement: true).AsPrimaryKey()
               .Add("PlopField", DbType.String, "50")
               .Add("AddField", DbType.String, "50")
               .Add("SomeTime", DbType.DateTime, isNullable: false);
            tbl.ExecuteDDL();
        }
    }

    //[Migration("1.0.0", "1.0.1", SchemaName = "PlopSchema")]
    //public class Migration2 : AbstractMigrationTask<SqlFu.SqlFuConnection>
    //{
    //    public override void Execute(SqlFuConnection db)
    //    {
    //        var tbl = db.DatabaseTools.GetCreateTableBuilder("PlopTable", IfTableExists.Ignore);
    //        tbl.Columns
    //           .Add("AddField", DbType.String, "50");
    //        tbl.ExecuteDDL();
    //    }
    //}

    public class DatabaseUnitOfWork : IUnitOfWork
    {
        private DbTransaction _trans;

        public DatabaseUnitOfWork(DbTransaction trans)
        {
            if (trans == null) throw new ArgumentException("trans");
            _trans = trans;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_trans != null)
            {
                _trans.Dispose();
                _trans = null;
            }
        }

        public void Commit()
        {
            _trans.Commit();
        }

        public string Tag { get; set; }
    }

    public class DatabaseTransactionManager : IUnitOfWorkCreator<SqlFuConnection>
    {
        public IUnitOfWork StartUnitOfWork(SqlFuConnection db)
        {
            return new DatabaseUnitOfWork(db.BeginTransaction());
        }
    }

    public class SqlFuAutomaticMigrationRepo : IAutomaticMigrationRepository<SqlFu.SqlFuConnection>
    {
        SqlFu.SqlFuConnection _db;
        public SqlFuAutomaticMigrationRepo(SqlFu.SqlFuConnection db)
        {
            _db = db;
        }

        public bool IsMigrationsInitialized()
        {
            return _db.DatabaseTools.TableExists("SqlFuAutomaticMigration");
        }

        public void UntrackSchemas(IEnumerable<string> schemas)
        {
            _db.ExecuteCommand("delete from " + _db.EscapeIdentifier("SqlFuAutomaticMigration") + String.Format(" where {0} in (@0)", _db.EscapeIdentifier("SchemaName")), schemas.ToList());
        }

        public void AppendVersion(string schema, string version)
        {
            _db.Insert(new MigrationTrack
            {
                SchemaName = schema,
                Version = version
            });

        }

        public string GetInstalledVersion(string schema)
        {
            return _db.GetValue<string>("select " + _db.EscapeIdentifier("Version") + " from " + _db.EscapeIdentifier("SqlFuAutomaticMigration") + String.Format(" where {0}=@0 order by {1} desc", _db.EscapeIdentifier("SchemaName"), _db.EscapeIdentifier("Id")), schema);
        }

        public IUnitOfWork StartUnitOfWork()
        {
            return new DatabaseUnitOfWork(_db.BeginTransaction());
        }
    }

    
    public class MySqlFuMigrator 
    {
        public static void RunMigrations(string connectionString)
        {
            using (var db = new SqlFuConnection(connectionString))
            {
                DatabaseMigration<SqlFuConnection>
                    .ConfigureFor(db)
                    .SearchAssemblyOf<SqlFuMigration1>()
                    .WithAutomaticMigrationRepository(new SqlFuAutomaticMigrationRepo(db))
                    .WithUnitOfWorkCreator(new DatabaseTransactionManager())
                    .PerformAutomaticMigrations();
            }
        }

    }


}
