using RedDog.Search;
using RedDog.Search.Http;
using RedDog.Search.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macaw.AbstractMigrator.Tests
{
        public class AzureSearchConnection : DbConnectionStringBuilder
        {
            public string Service 
            {
                get 
                {
                    object service = null;
                    return this.TryGetValue("Service", out service) ? service.ToString() : null;
                }
                set
                {
                    this["Service"] = value;
                }
            }
            public string Key
            {
                get
                {
                    object key = null;
                    return this.TryGetValue("Key", out key) ? key.ToString() : null;
                }
                set
                {
                    this["Key"] = value;
                }
            }
        }

        
        [Migration("1.0.0", SchemaName = "AutomaticMigration")]
        public class AzureSearchAutomaticMigrationSetupTask : AbstractMigrationTask<ApiConnection>
        {
            /// <summary>
            /// Task is executed automatically in a transaction
            /// </summary>
            /// <param name="db"/>
            public override void Execute(ApiConnection db)
            {
                var client = new IndexManagementClient(db);
                var result = client.CreateIndexAsync(new Index("automaticmigration")
                    .WithStringField("Id", f => f
                        .IsKey()
                        .IsRetrievable())
                    .WithStringField("SchemaName", f => f
                        .IsSearchable()
                        .IsRetrievable())
                    .WithStringField("Version", f => f
                        .IsRetrievable())
                    .WithDateTimeField("TimeOfUpdate", f => f
                        .IsRetrievable()
                        .IsSortable())).Result;

                if (!result.IsSuccess)
                    throw new ApplicationException("Could not create automaticmigration index: " + result.Error.Message);
            }
        }


        [Migration("1.1.0", SchemaName = "PlopSchema")]
        public class AzureSearchMigration1 : AbstractMigrationTask<ApiConnection>
        {
            public override void Execute(ApiConnection db)
            {
                var client = new IndexManagementClient(db);
                var result = client.CreateIndexAsync(new Index("plopindex")
                    .WithStringField("Id", f => f
                        .IsKey()
                        .IsRetrievable())
                    .WithStringField("PlopField", f => f
                        .IsSearchable()
                        .IsRetrievable())
                    .WithStringField("AddedField", f => f
                        .IsSearchable()
                        .IsRetrievable())
                    .WithDateTimeField("SomeTime", f => f
                        .IsRetrievable())).Result;
                if (!result.IsSuccess)
                    throw new ApplicationException("Could not create plopindex index: " + result.Error.Message);
            }
        }

        [Migration("1.0.1", "1.1.0", SchemaName = "PlopSchema")]
        public class Migration2 : AbstractMigrationTask<ApiConnection>
        {
            public override void Execute(ApiConnection db)
            {
                var client = new IndexManagementClient(db);
                var result = client.UpdateIndexAsync(new Index("plopindex")
                    .WithStringField("Id", f => f
                        .IsKey()
                        .IsRetrievable())
                    .WithStringField("PlopField", f => f
                        .IsSearchable()
                        .IsRetrievable())
                    .WithStringField("AddedField", f => f
                        .IsSearchable()
                        .IsRetrievable())
                    .WithDateTimeField("SomeTime", f => f
                        .IsRetrievable())).Result;
                if (!result.IsSuccess)
                    throw new ApplicationException("Could not create plopindex index: " + result.Error.Message);
            }
        }


        public class AzureSearchAutomaticMigrationRepo : IAutomaticMigrationRepository<ApiConnection>
        {
            ApiConnection _db;
            public AzureSearchAutomaticMigrationRepo(ApiConnection db)
            {
                _db = db;
            }

            public bool IsMigrationsInitialized()
            {
                var client = new IndexManagementClient(_db);
                var result = client.GetIndexAsync("automaticmigration").Result;

                return result.IsSuccess;
            }

            public void UntrackSchemas(IEnumerable<string> schemas)
            {
                var queryClient = new IndexQueryClient(_db);
                foreach (var schema in schemas)
                {
                    var migrations = queryClient.SearchAsync("automaticmigration", new SearchQuery(schema)
                        .OrderBy("TimeOfUpdate")
                        .SearchField("SchemaName")).Result;

                    if (!migrations.IsSuccess)
                        throw new ApplicationException("Error untracking schema: could not find migrations in schema " + schema + ", error: " + migrations.Error.Message);

                    if (migrations.Body != null)
                    {
                        var indexClient = new IndexManagementClient(_db);
                        var result = indexClient.PopulateAsync("automaticmigration",
                            migrations.Body.Records.Select(record =>
                                new IndexOperation(IndexOperationType.Delete, "Id", record.Properties["Id"].ToString())).ToArray()).Result;

                    }
                }
            }

            public void AppendVersion(string schema, string version)
            {
                var client = new IndexManagementClient(_db);
                var result = client.PopulateAsync("automaticmigration",
                    new IndexOperation(IndexOperationType.Upload, "Id", Guid.NewGuid().ToString())
                    .WithProperty("SchemaName", schema)
                    .WithProperty("Version", version)
                    .WithProperty("TimeOfUpdate", DateTimeOffset.Now)).Result;
                if (!result.IsSuccess)
                    throw new ApplicationException("Could not store migration in AutomaticMigration index. Error: " + result.Error.Message);
            }

            public string GetInstalledVersion(string schema)
            {
                var queryClient = new IndexQueryClient(_db);
                var results = queryClient.SearchAsync("automaticmigration", new SearchQuery(schema)
                    .OrderBy("TimeOfUpdate")
                    .SearchField("SchemaName")
                    .Top(1)
                    .Count(true)).Result;

                if (results == null || results.IsSuccess == false || results.Body == null || results.Body.Count < 1)
                    return null;

                return results.Body.Records.SingleOrDefault().Properties["Version"].ToString();
            }
        }


        public class AzureSearchMigrator
        {
            public static void RunMigrations(string connectionString)
            {
                var connection = new AzureSearchConnection();
                connection.ConnectionString = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;

                using (var db = ApiConnection.Create(connection.Service, connection.Key))
                {
                    DatabaseMigration<ApiConnection>
                        .ConfigureFor(db)
                        .SearchAssemblyOf<SqlFuMigration1>()
                        .WithAutomaticMigrationRepository(new AzureSearchAutomaticMigrationRepo(db))
                        .PerformAutomaticMigrations();
                }
            }
        }
}
