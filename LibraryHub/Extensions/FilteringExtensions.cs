
using System.Linq.Expressions;

namespace LibraryHub.Extensions;

// This code creates a custom Extension Method called .WhereSearch().
// It lets you dynamically search any table (Book, User, Product) across
// any text fields you want, building the SQL WHERE ... LIKE ... query at runtime dynamically!
public static class FilterExtensions
{
    public static IQueryable<T> WhereSearch<T>(
        this IQueryable<T> query,  // You can now attach .WhereSearch(...) directly onto any IQueryable variable (like _context.Books)
        string? search,
        params Expression<Func<T, string?>>[] selectors)  // this passes the structure of a lambda expression selecting a string property from class T
    {
        if (string.IsNullOrWhiteSpace(search) || selectors.Length == 0)
            return query;                               // it returns the original query untouched.

        var parameter = Expression.Parameter(typeof(T), "x"); // Creates a single unified parameter placeholder representing an item of type T
        Expression? combined = null;                          // exm - (Book x) like representing Book as x

        foreach (var selector in selectors)  // Loops through each property selector provided by the user (e.g., first x => x.Title, second x => x.Author)
        {
            var property = new ReplaceParameterVisitor( // Uses ReplaceParameterVisitor to rewrite the internal body of the expression so 
                selector.Parameters[0], parameter)      // that it points to our single shared parameter (x). b.Title becomes x.Title
                .Visit(selector.Body)!;

            var notNull = Expression.NotEqual(  // Prevents NullReferenceException errors when executing database queries.
                property,
                Expression.Constant(null, typeof(string)));

            var contains = Expression.Call(
                property,                     // The target object (x.Title).
                typeof(string).GetMethod(     // Uses reflection to locate string.Contains(string)
                    nameof(string.Contains),
                    new[] { typeof(string) })!,
                Expression.Constant(search));   // The argument passed into .Contains(...) (e.g., "Harry").

            var condition = Expression.AndAlso(notNull, contains);  // Performs a short-circuiting conditional && operation. ((x.Title != null && x.Title.Contains("Harry")))

            combined = combined == null   // (x.Title != null && x.Title.Contains("Harry")) || (x.Author != null && x.Author.Contains("Harry"))
                ? condition
                : Expression.OrElse(combined, condition);
        }

        var predicate = Expression.Lambda<Func<T, bool>>(  // Wraps our raw boolean condition and parameter into a full executable Lambda Expression
            combined!, parameter);

        return query.Where(predicate);
    }

    // Inherits from ExpressionVisitor, a built-in .NET class designed to walk through an Expression Tree object graph and modify nodes.
    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        // stores _oldParameter (e.g., b in b => b.Title) and _newParameter (our single x)

        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == _oldParameter
                ? _newParameter
                : base.VisitParameter(node);
        }
    }
}

//ExpressionVisitor = is a built-in .NET class that walks through an Expression Tree object graph
//node-by-node (like visiting every leaf on a tree). By inheriting from ExpressionVisitor,
//we can intercept specific tree nodes, alter them, and rebuild the tree on the fly.
