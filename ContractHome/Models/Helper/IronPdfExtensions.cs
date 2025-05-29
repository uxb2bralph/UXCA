using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using IronPdf.Editing;
using IronPdf.Signing;
using IronSoftware.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ContractHome.Models.Helper
{
    public static class IronPdfExtensions
    {
        static IronPdfExtensions()
        {
            IronPdf.License.LicenseKey = Settings.Default.IronPdfKey;
        }

        public static MemoryStream BuildContractWithSeal(this Contract contract)
        {
            using PdfDocument? pdf = BuildContractDocument(contract);
            if (pdf != null)
                return pdf.Stream;

            return new MemoryStream();
        }

        public static (int Width, int Height, string ImgUrl) GetContractImageData(this Contract contract, int pageIndex)
        {
            using PdfDocument? pdf = BuildContractDocument(contract, pageIndex);

            if (pdf == null)
            {
                return (0, 0, "");
            }

            using AnyBitmap bmp = pdf.PageToBitmap(0);
            using MemoryStream stream = new();

            bmp.ExportStream(stream, AnyBitmap.ImageFormat.Png);
            string imgBase64 = Convert.ToBase64String(stream.ToArray());
            string imgUrl = $"data:image/png;base64,{imgBase64}";
            return (bmp.Width, bmp.Height, imgUrl);
        }

        public static String? GetContractImage(this Contract contract, int pageIndex)
        {
            using PdfDocument? pdf = BuildContractDocument(contract);
            if (pdf != null)
            {
                using AnyBitmap bmp = pdf.PageToBitmap(pageIndex);
                String filePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{contract.ContractID:00000000}-{pageIndex:000}-{DateTime.Now.Ticks}.jpg");
                bmp.SaveAs(filePath, AnyBitmap.ImageFormat.Jpeg);
                return filePath;
            }

            return null;
        }

        public static PdfDocument? BuildContractDocument(this Contract contract, int pageIndex = -1)
        {
            PdfDocument? pdf = null;
            if (contract.FilePath != null && System.IO.File.Exists(contract.FilePath))
            {
                pdf = PdfDocument.FromFile(contract.FilePath/*, TrackChanges: true*/);
            }
            else if (contract.ContractContent != null)
            {
                pdf = new PdfDocument(contract.ContractContent.ToArray());
            }

            if (pdf == null)
            {
                return null;
            }

            PdfDocument outputDocument = pdf.CopyPage(pageIndex);

            using DCDataContext db = new();

            var sig = (from c in db.Contract
                       join s in db.ContractSignatureRequest on c.ContractID equals s.ContractID
                       where s.PageIndex == pageIndex && c.ContractID == contract.ContractID
                       select s).FirstOrDefault();

            if (sig != null && sig.SealImage != null)
            {
                byte[] buf = sig.SealImage.ToArray();
                ApplyStamp(outputDocument, buf, sig.MarginLeft, sig.MarginTop, sig.SealScale, 0);

            }

            var csr = (from c in db.Contract
                       join s in db.ContractSealRequest on c.ContractID equals s.ContractID
                       where s.PageIndex == pageIndex && c.ContractID == contract.ContractID
                       select s).FirstOrDefault();
            if (csr != null && csr.SealTemplate?.SealImage != null)
            {
                byte[] buf = csr.SealTemplate.SealImage.ToArray();
                ApplyStamp(outputDocument, buf, csr.MarginLeft, csr.MarginTop, csr.SealScale, 0);
            }

            var note = (from c in db.Contract
                        join n in db.ContractNoteRequest on c.ContractID equals n.ContractID
                        where n.PageIndex == pageIndex && c.ContractID == contract.ContractID
                        select n).FirstOrDefault();
            if (note != null)
            {
                ApplyStamp(outputDocument, note.Note, note.MarginLeft, note.MarginTop, 0, note.SealScale);
            }

            return outputDocument;
        }

        public static PdfDocument? BuildContractDocument(this Contract contract)
        {
            PdfDocument? pdf = null;
            if (contract.FilePath != null && System.IO.File.Exists(contract.FilePath))
            {
                pdf = PdfDocument.FromFile(contract.FilePath/*, TrackChanges: true*/);
            }
            else if (contract.ContractContent != null)
            {
                pdf = new PdfDocument(contract.ContractContent.ToArray());
            }

            if (pdf != null)
            {
                // Load Word document from file's path.
                foreach (var sig in contract.ContractSignatureRequest)
                {
                    if (sig.SealImage != null)
                    {
                        //String imgUrl = $"{Settings.Default.WebAppDomain}/ContractConsole/GetSignerSeal?ContractID={sig.ContractID}&CompanyID={sig.CompanyID}";
                        //using (AnyBitmap bmp = new AnyBitmap(sig.SealImage.ToArray()))
                        //{
                        //    ImageStamper imgStamper = new ImageStamper(bmp)
                        //    {
                        //        //Opacity = 60,
                        //        HorizontalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = sig.MarginLeft ?? 0, },
                        //        VerticalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = sig.MarginTop ?? 0 },
                        //        VerticalAlignment = IronPdf.Editing.VerticalAlignment.Top,
                        //        HorizontalAlignment = IronPdf.Editing.HorizontalAlignment.Left,
                        //        IsStampBehindContent = true,
                        //    };
                        //    pdf.ApplyStamp(imgStamper, sig.PageIndex ?? 0);
                        //}
                        byte[] buf = sig.SealImage.ToArray();
                        ApplyStamp(pdf, buf, sig.MarginLeft, sig.MarginTop, sig.SealScale, sig.PageIndex);

                    }
                }

                foreach (var sig in contract.ContractSealRequest)
                {
                    if (sig.SealTemplate?.SealImage != null)
                    {
                        byte[] buf = sig.SealTemplate.SealImage.ToArray();
                        ApplyStamp(pdf, buf, sig.MarginLeft, sig.MarginTop, sig.SealScale, sig.PageIndex);
                    }
                }

                foreach (var note in contract.ContractNoteRequest)
                {
                    ApplyStamp(pdf, note.Note, note.MarginLeft, note.MarginTop, note.PageIndex, note.SealScale);
                }

                //pdf.SaveAs(Path.Combine(FileLogger.Logger.LogDailyPath, $"DBG-{contract.ContractID:00000000}.pdf"));
                return pdf;
            }

            return null;
        }

        public static void ApplyStamp(PdfDocument pdf, byte[] buf, double? marginLeft, double? marginTop, double? sealScale, int? pageIndex)
        {
            using (MemoryStream stream = new MemoryStream(buf))
            {
                using (Bitmap bmp = new Bitmap(stream))
                {
                    var backgroundStamp = new HtmlStamper()
                    {
                        Html = $"<img style='z-index:10; mix-blend-mode:multiply;width:{bmp.Width * (sealScale ?? 1)}px;' src='data:application/octet-stream;base64,{Convert.ToBase64String(buf)}'/>",
                        Opacity = 60,
                        HorizontalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = marginLeft ?? 0, },
                        VerticalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = marginTop ?? 0 },
                        VerticalAlignment = IronPdf.Editing.VerticalAlignment.Top,
                        HorizontalAlignment = IronPdf.Editing.HorizontalAlignment.Left,
                        IsStampBehindContent = false,
                    };
                    pdf.ApplyStamp(backgroundStamp, pageIndex ?? 0);
                }
            }
        }

        private static void ApplyStamp(PdfDocument pdf, String text, double? marginLeft, double? marginTop, int? pageIndex, double? width)
        {
            var backgroundStamp = new HtmlStamper()
            {
                Html = $"<div>{text}</div>",
                //Opacity = 60,
                HorizontalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = marginLeft ?? 0, },
                VerticalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = marginTop ?? 0 },
                VerticalAlignment = IronPdf.Editing.VerticalAlignment.Top,
                HorizontalAlignment = IronPdf.Editing.HorizontalAlignment.Left,
                IsStampBehindContent = false,
                MaxWidth = new Length(unit: MeasurementUnit.Centimeter) { Value = width ?? 0, }
            };
            pdf.ApplyStamp(backgroundStamp, pageIndex ?? 0);
        }

        public static bool SignPdfByLocalUser(this GenericManager<DCDataContext> models, ContractSignatureRequest request, UserProfile signer)
        {
            if (request.SealImage == null)
            {
                return false;
            }

            if (request.Organization.OrganizationToken?.PKCS12 == null)
            {
                return false;
            }

            var contract = request.Contract;

            String tmpPath = Path.GetDirectoryName(contract.FilePath)!;
            String signedPdf = Path.Combine(tmpPath, $"{Guid.NewGuid()}.pdf");
            //if(File.Exists(signedPdf))
            //{
            //    File.Delete(signedPdf);
            //}
            File.Copy(contract.FilePath, signedPdf);

            IronPdf.License.LicenseKey = Settings.Default.IronPdfKey;
            PdfDocument pdf = PdfDocument.FromFile(contract.FilePath/*, TrackChanges: true*/);

            using (MemoryStream stream = new MemoryStream(request.SealImage.ToArray()))
            {
                using (Bitmap seal = new Bitmap(stream))
                {
                    // Create PdfSignature object
                    var sig = new PdfSignature("C:\\Project\\Github\\UXCA\\Doc\\70762419.pfx", "70762419")
                    {
                        // Add granular information
                        SignatureDate = DateTime.Now,
                        SigningContact = contract.ContractNo,
                        SigningLocation = "Taipei",
                        SigningReason = "Deal a contract",

                        //sig.TimeStampUrl = "https://www.freetsa.org/";
                        SignatureImage = new PdfSignatureImage(stream.ToArray(), 1, new CropRectangle((int)(0), (int)(0), (int)(seal.Width * 25.4 / seal.HorizontalResolution + 0.5), (int)(seal.Height * 25.4 / seal.VerticalResolution + 0.5), MeasurementUnits.Millimeters))
                        {
                            PageIndex = 1,
                        }
                    };
                    // Sign and save PDF document
                    pdf.Sign(sig);
                    sig = new PdfSignature("C:\\Project\\Github\\UXCA\\Doc\\70762419.pfx", "70762419")
                    {
                        // Add granular information
                        SignatureDate = DateTime.Now,
                        SigningContact = contract.ContractNo,
                        SigningLocation = "Taipei",
                        SigningReason = "Deal a contract",

                        //sig.TimeStampUrl = "https://www.freetsa.org/";
                        SignatureImage = new PdfSignatureImage(stream.ToArray(), 2, new CropRectangle((int)(100), (int)(0), (int)(seal.Width * 25.4 / seal.HorizontalResolution + 0.5), (int)(seal.Height * 25.4 / seal.VerticalResolution + 0.5), MeasurementUnits.Millimeters))
                        {
                            PageIndex = 2,
                        }
                    };
                    pdf = pdf.SaveAsRevision();
                    pdf.Sign(sig);
                    pdf.SaveAs(signedPdf);
                    request.ResponsePath = signedPdf;
                    //sig.SignPdfFile(signedPdf);
                }
            }

            return true;
        }

        public static int GetPdfPageCount(this Contract contract)
        {
            if (contract == null)
            {
                return 0;
            }

            PdfDocument? pdf = null;

            if (File.Exists(contract.FilePath))
            {
                pdf = PdfDocument.FromFile(contract.FilePath);
            }
            else if (contract.ContractContent != null)
            {
                pdf = new PdfDocument(contract.ContractContent.ToArray());
            }

            if (pdf == null)
            {
                return 0;
            }

            var pageCount = pdf.Pages.Count;
            pdf.Dispose();

            return pageCount;
        }


    }
}
