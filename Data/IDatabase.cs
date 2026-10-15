using System.Data;
using System.Data.Common;
namespace EmailWebhooks.Data
{
    public interface IDatabase
    {

        DbConnection CreateConnection();

        void Initialize();

        Task<IEnumerable<T>> QueryAsync<T>(string sql,object? parameters = null);
        Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null);

        
    }
}