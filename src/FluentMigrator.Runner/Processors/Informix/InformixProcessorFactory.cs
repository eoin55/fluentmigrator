using FluentMigrator.Runner.Generators.Informix;

namespace FluentMigrator.Runner.Processors.Informix
{
    public class InformixProcessorFactory : MigrationProcessorFactory
    {
        public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
        {
            var factory = new InformixDbFactory();
            var connection = factory.CreateConnection(connectionString);
            return new InformixProcessor(connection, new InformixGenerator(), announcer, options, factory);
        }
    }
}