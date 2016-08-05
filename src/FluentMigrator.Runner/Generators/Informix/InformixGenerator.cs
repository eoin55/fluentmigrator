using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Generic;

namespace FluentMigrator.Runner.Generators.Informix
{
    public class InformixGenerator : GenericGenerator
    {
        public InformixGenerator()
            : base(new InformixColumn(), new InformixQuoter(), new EmptyDescriptionGenerator())
        {
        }

        public override string Generate(AlterDefaultConstraintExpression expression)
        {
            return string.Format(
                "ALTER TABLE {0} ALTER COLUMN {1} SET DEFAULT {2}",
                QuoteSchemaAndTable(expression.SchemaName, expression.TableName),
                Quoter.QuoteColumnName(expression.ColumnName),
                ((InformixColumn) Column).FormatAlterDefaultValue(expression.ColumnName, expression.DefaultValue));
        }

        public override string Generate(DeleteDefaultConstraintExpression expression)
        {
            return string.Format(
                "ALTER TABLE {0} ALTER COLUMN {1} DROP DEFAULT",
                QuoteSchemaAndTable(expression.SchemaName, expression.TableName),
                Quoter.QuoteColumnName(expression.ColumnName));
        }

        public override string Generate(RenameTableExpression expression)
        {
            return string.Format(
                "RENAME TABLE {0} TO {1}",
                QuoteSchemaAndTable(expression.SchemaName, expression.OldName),
                Quoter.QuoteTableName(expression.NewName));
        }

