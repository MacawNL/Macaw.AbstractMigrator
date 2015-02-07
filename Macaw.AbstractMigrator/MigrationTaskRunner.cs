using System;
using System.Collections.Generic;
using System.IO;

namespace Macaw.AbstractMigrator
{
    public class MigrationTaskRunner<TDatabase> : IRunMigrations<TDatabase> where TDatabase : class
    {
        private readonly TDatabase _db;
        private readonly TextWriter _logger;

        public MigrationTaskRunner(TDatabase db, TextWriter logger)
        {
            _db = db;
            _logger = logger;
        }

        public void Run(params IMigrationTask<TDatabase>[] tasks)
        {
            if (tasks.IsNullOrEmpty()) return;
            //using (var t = _db.BeginTransaction())
            //{
                foreach (var task in tasks)
                {
                    if (task.NextVersion == null)
                    {
                        _logger.WriteLine("Installing database schema '{1}' with version {0}", task.CurrentVersion,
                                     task.SchemaName);
                    }
                    else
                    {
                        _logger.WriteLine("Executing '{2}' migration from version {0} to version {1}", task.CurrentVersion,
                                     task.NextVersion, task.SchemaName);
                    }
                    task.Execute(_db);
//                 }
//                _db.Transaction.Commit();
            }
        }
    }
    public interface IUnitOfWork : IDisposable
    {
        void Commit();
        string Tag { get; set; }
    }

    public interface IUnitOfWorkCreator<TDatabase>
    {
        IUnitOfWork StartUnitOfWork(TDatabase db);
    }

    public class NoTransaction : IUnitOfWork
    {
        public void Commit()
        {
        }

        public string Tag { get; set; }

        public void Dispose()
        {
        }
    }

    public class NoTransactionSupport<TDatabase> : IUnitOfWorkCreator<TDatabase> where TDatabase : class 
    {
        static Lazy<NoTransactionSupport<TDatabase>> lazyValue = new Lazy<NoTransactionSupport<TDatabase>>(() => new NoTransactionSupport<TDatabase>());
        public static NoTransactionSupport<TDatabase> Instance 
        {
            get
            {
                return lazyValue.Value;
            }
        }
        public IUnitOfWork StartUnitOfWork(TDatabase db)
        {
            return new NoTransaction();
        }
    }

}