using System.Data;

namespace GOBID.Infrastructure.Repositories
{
    public class DapperBaseRepository
    {
        protected readonly IDbConnection _connection;


        public DapperBaseRepository(IDbConnection connection)
        {
            _connection = connection;

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        ~DapperBaseRepository()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Close();
        }


    }
}
