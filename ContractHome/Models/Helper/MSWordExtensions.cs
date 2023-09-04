using CommonLib.DataAccess;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ContractHome.Models.DataEntity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Office.Interop.Word;
using System.Drawing;
using ContractHome.Properties;
using System.Linq.Dynamic.Core;

namespace ContractHome.Models.Helper
{
    public static class MSWordExtensions
    {
        public static String? BuildCurrentContractByWord(this Contract contract, GenericManager<DCDataContext> models)
        {
            if (contract.FilePath != null && System.IO.File.Exists(contract.FilePath))
            {
                // 建立一個 Word 應用程式
                Application wordApp = new Application();

                // 關閉 Word 應用程式時也關閉文件
                wordApp.DisplayAlerts = WdAlertLevel.wdAlertsNone;

                // 開啟一個 Word 文件
                Document document = wordApp.Documents.Open(contract.FilePath);

                // 尋找所有圖形並替換
                var s = document.Shapes;
                var shapes = document.InlineShapes.ToDynamicArray();
                foreach (InlineShape shape in shapes)
                {
                   // 判斷圖形大小是否符合要求
                    if (shape.Width == 180 && shape.Height == 150)
                    {
                        // 替換圖形
                        shape.Select();
                        wordApp.Selection.InlineShapes.AddPicture(@"C:\Path\To\Your\demo.jpg");
                    }
                }

                foreach (Shape shape in s)
                {
                    // 判斷圖形大小是否符合要求
                    if (shape.Width == 180 && shape.Height == 150)
                    {
                        // 替換圖形
                        shape.Select();
                        wordApp.Selection.InlineShapes.AddPicture(@"C:\Path\To\Your\demo.jpg");
                    }
                }

                // 將文件另存為 PDF 格式
                String filePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{Guid.NewGuid()}.pdf");
                document.SaveAs2(filePath, WdExportFormat.wdExportFormatPDF);

                // 關閉文件和應用程式
                document.Close();
                wordApp.Quit();

                //var elements = document.GetChildElements(true, ElementType.Picture).ToList();
                //if (elements?.Count > 0)
                //{
                //    foreach (var element in elements)
                //    {
                //        var picture = (Picture)element;
                //        var template = picture.TryToMatchTemplate(models);
                //        if (template != null)
                //        {
                //            var stamp = contract.ContractSealRequest.Where(r => r.SealID == template.SealID)
                //                            .FirstOrDefault();
                //            if (stamp?.StampDate.HasValue == true && stamp?.SealImage != null)
                //            {
                //                picture.BuildSeal(stamp.SealImage.ToArray());
                //            }
                //        }
                //    }
                //}

                //document.Save(output, SaveOptions.PdfDefault);
                return filePath;
            }

            return null;
        }

    }
}
