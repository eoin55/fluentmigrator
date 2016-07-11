using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Base;

namespace FluentMigrator.Runner.Generators.Informix
{
    internal class InformixColumn : ColumnBase
    {
        public InformixColumn()
            : base(new InformixTypeMap(), new InformixQuoter())
        {
            ClauseOrder = new List<Func<ColumnDefinition, string>> { FormatString, FormatType, FormatNullable, FormatDefaultValue };
            AlterClauseOrder = new List<Func<ColumnDefinition, string>> { FormatType, FormatNullable, FormatDefaultValue };
        }

        public List<Func<ColumnDefinition, string>> AlterClauseOrder
        {
            get; set;
        }

        public string FormatAlterDefaultValue(string column, object defaultValue)
        {
            return defaultValue is SystemMethods
                ? FormatSystemMethods((SystemMethods)defaultValue)
                : Quoter.QuoteValue(defaultValue);
        }

        public string GenerateAlterClause(ColumnDefinition column)
        {
            if (column.IsIdentity)
                throw new NotSupportedException("Altering an identity column is not supported.");

            var alterClauses = AlterClauseOrder.Aggregate(new StringBuilder(), (acc, newRow) =>
            {
                var clause = newRow(column);
                if (acc.Length == 0)
                    acc.Append(newRow(column));
                else if (!string.IsNullOrEmpty(clause))
                    acc.Append(clause.PadLeft(clause.Length + 1));

                return acc;
            });

            return string.Format("ALTER COLUMN {0} SET DATA TYPE {1}", Quoter.QuoteColumnName(column.Name), alterClauses);
        }

        protected override string FormatDefaultValue(ColumnDefinition column)
        {
            var isCreate = column.GetAdditionalFeature("IsCreateColumn", false);

            if (isCreate && column.DefaultValue is ColumnDefinition.UndefinedDefaultValue)
                return string.Empty;

            if (column.DefaultValue is ColumnDefinition.UndefinedDefaultValue)
                return string.Empty;

            // see if this is for a system method
            if (!(column.DefaultValue is SystemMethods))
                return "DEFAULT " + Quoter.QuoteValue(column.DefaultValue);

            var method = FormatSystemMethods((SystemMethods)column.DefaultValue);
            if (string.IsNullOrEmpty(method))
                return string.Empty;

            return "DEFAULT " + method;
        }

        protected override string FormatIdentity(ColumnDefinition column)
        {
            throw new NotImplementedException();
        }

        protected override string FormatNullable(ColumnDefinition column)
        {
            if (column.IsNullable.HasValue && column.IsNullable.Value)
                return string.Empty;

            return "NOT NULL";
        }

        protected override string FormatSystemMethods(SystemMethods systemMethod)
        {
            throw new NotImplementedException();
        }

        public override string AddPrimaryKeyConstraint(string tableName, IEnumerable<ColumnDefinition> primaryKeyColumns)
        {
            var keyColumns = string.Join(", ", primaryKeyColumns.Select(x => Quoter.QuoteColumnName(x.Name)).ToArray());

            const string sql = "); ALTER TABLE {0} ADD CONSTRAINT PRIMARY KEY ({1}";
            
            return string.Format(sql, tableName, keyColumns);
        }
    }
}