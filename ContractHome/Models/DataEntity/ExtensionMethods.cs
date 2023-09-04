using ContractHome.Models.ViewModel;
using System.Data.Linq.Mapping;
using System.Linq.Dynamic.Core;
using System.Reflection;
using CommonLib.Utility;
using System.Data.Linq;

namespace ContractHome.Models.DataEntity
{
    public static class ExtensionMethods
    {
        public static ColumnAttribute? GetColumnAttribute(this PropertyInfo propertyInfo)
        {
            if (Attribute.IsDefined(propertyInfo, typeof(ColumnAttribute)))
            {
                return (ColumnAttribute?)Attribute.GetCustomAttribute(propertyInfo, typeof(ColumnAttribute));
            }

            return null;
        }
        public static bool CheckPrimaryKey(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetColumnAttribute()?.IsPrimaryKey == true;

            //if (propertyInfo.CustomAttributes?.Any() == true)
            //{
            //    if (propertyInfo.CustomAttributes
            //        .Any(p => p.NamedArguments
            //            .Any(a => a.MemberName == "IsPrimaryKey" && a.TypedValue.Value.Equals(true))))
            //    {
            //        return true;
            //    }
            //}
            //return false;
        }

        public static IQueryable<TEntity> BuildQuery<TEntity>(this IQueryable<TEntity> items,DataTableColumn[] fields)
            where TEntity : class, new()
        {
            Type type = typeof(TEntity);
            foreach (DataTableColumn field in fields)
            {
                PropertyInfo propertyInfo = type.GetProperty(field.Name);
                var columnAttribute = propertyInfo?.GetColumnAttribute();
                if (columnAttribute != null)
                {
                    String fieldValue = field.Value.GetEfficientString();
                    if (fieldValue == null)
                    {
                        continue;
                    }
                    var t = propertyInfo.PropertyType;
                    if (t == typeof(String))
                    {
                        items = items.Where($"{propertyInfo.Name}.StartsWith(@0)", fieldValue);
                    }
                    else
                    {
                        items = items.Where($"{propertyInfo.Name} == @0", Convert.ChangeType(fieldValue, propertyInfo.PropertyType));
                    }
                }
            }

            return items;
        }

        public static DataItemValue[]? GetPrimaryKeyValues <TEntity>(this TEntity? item)
            where TEntity : class, new()
        {
            Type type = typeof(TEntity); // _model.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (item != null)
            {
                var pk = properties.Where(p => p.CheckPrimaryKey());
                if (pk.Any())
                {
                    var keyItem = pk.Select(p => new DataItemValue
                    {
                        Name = p.Name,
                        Value = p.GetValue(item, null)
                    }).ToArray();
                    return keyItem;
                }
            }
            return null;
        }
        public static dynamic? PrepareDataItem(this DataTableColumn[]? dataColumn, dynamic? dataItem, ITable dataTable, Type type)
        {
            if (dataItem == null)
            {
                dataItem = Activator.CreateInstance(type);
                dataTable.InsertOnSubmit(dataItem);
            }

            if (dataColumn != null)
            {
                foreach (DataTableColumn field in dataColumn)
                {
                    PropertyInfo? propertyInfo = type.GetProperty(field.Name);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        var colValue = field.Value.GetEfficientString();
                        object? value = colValue != null
                                        ? Convert.ChangeType(field.Value, propertyInfo.PropertyType)
                                        : null;
                        propertyInfo.SetValue(dataItem, value, null);
                    }
                }
            }

            return dataItem;
        }
    }
}
