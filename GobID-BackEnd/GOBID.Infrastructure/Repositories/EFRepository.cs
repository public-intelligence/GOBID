using GOBID.Application.Interfaces.Repositories;
using GOBID.Application.Wrappers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GOBID.Infrastructure.Repositories
{
    public class EFRepository<T> : IRepositoryAsync<T> where T : class
    {
        protected readonly DbContext _dbContext;

        public EFRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual IQueryable<T> OdataQuery()
        {
            return _dbContext.Set<T>().AsQueryable();
        }

        public virtual T GetById(dynamic id)
        {

            return _dbContext.Set<T>().Find(id);
        }

        public virtual async Task<T> GetByIdAsync(dynamic id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public IEnumerable<T> ListAll()
        {
            return _dbContext.Set<T>().AsEnumerable();
        }

        public async Task<List<T>> ListAllAsync(IEnumerable<Expression<Func<T, object>>>? includes = null)
        {
            var query = _dbContext.Set<T>().AsQueryable();
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            return await query.ToListAsync();
        }

        public async Task<List<T>> WhereAllAsync(Expression<Func<T, bool>>? where = null, IEnumerable<Expression<Func<T, object>>>? includes = null)
        {
            var query = _dbContext.Set<T>().AsQueryable();
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return await query.Where(where!).AsNoTracking().ToListAsync();
        }

        public async Task<T> WhereAsync(Expression<Func<T, bool>>? where = null, IEnumerable<Expression<Func<T, object>>>? includes = null)
        {
            var query = _dbContext.Set<T>().AsQueryable();
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return (await query.Where(where!).AsNoTracking().FirstOrDefaultAsync())!;
        }

        public async Task<bool> Exists(Expression<Func<T, bool>>? where = null)
        {
            return await _dbContext.Set<T>().AnyAsync(where!);
        }

        public T Add(T entity)
        {
            _dbContext.Set<T>().Add(entity);
            _dbContext.SaveChanges();

            return entity;
        }

        public async Task<T> AddAsync(T entity)
        {
            _dbContext.Set<T>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public void Update(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public void Delete(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            _dbContext.SaveChanges();
        }

        public async Task<T> DeleteAsync(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task<PagedResponse<T>> GetPagedList(int pageNumber, int pageSize, List<Expression<Func<T, bool>>>? filter = null,
                                   Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
                                   IEnumerable<Expression<Func<T, object>>>? includes = null,
                                   Expression<Func<T, T>>? selector = null)
        {
            var query = _dbContext.Set<T>().AsQueryable();
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            if (filter != null)
            {
                foreach (var fil in filter)
                {
                    query = query.Where(fil);
                }
            }
            int totalCount = query.Count();

            if (orderBy != null)
            {
                query = orderBy(query);
            }
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            //Controlling overflow//
            pageNumber = (pageNumber <= 0) ? 1 : pageNumber;
            pageSize = (pageSize > 100 || pageSize <= 0) ? 10 : pageSize;
            pageSize = (pageSize > totalCount) ? totalCount : pageSize;
            pageSize = (totalCount == 0) ? 10 : pageSize;
            //Controlling overflow//

            if (selector != null)
            {
                query = query.Select(selector).Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }
            else
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }
            var pageData = await query.ToListAsync();

            return new PagedResponse<T>(pageData, pageNumber, pageSize, totalCount, totalPages);
        }
    }
}
