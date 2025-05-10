using ASC.Model.BaseTypes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASC.DataAccess
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> FindAsync(string partitionKey, string rowKey);
        Task<IEnumerable<T>> FindAllByPartitionKeyAsync(string partitionKey);
        Task<IEnumerable<T>> FindAllAsync();
        Task<T> AddAsync(T entity);
        Task Update(T entity);
        Task Delete(T entity);
    }
}