using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Data;

namespace CommonLib.Utility
{
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }

        // 根據查詢內容是否有% 符號來決定Like的方式 , ex : 如果使用者輸入 gg% => DB的where條件要是[欄位] like N'gg%'        
        public static IQueryable<T> Like<T>(this IQueryable<T> query, Expression<Func<T, string>> lambda, string param)
        {
            // 解析Lambda的內容
            var body = lambda.Body as MemberExpression;
            if (body == null) { return query; }

            // 產生參數
            ParameterExpression paramSource = Expression.Parameter(query.ElementType, "m");
            Expression columnExp = Expression.Property(paramSource, body.Member.Name);

            if (string.IsNullOrEmpty(param)) { return query; }

            string queryString = param.Replace("%", "");
            Expression paramQuery = Expression.Constant(queryString, typeof(string));
            MethodCallExpression method;

            if (param.StartsWith("%"))
            {
                //所產生的Lambda :  d => d.欄位.EndsWith(paramString)
                method = Expression.Call(columnExp, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), paramQuery);
            }
            else if (param.EndsWith("%"))
            {
                //所產生的Lambda :  d => d.欄位.StartsWith(paramString)
                method = Expression.Call(columnExp, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), paramQuery);
            }
            else
            {
                //所產生的Lambda :  d => d.欄位.Contains(paramString)
                method = Expression.Call(columnExp, typeof(string).GetMethod("Contains", new[] { typeof(string) }), paramQuery);
            }

            return query.Where(Expression.Lambda<Func<T, bool>>(method, paramSource));
        }

        public static IQueryable<T> Between<T>(this IQueryable<T> query,
            string propertyName,
            object leftValue,
            object rightValue
        )
        {
            var param = Expression.Parameter(typeof(T), "p");
            var property = Expression.Property(param, propertyName);
            var leftConvert = Expression.Convert(Expression.Constant(leftValue), property.Type);
            var rightConvert = Expression.Convert(Expression.Constant(rightValue), property.Type);
            var body = Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, leftConvert),
                Expression.LessThanOrEqual(property, rightConvert)
            );
            var queryLambda = Expression.Lambda<Func<T, bool>>(body, param);
            return query.Where(queryLambda);
        }

        // 依據mapping表對應物件property並給值比對
        public static IQueryable<T> EqualMultiple<T>(this IQueryable<T> query, IDictionary<string, object> filters)
        {
            var param = Expression.Parameter(typeof(T), "p");
            Expression body = null;
            foreach (var pair in filters)
            {
                var property = Expression.Property(param, pair.Key);
                var constant = Expression.Constant(pair.Value);
                var converted = Expression.Convert(constant, property.Type);
                var expression = Expression.Equal(property, converted);
                body = body == null ? expression : Expression.AndAlso(body, expression);
            }
            var queryLambda = Expression.Lambda<Func<T, bool>>(body, param);
            return query.Where(queryLambda);
        }

        // 依據多欄位排序的集合呼叫對應的排序方法
        public static IQueryable<T> OrderByMultiple<T>(this IQueryable<T> query, List<OrderByCol> cols)
        {
            foreach (var col in cols)
            {
                ParameterExpression paramSource = Expression.Parameter(query.ElementType, "m");
                Expression columnExp = Expression.Property(paramSource, col.colName);

                // 決定要執行的排序Method
                string methodName =
                            $@"{(cols.IndexOf(col) == 0 ? "Order" : "Then")}By{(col.sortType == OrderbyType.Desc ? "Descending" : "")}";

                // 產生 Call Method 的節點
                var method = Expression.Call(
                            typeof(Queryable),
                            methodName,
                            new Type[] { query.ElementType, columnExp.Type },
                            query.Expression,
                            Expression.Quote(Expression.Lambda(columnExp, paramSource)));

                query = query.Provider.CreateQuery<T>(method);
            }

            return query;
        }

        public enum OrderbyType
        {
            Asc,
            Desc
        }

        public class OrderByCol
        {
            public OrderbyType sortType { get; set; }
            public string colName { get; set; }
        }
    }
}