using ASC.Model.BaseTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ASC.DataAccess
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly DbContext _dbContext;
        private Dictionary<string, object> _repositories = new Dictionary<string, object>();

        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public int CommitTransaction()
        {
            return _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            string type = typeof(T).Name;

            if (_repositories.ContainsKey(type))
            {
                return (IRepository<T>)_repositories[type];
            }

            var repositoryType = typeof(Repository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _dbContext);

            if (repositoryInstance == null)
            {
                throw new InvalidOperationException($"Không thể khởi tạo repository cho loại {type}");
            }

            _repositories.Add(type, repositoryInstance);
            return (IRepository<T>)repositoryInstance;
        }
    }
}
