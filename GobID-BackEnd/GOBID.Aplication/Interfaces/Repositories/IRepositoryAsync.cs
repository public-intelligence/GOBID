using GOBID.Application.Wrappers;
using System.Linq.Expressions;

namespace GOBID.Application.Interfaces.Repositories
{
    public interface IRepositoryAsync<T> where T : class
    {
        IQueryable<T> OdataQuery();
        Task<T> GetByIdAsync(dynamic id);
        Task<List<T>> ListAllAsync(IEnumerable<Expression<Func<T, object>>>? includes = null);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<T> DeleteAsync(T entity);
        Task<T> WhereAsync(Expression<Func<T, bool>>? where = null, IEnumerable<Expression<Func<T, object>>>? includes = null);
        Task<List<T>> WhereAllAsync(Expression<Func<T, bool>>? where = null, IEnumerable<Expression<Func<T, object>>>? includes = null);
        Task<bool> Exists(Expression<Func<T, bool>>? where = null);
        Task<PagedResponse<T>> GetPagedList(int pageNumber, int pageSize, List<Expression<Func<T, bool>>>? filter = null,
                Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                IEnumerable<Expression<Func<T, object>>>? includes = null,
                Expression<Func<T, T>>? selector = null);

    }
}
