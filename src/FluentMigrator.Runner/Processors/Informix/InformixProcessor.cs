using System;
using System.Data;
using System.Linq;
using FluentMigrator.Builders.Execute;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.Informix;
using FluentMigrator.Runner.Helpers;

namespace FluentMigrator.Runner.Processors.Informix
{
    public class InformixProcessor : GenericProcessorBase
    {
        public InformixProcessor(IDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options, IDbFactory factory)
            : base(connection, factory, generator, announcer, options)
        {
            Quoter = new InformixQuoter();
        }

        public override string DatabaseType
        {
            get { return "IBM Informix"; }
        }

        public IQuoter Quoter { get; set; }

        public override bool SupportsTransactions
        {
            get { return true; }
        }

        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "t.owner = '" + FormatToSafeName(schemaName) + "' AND ";

            const string sql = "SELECT c.colname FROM systables AS t INNER JOIN syscolumns AS c ON t.tabid = c.tabid WHERE {0} t.tabname = '{1}' AND c.colname = '{2}'";
            var exists = Exists(sql, schema, FormatToSafeName(tableName), FormatToSafeName(columnName));
            return exists;
        }

        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "t.owner = '" + FormatToSafeName(schemaName) + "' AND ";

            const string sql = "SELECT c.constrname FROM sysconstraints c INNER JOIN systables t ON t.tabid = c.tabid WHERE {0} t.tabname = '{1}' AND c.constrname = '{2}'";
            var exists = Exists(sql, schema, FormatToSafeName(tableName), FormatToSafeName(constraintName));
            return exists;
        }

        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "t.owner = '" + FormatToSafeName(schemaName) + "' AND ";
            var defaultValueAsString = string.Format("%{0}%", FormatHelper.FormatSqlEscape(defaultValue.ToString()));

            const string sql = "SELECT c.default FROM systables AS t INNER JOIN sysdefaults AS c ON t.tabid = c.tabid WHERE {0} t.tabname = '{1}' AND c.default = '{2}'";
            var exists = Exists(sql, schema, FormatToSafeName(tableName), columnName.ToUpper(), defaultValueAsString);
            return exists;
        }

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(string.Format(template, args), Connection, Transaction))
            using (var reader = command.ExecuteReader())
                return reader.Read();
        }

        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            var schema = !string.IsNullOrEmpty(schemaName) ? Quoter.QuoteSchemaName(schemaName) + "." : string.Empty;

            const string sql = "SELECT * FROM sysindexes i INNER JOIN systables t ON t.tabid = i.tabid WHERE {0} AND t.tabname = '{1}' AND i.idxname = '{2}'";
            var exists = Exists(sql, schema, FormatToSafeName(tableName), FormatToSafeName(indexName));
            return exists;
        }

        public override void Process(PerformDBOperationExpression expression)
        {
            Announcer.Say("Performing DB Operation");

            if (Options.PreviewOnly)
                return;

            EnsureConnectionIsOpen();

            expression.Operation(Connection, Transaction);
        }

        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            var ds = new DataSet();
            using (var command = Factory.CreateCommand(string.Format(template, args), Connection, Transaction))
            {
                var adapter = Factory.CreateDataAdapter(command);
                adapter.Fill(ds);
                return ds;
            }
        }

        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            var schemaAndTable = !string.IsNullOrEmpty(schemaName) ? Quoter.QuoteSchemaName(schemaName) + "." + Quoter.QuoteTableName(tableName) : Quoter.QuoteTableName(tableName);
            return Read("SELECT * FROM {0}", schemaAndTable);
        }

        public override bool SchemaExists(string schemaName)
        {
            throw new NotImplementedException();
        }

        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        public override bool TableExists(string schemaName, string tableName)
        {
            var schema = string.IsNullOrEmpty(schemaName) ? string.Empty : "OWNER = '" + FormatToSafeName(schemaName) + "' AND ";

            return Exists("SELECT TABNAME FROM SYSTABLES WHERE {0} TABNAME = '{1}'", schema, FormatToSafeName(tableName));
        }

        protected override void Process(string sql)
        {
            Announcer.Sql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
                return;

            EnsureConnectionIsOpen();

            using (var command = Factory.CreateCommand(sql, Connection, Transaction))
            {
                command.CommandTimeout = Options.Timeout;
                command.ExecuteNonQuery();
            }
        }

        private string FormatToSafeName(string sqlName)
        {
            var rawName = Quoter.UnQuote(sqlName);

            return rawName.Contains('\'') ? FormatHelper.FormatSqlEscape(rawName).ToLower() : rawName.ToLower();
        }
    }
}