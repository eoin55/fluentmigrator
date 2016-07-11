using System.Reflection;
using System;
using System.Data.Common;

namespace FluentMigrator.Runner.Processors.Informix
{
    public class InformixDbFactory : ReflectionBasedDbFactory
    {
        public InformixDbFactory()
            : base("IBM.Data.DB2", "IBM.Data.DB2.DB2Factory")
        {
        }

        protected override DbProviderFactory CreateFactory()
        {
            var assembly = AppDomain.CurrentDomain.Load("IBM.Data.DB2, Version=9.7.4.4, Culture=neutral, PublicKeyToken=7c307b91aa13d208");
            var type = assembly.GetType("IBM.Data.DB2.DB2Factory");
            var field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public);

            if (field == null)
                return base.CreateFactory();

            return (DbProviderFactory)field.GetValue(null);
        }
    }
}