        public override string Generate(DeleteColumnExpression expression)
        {
            var builder = new StringBuilder();
            if (expression.ColumnNames.Count == 0 || string.IsNullOrEmpty(expression.ColumnNames.First()))
                return string.Empty;

            builder.AppendFormat("ALTER TABLE {0}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName));
            foreach (var column in expression.ColumnNames)
                builder.AppendFormat(" DROP COLUMN {0}", Quoter.QuoteColumnName(column));

            return builder.ToString();
        }

        public override string Generate(CreateColumnExpression expression)
        {
            expression.Column.AdditionalFeatures.Add(new KeyValuePair<string, object>("IsCreateColumn", true));

            return string.Format(
                "ALTER TABLE {0} ADD {1}",
                QuoteSchemaAndTable(expression.SchemaName, expression.TableName),
                Column.Generate(expression.Column));
        }

        public override string Generate(CreateForeignKeyExpression expression)
        {
            if (expression.ForeignKey.PrimaryColumns.Count != expression.ForeignKey.ForeignColumns.Count)
                throw new ArgumentException("Number of primary columns and secondary columns must be equal");

            var keyName = string.IsNullOrEmpty(expression.ForeignKey.Name)
                ? GenerateForeignKeyName(expression)
                : expression.ForeignKey.Name;
            var keyWithSchema = string.IsNullOrEmpty(expression.ForeignKey.ForeignTableSchema)
                ? Quoter.QuoteConstraintName(keyName)
                : Quoter.QuoteSchemaName(expression.ForeignKey.ForeignTableSchema) + "." + Quoter.QuoteConstraintName(keyName);

            var primaryColumns = expression.ForeignKey.PrimaryColumns.Aggregate(new StringBuilder(), (acc, col) =>
            {
                var separator = acc.Length == 0 ? string.Empty : ", ";
                return acc.AppendFormat("{0}{1}", separator, Quoter.QuoteColumnName(col));
            });

            var foreignColumns = expression.ForeignKey.ForeignColumns.Aggregate(new StringBuilder(), (acc, col) =>
            {
                var separator = acc.Length == 0 ? string.Empty : ", ";
                return acc.AppendFormat("{0}{1}", separator, Quoter.QuoteColumnName(col));
            });

            return string.Format(
                "ALTER TABLE {0} ADD CONSTRAINT FOREIGN KEY ({1}) REFERENCES {2} ({3}) CONSTRAINT {4} {5}",
                QuoteSchemaAndTable(expression.ForeignKey.ForeignTableSchema, expression.ForeignKey.ForeignTable),
                foreignColumns,
                QuoteSchemaAndTable(expression.ForeignKey.PrimaryTableSchema, expression.ForeignKey.PrimaryTable),
                primaryColumns,
                keyWithSchema,
                FormatCascade("DELETE", expression.ForeignKey.OnDelete));
        }

        public override string Generate(CreateConstraintExpression expression)
        {
            var constraintName = !string.IsNullOrEmpty(expression.Constraint.SchemaName) ?
                Quoter.QuoteSchemaName(expression.Constraint.SchemaName) + "." + Quoter.QuoteConstraintName(expression.Constraint.ConstraintName)
                : Quoter.QuoteConstraintName(expression.Constraint.ConstraintName);

            var constraintType = expression.Constraint.IsPrimaryKeyConstraint ? "PRIMARY KEY" : "UNIQUE";
            var quotedNames = expression.Constraint.Columns.Select(q => Quoter.QuoteColumnName(q));
            var columnList = string.Join(", ", quotedNames.ToArray());

            return string.Format(
                "ALTER TABLE {0} ADD CONSTRAINT {1} ({2}) CONSTRAINT {3}",
                QuoteSchemaAndTable(expression.Constraint.SchemaName, expression.Constraint.TableName),
                constraintType,
                columnList,
                constraintName);
        }

        public override string Generate(CreateIndexExpression expression)
        {
            var indexWithSchema = string.IsNullOrEmpty(expression.Index.SchemaName)
                ? Quoter.QuoteIndexName(expression.Index.Name)
                : Quoter.QuoteSchemaName(expression.Index.SchemaName) + "." + Quoter.QuoteIndexName(expression.Index.Name);

            var columnList = expression.Index.Columns.Aggregate(new StringBuilder(), (item, itemToo) =>
            {
                var accumulator = item.Length == 0 ? string.Empty : ", ";
                var direction = itemToo.Direction == Direction.Ascending ? string.Empty : " DESC";

                return item.AppendFormat("{0}{1}{2}", accumulator, Quoter.QuoteColumnName(itemToo.Name), direction);
            });

            return string.Format(
                "CREATE {0}INDEX {1} ON {2} ({3})",
                expression.Index.IsUnique ? "UNIQUE " : string.Empty,
                indexWithSchema,
                QuoteSchemaAndTable(expression.Index.SchemaName, expression.Index.TableName),
                columnList);
        }

        public override string Generate(CreateSchemaExpression expression)
        {
            return string.Format("CREATE SCHEMA {0}", Quoter.QuoteSchemaName(expression.SchemaName));
        }

        public override string Generate(DeleteTableExpression expression)
        {
            return string.Format("DROP TABLE {0}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName));
        }

        public override string Generate(DeleteIndexExpression expression)
        {
            var indexWithSchema = string.IsNullOrEmpty(expression.Index.SchemaName) 
                ? Quoter.QuoteIndexName(expression.Index.Name)
                : Quoter.QuoteSchemaName(expression.Index.SchemaName) + "." + Quoter.QuoteIndexName(expression.Index.Name);

            return string.Format("DROP INDEX {0}", indexWithSchema);
        }

        public override string Generate(DeleteSchemaExpression expression)
        {
            return string.Format("DROP SCHEMA {0} RESTRICT", Quoter.QuoteSchemaName(expression.SchemaName));
        }

        public override string Generate(DeleteConstraintExpression expression)
        {
            var constraintName = string.IsNullOrEmpty(expression.Constraint.SchemaName)
                ? Quoter.QuoteConstraintName(expression.Constraint.ConstraintName)
                : Quoter.QuoteSchemaName(expression.Constraint.SchemaName) + "." + Quoter.QuoteConstraintName(expression.Constraint.ConstraintName);

            return string.Format(
                "ALTER TABLE {0} DROP CONSTRAINT {1}",
                QuoteSchemaAndTable(expression.Constraint.SchemaName, expression.Constraint.TableName),
                constraintName);
        }

        public override string Generate(DeleteForeignKeyExpression expression)
        {
            var constraintName = string.IsNullOrEmpty(expression.ForeignKey.ForeignTableSchema)
                ? Quoter.QuoteConstraintName(expression.ForeignKey.Name)
                : Quoter.QuoteSchemaName(expression.ForeignKey.ForeignTableSchema) + "." + Quoter.QuoteConstraintName(expression.ForeignKey.Name);

            return string.Format(
                "ALTER TABLE {0} DROP FOREIGN KEY {1}",
                QuoteSchemaAndTable(expression.ForeignKey.ForeignTableSchema, expression.ForeignKey.ForeignTable),
                constraintName);
        }

        public override string Generate(DeleteDataExpression expression)
        {
            if (expression.IsAllRows)
                return string.Format("DELETE FROM {0}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName));

            var deleteExpressions = new StringBuilder();
            foreach (var row in expression.Rows)
            {
                var clauses = row.Aggregate(new StringBuilder(), (acc, rowVal) =>
                {
                    var accumulator = acc.Length == 0 ? string.Empty : " AND ";
                    var clauseOperator = rowVal.Value == null ? "IS" : "=";

                    return acc.AppendFormat("{0}{1} {2} {3}", accumulator, Quoter.QuoteColumnName(rowVal.Key), clauseOperator, Quoter.QuoteValue(rowVal.Value));
                });

                var separator = deleteExpressions.Length > 0 ? " " : string.Empty;
                deleteExpressions.AppendFormat("{0}DELETE FROM {1} WHERE {2}", separator, QuoteSchemaAndTable(expression.SchemaName, expression.TableName), clauses);
            }

            return deleteExpressions.ToString();
        }

        public override string Generate(RenameColumnExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("This feature not directly supported by most versions of Informix.");
        }

        public override string Generate(InsertDataExpression expression)
        {
            var sb = new StringBuilder();
            foreach (var row in expression.Rows)
            {
                var columnList = row.Aggregate(new StringBuilder(), (acc, rowVal) =>
                {
                    var accumulator = acc.Length == 0 ? string.Empty : ", ";
                    return acc.AppendFormat("{0}{1}", accumulator, Quoter.QuoteColumnName(rowVal.Key));
                });

                var dataList = row.Aggregate(new StringBuilder(), (acc, rowVal) =>
                {
                    var accumulator = acc.Length == 0 ? string.Empty : ", ";
                    return acc.AppendFormat("{0}{1}", accumulator, Quoter.QuoteValue(rowVal.Value));
                });

                var separator = sb.Length == 0 ? string.Empty : " ";

                sb.AppendFormat(
                    "{0}INSERT INTO {1} ({2}) VALUES ({3})",
                    separator,
                    QuoteSchemaAndTable(expression.SchemaName, expression.TableName),
                    columnList,
                    dataList);
            }

            return sb.ToString();
        }

        public override string Generate(UpdateDataExpression expression)
        {
            var updateClauses = expression.Set.Aggregate(new StringBuilder(), (acc, newRow) =>
            {
                var accumulator = acc.Length == 0 ? string.Empty : ", ";
                return acc.AppendFormat("{0}{1} = {2}", accumulator, Quoter.QuoteColumnName(newRow.Key), Quoter.QuoteValue(newRow.Value));
            });

            if (expression.IsAllRows)
                return string.Format("UPDATE {0} SET {1}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName), updateClauses);

            var whereClauses = expression.Where.Aggregate(new StringBuilder(), (acc, rowVal) =>
            {
                var accumulator = acc.Length == 0 ? string.Empty : " AND ";
                var clauseOperator = rowVal.Value == null ? "IS" : "=";

                return acc.AppendFormat("{0}{1} {2} {3}", accumulator, Quoter.QuoteColumnName(rowVal.Key), clauseOperator, Quoter.QuoteValue(rowVal.Value));
            });

            return string.Format("UPDATE {0} SET {1} WHERE {2}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName), updateClauses, whereClauses);
        }

        public override string Generate(CreateTableExpression expression)
        {
            return string.Format("CREATE TABLE {0} ({1})", QuoteSchemaAndTable(expression.SchemaName, expression.TableName), Column.Generate(expression.Columns, expression.TableName));
        }

        public override string Generate(AlterColumnExpression expression)
        {
            try
            {
                // throws an exception of an attempt is made to alter an identity column, as it is not supported by most version of Informix.
                return string.Format("ALTER TABLE {0} {1}", QuoteSchemaAndTable(expression.SchemaName, expression.TableName), ((InformixColumn) Column).GenerateAlterClause(expression.Column));
            }
            catch (NotSupportedException e)
            {
                return compatabilityMode.HandleCompatabilty(e.Message);
            }
        }

        public override string Generate(AlterSchemaExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("This feature not directly supported by most versions of Informix.");
        }

        private string QuoteSchemaAndTable(string schemaName, string tableName)
        {
            // appends a schema, if provided. If not, the provider will use the schema specified in the connection string.
            return !string.IsNullOrEmpty(schemaName) ? Quoter.QuoteSchemaName(schemaName) + "." + Quoter.QuoteTableName(tableName) : Quoter.QuoteTableName(tableName);
        }
    }
}