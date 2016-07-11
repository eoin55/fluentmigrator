using FluentMigrator.Runner.Generators.Base;
using System.Data;

namespace FluentMigrator.Runner.Generators.Informix
{
    internal class InformixTypeMap : TypeMapBase
    {
        protected override void SetupTypeMaps()
        {
            SetTypeMap(DbType.AnsiString, "VARCHAR(255)");
            SetTypeMap(DbType.AnsiString, "VARCHAR($size)", 255);
            SetTypeMap(DbType.AnsiStringFixedLength, "CHAR(32767)");
            SetTypeMap(DbType.AnsiStringFixedLength, "CHAR($size)", 32767);
            SetTypeMap(DbType.Binary, "BLOB");
            SetTypeMap(DbType.Boolean, "BOOLEAN");
            SetTypeMap(DbType.Byte, "BYTE");
            SetTypeMap(DbType.Date, "DATE");
            SetTypeMap(DbType.DateTime, "DATETIME YEAR TO FRACTION");
            SetTypeMap(DbType.Decimal, "DECIMAL(19,5)");
            SetTypeMap(DbType.Decimal, "DECIMAL($size,$precision)", 31);
            SetTypeMap(DbType.Int16, "SMALLINT");
            SetTypeMap(DbType.Int32, "INT");
            SetTypeMap(DbType.Int64, "BIGINT");
            SetTypeMap(DbType.Single, "FLOAT");
            SetTypeMap(DbType.String, "NVARCHAR(255)");
            SetTypeMap(DbType.String, "NVARCHAR($size)", 255);
            SetTypeMap(DbType.String, "LVARCHAR($size)", int.MaxValue);
            SetTypeMap(DbType.StringFixedLength, "NCHAR(32767)");
            SetTypeMap(DbType.StringFixedLength, "NCHAR($size)", 32767);
        }
    }
}