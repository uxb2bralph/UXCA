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
using GemBox.Document;
using System.Drawing;
using ContractHome.Properties;
using Newtonsoft.Json.Linq;

namespace ContractHome.Models.Helper
{
    public static class GemboxExtensions
    {
        public static SealTemplate? TryToMatchTemplate(this Picture picture, GenericManager<DCDataContext> models)
        {
            using (System.Drawing.Image bmp = Bitmap.FromStream(picture.PictureStream))
            {
                var template = models.GetTable<SealTemplate>()
                        .Where(t => t.Width == bmp.Width)
                        .Where(t => t.Height == bmp.Height)
                        .FirstOrDefault();
                return template;
            }
        }

        static byte[] GetImage(String url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadData(url);
            }
        }

        public static void BuildSeal(this Picture picture, String seal)
        {
            if (seal != null)
            {
                byte[]? data = null;
                if (seal.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    data = GetImage(seal);
                }
                else if (seal.StartsWith("data", StringComparison.InvariantCultureIgnoreCase))
                {
                    data = Convert.FromBase64String(seal.Substring(seal.IndexOf(',') + 1));
                }

                if (data != null)
                {
                    BuildSeal(picture, data);
                }
            }
        }

        public static void BuildSeal(this Picture picture, byte[] data,double? scale = null)
        {
            MemoryStream stream = new MemoryStream(data);
            picture.PictureStream = stream;
            using (System.Drawing.Image img = System.Drawing.Image.FromStream(stream))
            {
                if(scale.HasValue)
                {
                    picture.Layout.Size = new GemBox.Document.Size(img.Width / img.HorizontalResolution * scale.Value / 100D, img.Height / img.VerticalResolution * scale.Value / 100D, LengthUnit.Inch);
                }
                else
                {
                    picture.Layout.Size = new GemBox.Document.Size(img.Width / (Settings.Default.SealImageDPI ?? img.HorizontalResolution), img.Height / (Settings.Default.SealImageDPI ?? img.VerticalResolution), LengthUnit.Inch);
                }
            }
        }

        public static MemoryStream BuildCurrentContract(this Contract contract, GenericManager<DCDataContext> models, bool preview = false)
        {
            MemoryStream output = new MemoryStream();

            if(contract.ContractSignature != null)
            {
                ContractSignatureRequest request = contract.ContractSignature.ContractSignatureRequest;
                if(request.ResponsePath != null && File.Exists(request.ResponsePath))
                {
                    JObject content = JObject.Parse(File.ReadAllText(request.ResponsePath));
                    if ((String)content["code"] == "0")
                    {
                        output.Write(Convert.FromBase64String((String)content["msg"]));
                        return output;
                    }
                }
            }

            if (contract.FilePath != null && System.IO.File.Exists(contract.FilePath))
            {
                ComponentInfo.SetLicense(Settings.Default.GemboxKey);

                // Load Word document from file's path.
                var document = DocumentModel.Load(contract.FilePath);
                var elements = document.GetChildElements(true, ElementType.Picture).ToList();
                if (elements?.Count > 0)
                {
                    foreach (var element in elements)
                    {
                        var picture = (Picture)element;
                        var template = picture.TryToMatchTemplate(models);
                        if (template != null)
                        {
                            var stamp = contract.ContractSealRequest.Where(r => r.SealID == template.SealID)
                                            .FirstOrDefault();
                            if ((stamp?.StampDate.HasValue == true || preview) && stamp?.SealImage != null)
                            {
                                picture.BuildSeal(stamp.SealImage.ToArray(),stamp.SealScale ?? 100D);
                            }
                        }
                    }
                }

                foreach (var p in document.GetChildElements(true, ElementType.Paragraph))
                {
                    ((Paragraph)p).ParagraphFormat.LineSpacing = Settings.Default.LineSpacing;
                }

                document.Save(output, SaveOptions.PdfDefault);
            }
            return output;
        }

        public static String? ConvertDocxToPdf(this String docxFile)
        {
            if (docxFile != null && System.IO.File.Exists(docxFile))
            {
                String filePath = Path.Combine(Contract.ContractStore, $"{Guid.NewGuid()}.pdf");
                ComponentInfo.SetLicense(Settings.Default.GemboxKey);

                // Load Word document from file's path.
                var document = DocumentModel.Load(docxFile);
                foreach (var p in document.GetChildElements(true, ElementType.Paragraph))
                {
                    ((Paragraph)p).ParagraphFormat.LineSpacing = Settings.Default.LineSpacing;
                }

                document.Save(filePath);
                return filePath;
            }

            return null;
        }

        public static String GetCurrentContractPdfBase64(this Contract contract, GenericManager<DCDataContext> models, bool preview = false)
        {
            if (contract.ContractSignature != null)
            {
                ContractSignatureRequest request = contract.ContractSignature.ContractSignatureRequest;
                if (request.ResponsePath != null && File.Exists(request.ResponsePath))
                {
                    JObject content = JObject.Parse(File.ReadAllText(request.ResponsePath));
                    if ((String)content["code"] == "0")
                    {
                        return (String)content["msg"];
                    }
                }
            }

            MemoryStream output = new MemoryStream();
            if (contract.FilePath != null && System.IO.File.Exists(contract.FilePath))
            {
                ComponentInfo.SetLicense(Settings.Default.GemboxKey);

                // Load Word document from file's path.
                var document = DocumentModel.Load(contract.FilePath);
                var elements = document.GetChildElements(true, ElementType.Picture).ToList();
                if (elements?.Count > 0)
                {
                    foreach (var element in elements)
                    {
                        var picture = (Picture)element;
                        var template = picture.TryToMatchTemplate(models);
                        if (template != null)
                        {
                            var stamp = contract.ContractSealRequest.Where(r => r.SealID == template.SealID)
                                            .FirstOrDefault();
                            if ((stamp?.StampDate.HasValue == true || preview) && stamp?.SealImage != null)
                            {
                                picture.BuildSeal(stamp.SealImage.ToArray(), stamp.SealScale ?? 100D);
                            }
                        }
                    }
                }

                foreach (var p in document.GetChildElements(true, ElementType.Paragraph))
                {
                    ((Paragraph)p).ParagraphFormat.LineSpacing = Settings.Default.LineSpacing;
                }

                document.Save(output, SaveOptions.PdfDefault);
            }

            return Convert.ToBase64String(output.ToArray());
        }

    }
}
