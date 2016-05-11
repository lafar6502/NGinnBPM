using System;
using System.Data.Common;
using System.Configuration;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlSession : DbSession
    {
        private readonly bool _disposeConn;

        public SqlSession(DbConnection conn, bool disposeIt = true)
        {
            Connection = conn;
            _disposeConn = disposeIt;
        }

        public new DbConnection Connection { get; private set; }

        public override void Dispose()
        {
            base.Dispose();
            if (!_disposeConn || Connection == null) return;
            Connection.Dispose();
            Connection = null;
        }

        
    }

    public class SqlDbSessionFactory : IDbSessionFactory
    {
        public string ConnectionString { get; set; }

        
        public DbSession OpenSession()
        {
            if (string.IsNullOrEmpty(ConnectionString)) throw new Exception("ConnectionString");
            var cs = ConfigurationManager.ConnectionStrings[ConnectionString];
            var fac = DbProviderFactories.GetFactory(string.IsNullOrEmpty(cs?.ProviderName) ? "System.Data.SqlClient" : cs.ProviderName);
            var conn = fac.CreateConnection();
            conn.ConnectionString = cs == null ? ConnectionString : cs.ConnectionString;
            conn.Open();
            return new SqlSession(conn);
        }

        public DbSession OpenSession(object connection)
        {
            var c = connection as DbConnection;
            if (c == null || c.State != System.Data.ConnectionState.Open)
            {
                return OpenSession();
            };
            var cse = ConfigurationManager.ConnectionStrings[this.ConnectionString];
            var cs = cse == null ? this.ConnectionString : cse.ConnectionString;
            return !SqlUtil.IsSameDatabaseConnection(cs, c.ConnectionString) ? OpenSession() : new SqlSession(c, false);
        }
    }
}
