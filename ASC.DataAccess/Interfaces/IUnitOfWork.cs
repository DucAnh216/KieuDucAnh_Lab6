using ASC.Model.BaseTypes;

namespace ASC.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : BaseEntity;
       public int CommitTransaction();
    }
}
