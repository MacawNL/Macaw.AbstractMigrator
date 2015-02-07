Macaw AbstractMigrator
======================

Over the past years of developing cloud-based solutions, I've become very attached to the concept of automatic migrations. Never again will I do a project 
without it. The idea of deploying any kind of software and having to manually upgrade the database with some (poorly tested) scripts while the system is down 
would make me extremely unconfortable. The concept of downtime during deployments is clearly a thing of the past.

For relational databases such as SQL Server, there are awesome Migration libraries. My favourite is Insight.Database.Schema, but I've also used SqlFu and 
FluentMigrator and they do the job as well.

Lately however, I've crossed paths with a few nonrelational databases, such as OrientDB and Azure Search. These are not based on SQL, but they do 
let you create schema, indexes, procedures, scripts etc. .NET support for these databases is very basic: usually just a small wrapper around a REST interface or 
Socket connection, there's no Linq, no Entity Framework, no DbConnection, no supporty in Micro ORM's and no fast object mapping. And for sure, there is no 
support for automatic migrations.

I order to get migrations and be able to deploy to these newfangled databases with confidence, I 've created Macaw.AbstractMigrations. It's a blatant ripoff 
of the migrations code in SqlFu, but I abstracted all references to SqlFu itself and to DbConnection and replaced it with a generic &lt;TDatabase&gt; parameter so 
that I'm left with just the generic Migration framework. There are no dependencies other than basic .NET.

In order to use it, you need to supply several things:
## (one or more) MigrationTasks
These are classes that derive from AbstractMigrationTask&lt;TDatabase&gt; and are annotated with [Migration("[from version]", "[ to version]", SchemaName = "[your schema name]")]

Per schema, you always provide one MigrationTask with a single version number: this migration should directly create the latest schema and have the highest 
version number. In a non-initialized database, this task will be the only one that gets run (note that this is differerent from other Migration systems, that run 
all migrations on a new database). 
Then you can provide any number of MigrationTasks with two version numbers: from and to. These numbers lead up to the latest version which should match the number of 
the single-version migration. The upgrade algorithm is not very smart: it does not compensate for overlapping upgrades etc. It just executes them in order.

In a normal migration framework it would stop here and you'd be able to run your migrations from a deployment script or from inside your own code.
However, because Macaw AbstractMigrator does not know your database you need to supply a few more things:

## AutomaticMigration repository
You supply an implementation of IAutomaticMigrationRepository&lt;TDatabase&lt;. This repository is used to store the upgrade history and current state of 
your schemas.

## A MigrationTask for your AutomaticMigration repository
Probably the system needs to prepare a table before the repository functions can work. So you need to provide another MigrationTask, marked with the 
reserved SchemaName "AutomaticMigration". It follows the exact same rules as any other MigrationTask.

## (optionally) A IUnitOfWorkCreator
If your database supports transactions, you should implement and supply a function that creates one. The function should return an IUnitOfWork, which
(for a relational database) wraps a transaction object and implements the Commit function with it.


### Licence
MIT

