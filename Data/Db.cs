using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AuthSystemApi.Data   
{
    public class DbHelper       
    {
        private readonly IConfiguration _configuration;

        public DbHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")
            );
        }
    }
}
