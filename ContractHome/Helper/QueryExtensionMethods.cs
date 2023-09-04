using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;
using System.Data;
using System.Data.SqlClient;

namespace ContractHome.Helper
{
    public static class QueryExtensionMethods
    {


        public static DataSet GetDataSetResult(this GenericManager<DCDataContext> models, IQueryable items)

        {
            using (SqlCommand sqlCmd = (SqlCommand)models.GetCommand(items))
            {
                //sqlCmd.Connection = (SqlConnection)models.DataContext.Database.GetDbConnection();
                sqlCmd.Connection = (SqlConnection)models.DataContext.Connection;
                using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCmd))
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }
            }
        }

        public static DataSet? GetDataSetResult(this GenericManager<DCDataContext> models, IQueryable items, DataTable table)

        {
            using (SqlCommand sqlCmd = (SqlCommand)models.GetCommand(items))
            {
                //sqlCmd.Connection = (SqlConnection)models.DataContext.Database.GetDbConnection();
                sqlCmd.Connection = (SqlConnection)models.DataContext.Connection;
                using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCmd))
                {
                    int colCount = table.Columns.Count;
                    adapter.Fill(table);
                    if (colCount > 0)
                    {
                        while (table.Columns.Count > colCount)
                        {
                            table.Columns.RemoveAt(table.Columns.Count - 1);
                        }
                    }
                    return table.DataSet;
                }
            }
        }

        public static ClosedXML.Excel.XLWorkbook GetExcelResult(this GenericManager<DCDataContext> models, IQueryable items, String tableName = null)

        {
            using (DataSet ds = models.GetDataSetResult(items))
            {
                if (tableName != null)
                    ds.Tables[0].TableName = ds.DataSetName = tableName;
                return ConvertToExcel(ds);
            }
        }

        public static ClosedXML.Excel.XLWorkbook ConvertToExcel(this DataSet ds)
        {
            ClosedXML.Excel.XLWorkbook xls = new ClosedXML.Excel.XLWorkbook();
            try
            {
                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    ds.Tables[i].TableName = $"_{ds.Tables[i].TableName}";
                }
                xls.Worksheets.Add(ds);
            }
            catch (Exception ex)
            {
                ApplicationLogging.CreateLogger("QueryExtensionMethods").LogError(ex, ex.Message);
            }
            return xls;
        }


    }

}
