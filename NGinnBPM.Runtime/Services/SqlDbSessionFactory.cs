using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Configuration;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlSession : DbSession
    {
        private DbConnection _conn;
        private bool _disposeConn = true;

        public SqlSession(DbConnection conn, bool disposeIt = true)
        {
            _conn = conn;
            _disposeConn = disposeIt;
        }

        public DbConnection Connection
        {
            get { return _conn; }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_disposeConn && _conn != null)
            {
                _conn.Dispose();
                _conn = null;
            }
        }

        
    }

    public class SqlDbSessionFactory : IDbSessionFactory
    {
        public string ConnectionString { get; set; }

        public DbSession OpenSession()
        {
            if (string.IsNullOrEmpty(ConnectionString)) throw new Exception("ConnectionString");
            var cs = ConfigurationManager.ConnectionStrings[this.ConnectionString];
            var fac = DbProviderFactories.GetFactory(cs == null || string.IsNullOrEmpty(cs.ProviderName) ? "System.Data.SqlClient" : cs.ProviderName);
            var conn = fac.CreateConnection();
            conn.ConnectionString = cs == null ? this.ConnectionString : cs.ConnectionString;
            conn.Open();
            return new SqlSession(conn, true);
        }
    }
}
