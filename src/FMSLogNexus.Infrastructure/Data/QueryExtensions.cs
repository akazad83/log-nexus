using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data;

/// <summary>
/// Extension methods for common query patterns.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Applies pagination to a query.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to paginate.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated query.</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Applies ordering to a query.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <param name="query">Query to order.</param>
    /// <param name="keySelector">Property selector.</param>
    /// <param name="ascending">True for ascending, false for descending.</param>
    /// <returns>Ordered query.</returns>
    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool ascending)
    {
        return ascending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }

    /// <summary>
    /// Conditionally applies a where clause.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="condition">Condition to check.</param>
    /// <param name="predicate">Predicate to apply if condition is true.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Conditionally applies a where clause for non-null values.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="value">Value to check for null.</param>
    /// <param name="predicate">Predicate factory.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereIfNotNull<T, TValue>(
        this IQueryable<T> query,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> predicate)
        where TValue : class
    {
        return value != null ? query.Where(predicate(value)) : query;
    }

    /// <summary>
    /// Conditionally applies a where clause for non-empty strings.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="value">String value to check.</param>
    /// <param name="predicate">Predicate factory.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereIfNotEmpty<T>(
        this IQueryable<T> query,
        string? value,
        Func<string, Expression<Func<T, bool>>> predicate)
    {
        return !string.IsNullOrWhiteSpace(value) ? query.Where(predicate(value)) : query;
    }

    /// <summary>
    /// Conditionally applies a where clause for nullable value types.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="value">Nullable value to check.</param>
    /// <param name="predicate">Predicate factory.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereIfHasValue<T, TValue>(
        this IQueryable<T> query,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> predicate)
        where TValue : struct
    {
        return value.HasValue ? query.Where(predicate(value.Value)) : query;
    }

    /// <summary>
    /// Filters active entities.
    /// </summary>
    /// <typeparam name="T">Entity type implementing IActivatable.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="activeOnly">If true, only active entities are returned.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query, bool activeOnly = true)
        where T : IActivatable
    {
        return activeOnly ? query.Where(e => e.IsActive) : query;
    }

    /// <summary>
    /// Includes navigation properties for efficient loading.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    /// <param name="query">Query to extend.</param>
    /// <param name="condition">Condition to check.</param>
    /// <param name="navigationPropertyPath">Navigation property path.</param>
    /// <returns>Query with includes.</returns>
    public static IQueryable<T> IncludeIf<T, TProperty>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, TProperty>> navigationPropertyPath)
        where T : class
    {
        return condition ? query.Include(navigationPropertyPath) : query;
    }

    /// <summary>
    /// Applies date range filter.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="dateProperty">Date property selector.</param>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereDateRange<T>(
        this IQueryable<T> query,
        Expression<Func<T, DateTime>> dateProperty,
        DateTime? from,
        DateTime? to)
    {
        if (from.HasValue)
        {
            var fromParam = Expression.Constant(from.Value);
            var greaterThanOrEqual = Expression.GreaterThanOrEqual(dateProperty.Body, fromParam);
            var fromLambda = Expression.Lambda<Func<T, bool>>(greaterThanOrEqual, dateProperty.Parameters);
            query = query.Where(fromLambda);
        }

        if (to.HasValue)
        {
            var toParam = Expression.Constant(to.Value);
            var lessThanOrEqual = Expression.LessThanOrEqual(dateProperty.Body, toParam);
            var toLambda = Expression.Lambda<Func<T, bool>>(lessThanOrEqual, dateProperty.Parameters);
            query = query.Where(toLambda);
        }

        return query;
    }

    /// <summary>
    /// Converts a query to a read-only query with no tracking.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to convert.</param>
    /// <returns>No-tracking query.</returns>
    public static IQueryable<T> AsReadOnly<T>(this IQueryable<T> query)
        where T : class
    {
        return query.AsNoTracking();
    }

    /// <summary>
    /// Gets count and items in a single operation.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to execute.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of total count and items.</returns>
    public static async Task<(int TotalCount, List<T> Items)> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Paginate(page, pageSize).ToListAsync(cancellationToken);
        return (totalCount, items);
    }

    /// <summary>
    /// Searches string properties using LIKE.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Query to filter.</param>
    /// <param name="searchTerm">Search term.</param>
    /// <param name="propertySelectors">Properties to search.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || propertySelectors.Length == 0)
            return query;

        var pattern = $"%{searchTerm}%";
        
        // Build OR expression for all properties
        Expression<Func<T, bool>>? combinedExpression = null;

        foreach (var selector in propertySelectors)
        {
            var parameter = selector.Parameters[0];
            var property = selector.Body;
            
            // Create: EF.Functions.Like(property, pattern)
            var likeMethod = typeof(DbFunctionsExtensions)
                .GetMethod(nameof(DbFunctionsExtensions.Like), 
                    new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);
            var patternConstant = Expression.Constant(pattern);
            var likeCall = Expression.Call(likeMethod!, efFunctions, property, patternConstant);

            var lambda = Expression.Lambda<Func<T, bool>>(likeCall, parameter);

            if (combinedExpression == null)
            {
                combinedExpression = lambda;
            }
            else
            {
                // Combine with OR
                var orExpression = Expression.OrElse(
                    combinedExpression.Body,
                    Expression.Invoke(lambda, combinedExpression.Parameters[0]));
                combinedExpression = Expression.Lambda<Func<T, bool>>(orExpression, combinedExpression.Parameters);
            }
        }

        return combinedExpression != null ? query.Where(combinedExpression) : query;
    }
}
