using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASC.Model.BaseTypes;
using Microsoft.EntityFrameworkCore;

namespace ASC.DataAccess
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly DbContext _dbContext;

        public Repository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<T> AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.UpdatedDate = DateTime.UtcNow;
            _dbContext.Set<T>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.UpdatedDate = DateTime.UtcNow;
            entity.IsDeleted = true;
            _dbContext.Set<T>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<T?> FindAsync(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                throw new ArgumentNullException("PartitionKey and RowKey cannot be null or empty.");
            return await _dbContext.Set<T>()
                .FirstOrDefaultAsync(t => t.PartitionKey == partitionKey && t.RowKey == rowKey && !t.IsDeleted);
        }

        public async Task<IEnumerable<T>> FindAllByPartitionKeyAsync(string partitionKey)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));
            return await _dbContext.Set<T>()
                .Where(t => t.PartitionKey == partitionKey && !t.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync()
        {
            return await _dbContext.Set<T>()
                .Where(t => !t.IsDeleted)
                .ToListAsync();
        }
    }
}