using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace CaptaVale.Conexion
{
    public class ConexionSql
    {
        private static string _connectionString = "Server=tcp:captavale.database.windows.net,1433;Initial Catalog=CaptaVale;Persist Security Info=False;User ID=consulta;Password=KarinaG_123!!!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        public static SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(_connectionString);
            return connection;
        }

        public bool OpenConnection()
        {
            SqlConnection connection = GetConnection();
            connection.Open();
            return true;
        }

        public void CloseConnection()
        {
            using (SqlConnection connection = GetConnection())
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

}
