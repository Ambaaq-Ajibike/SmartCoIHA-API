using System.Linq.Expressions;

namespace Application.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<T> GetByExpressionAsync(Expression<Func<T, bool>> exp);
        Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> exp);
        Task<T> AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync();
    }
}
