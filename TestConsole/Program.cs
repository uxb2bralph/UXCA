using GemBox.Document;
using GemBox.Pdf;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using CommonLib.Utility;
using Newtonsoft.Json.Linq;
using GrapeCity.Documents.Pdf.TextMap;
using GrapeCity.Documents.Pdf;
using GdPicture14;
using IronPdf.Editing;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Helper;
using ContractHome.Properties;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Test01();

            //Test02();
            // 讀取指定的pem格式私鑰文檔
            //System.Diagnostics.Debugger.Launch();
            //Test03(args);

            //System.Diagnostics.Debugger.Launch();
            //if (args.Length < 4)
            //{
            //    return;
            //}

            //ComponentInfo.SetLicense("DN-2022Dec09-Xo7dUjpWkKP0gwWamS5eh0EDbVNnFIZOvASNOEz4pQfffzmEDa7NFRsu254C7Vx6gAyGxtVxVBgY9JPADZPTW6OUVKQ==A");

            //var document = DocumentModel.Load(args[0]);

            //// Create visual representation of digital signature.
            //var signature = new Picture(document, args[1]);

            //// Position signature image at the end of the document.
            //var lastSection = document.Sections[document.Sections.Count - 1];
            //lastSection.Blocks.Add(new Paragraph(document, signature));

            //var options = new PdfSaveOptions()
            //{
            //    DigitalSignature =
            //{
            //    CertificatePath = args[2],
            //    CertificatePassword = args[3],
            //    Signature = signature,
            //    IsAdvancedElectronicSignature = true
            //}
            //};

            //document.Save("PDF Digital Signature.pdf", options);

            //ComponentInfo.SetLicense("DN-2022Dec09-Xo7dUjpWkKP0gwWamS5eh0EDbVNnFIZOvASNOEz4pQfffzmEDa7NFRsu254C7Vx6gAyGxtVxVBgY9JPADZPTW6OUVKQ==A");
            //var document = DocumentModel.Load("G:\\temp\\Sample_20230310v1.pdf");
            ////Test02_1(document);
            //document.Save("G:\\temp\\test.pdf");
            //using (FileStream fs = new FileStream("G:\\temp\\Sample_20230310v1.pdf", FileMode.Open,
            //FileAccess.Read, FileShare.Read))
            //{
            //    GcPdfDocument doc = new GcPdfDocument();
            //    doc.Load(fs);
            //    var images = doc.GetImages();
            //    //doc.Pages[0].ContentStreams.Add
            //    var image = images[0];

            //    doc.Save("G:\\temp\\test.pdf");
            //}


            //LicenseManager oLicenseManager = new LicenseManager(); //Go to http://www.gdpicture.com/download-gdpicture/ to get a 1 month trial key unlocking all features of the toolkit.
            //oLicenseManager.RegisterKEY(" 0451510733397095748521972"); //Please, replace XXXX by a valid demo or commercial license key.

            //GdPicturePDF doc = new GdPicturePDF();
            //doc.LoadFromFile("G:\\temp\\Sample_20230310v1.pdf");
            //var count = doc.GetPageImageCount();
            //var imgName = doc.GetPageImageResName(1);

            //doc.SaveToFile("G:\\temp\\test.pdf");

            IronPdf.License.LicenseKey = Settings.Default.IronPdfKey;
            IronPdf.PdfDocument pdf = IronPdf.PdfDocument.FromFile("G:\\temp\\Sample_20230310v1.pdf");
            var backgroundStamp = new HtmlStamper("<div style=\"background-image: url('file://G:/temp/test.jpg');width: 285px;height: 175px;z-index: -1;\"></div>")
            {
                Opacity = 50,
                HorizontalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = 10, },
                VerticalOffset = new Length(unit: MeasurementUnit.Centimeter) { Value = 5 },
                VerticalAlignment = IronPdf.Editing.VerticalAlignment.Top,
                HorizontalAlignment = IronPdf.Editing.HorizontalAlignment.Left,
                IsStampBehindContent = true,
            };
            pdf.ApplyStamp(backgroundStamp, 0);
            pdf.Flatten();
            pdf.SaveAs("G:\\temp\\test.pdf");

            var bmp = pdf.PageToBitmap(0);
            bmp.SaveAs("G:\\temp\\data.jpg", IronSoftware.Drawing.AnyBitmap.ImageFormat.Jpeg);

            //Test04();

            //using (ModelSource models = new ModelSource())
            //{
            //    var items = models.GetTable<UserRole>().ToList();
            //    File.WriteAllText("G:\\temp\\data.json", items[0].JsonStringify());
            //}

            //using (ModelSource models = new ModelSource())
            //{
            //    UserProfile profile = models.GetTable<UserProfile>().Where(u => u.UID == 4).First();
            //    ContractSignatureRequest request = models.GetTable<ContractSignatureRequest>()
            //            .Where(r => r.CompanyID == 1).Where(r => r.ContractID == 6).First();

            //    models.SignPdfByLocalUser(request, profile);
            //}

            //WpfTest.MainWindow mainWindow = new WpfTest.MainWindow();
            //mainWindow.Show();
            //Console.ReadKey();
        }

        private static void Test04()
        {
            Task.Delay(1000).ContinueWith(ts =>
            {
                Console.WriteLine(DateTime.Now);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            });

            Console.ReadKey();
        }

        private static void Test03(string[] args)
        {
            if (args.Length < 3)
            {
                return;
            }

            SHA256 hash;

            int signerIdx = -1;
            if (!int.TryParse(args[0], out signerIdx) || signerIdx < 0 || signerIdx > 1)
            {
                return;
            }

            String pdfPath = args[1];
            if (pdfPath == null || !File.Exists(pdfPath))
            {
                return;
            }

            String keyContent = File.ReadAllText("keys.json");
            JArray keys = JArray.Parse(keyContent);
            JToken key = keys[signerIdx];

            string privateKeyFilePath = (String)key["KeyPath"]!;
            string privateKey = File.ReadAllText(privateKeyFilePath);

            // 創建RSA加密服務提供者
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportFromPem(privateKey);
            //// 從私鑰文檔中取得私鑰參數
            //RSAParameters privateKeyParams = ConvertPemToRSAParameters(privateKey);

            //// 將私鑰參數設置到RSA加密服務提供者中
            //rsa.ImportParameters(privateKeyParams);

            // 輸入要加密的字串
            string inputString = $"{key["Token"]}{DateTime.Now:yyyyMMddHHmmss}";
            hash = SHA256.Create();
            byte[] inputBytes = hash.ComputeHash(Encoding.Default.GetBytes(inputString));
            // 將輸入字串轉換為byte陣列

            // 進行RSA加密運算
            byte[] encryptedBytes = rsa.SignData(inputBytes, hash); //rsa.Encrypt(inputBytes, false);

            // 將加密的結果以HEX方式呈現
            string encryptedHex = BitConverter.ToString(encryptedBytes).Replace("-", "");

            //Console.WriteLine("Input String: {0}", inputString);
            //Console.WriteLine("Encrypted Hex: {0}", encryptedHex);
            encryptedBytes = rsa.SignHash(inputBytes, "SHA256");
            var apSignature = encryptedBytes.ToHexString(format: "x2");
            var data = new
            {
                clusterid = key["ClusterID"],
                b64pdf = Convert.ToBase64String(File.ReadAllBytes(args[1])),
                uid = $"uxb2b-{DateTime.Now.Ticks}",
                pdfpw = "",
                //ownerpw = "",
                //userpw = "",
                thirdpartyclusterid = "",
                email = key["Email"],
                apOneTimeToken = inputString,
                apSignature = apSignature
            };
            String dataToSign = data.JsonStringify();
            File.WriteAllText("G:\\temp\\test.json", dataToSign);

            String url = "https://eguitest.uxifs.com/CryptoKeyManageCVS2API/3partypdfsign/GRA_TPSSC_IV_CVS2_AP.do";
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                String result = client.UploadString(url, dataToSign);

                JObject content = JObject.Parse(result);
                if ((String)content["code"] == "0")
                {
                    File.WriteAllBytes(args[2], Convert.FromBase64String((String)content["msg"]));
                }
                else
                {
                    Console.WriteLine("error response...");
                    Console.WriteLine(result);
                }
            }
        }

        //static RSAParameters ConvertPemToRSAParameters(string pem)
        //{
        //    // 從pem格式的私鑰文檔中取得私鑰參數
        //    byte[] pemBytes = Encoding.ASCII.GetBytes(pem);
        //    byte[] pkcs8Bytes = Convert.FromBase64String(Encoding.ASCII.GetString(pemBytes).Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", ""));
        //    AsnEncodedData privateKeyData = new AsnEncodedData(pkcs8Bytes);
        //    RSAParameters rsaParams = new RSAParameters();
        //    rsaParams.Modulus = privateKeyData.Modulus;
        //    rsaParams.Exponent = privateKeyData.Exponent;
        //    rsaParams.D = privateKeyData.ModulusInv;
        //    rsaParams.P = privateKeyData.Factors[0];
        //    rsaParams.Q = privateKeyData.Factors[1];
        //    rsaParams.DP = privateKeyData.Exponent1;
        //    rsaParams.DQ = privateKeyData.Exponent2;
        //    rsaParams.InverseQ = privateKeyData.Coefficient;
        //    return rsaParams;
        //}

        private static void Test02()
        {
            // If using the Professional version, put your serial key below.
            GemBox.Document.ComponentInfo.SetLicense("DN-2022Dec09-Xo7dUjpWkKP0gwWamS5eh0EDbVNnFIZOvASNOEz4pQfffzmEDa7NFRsu254C7Vx6gAyGxtVxVBgY9JPADZPTW6OUVKQ==A");

            // Load Word document from file's path.
            var document = DocumentModel.Load("G:\\temp\\Sample.docx");
            Test02_1(document);
        }

        private static void Test02_1(DocumentModel document)
        {
            // Get Word document's plain text.
            string text = document.Content.ToString();

            // Get Word document's count statistics.
            int charactersCount = text.Replace(Environment.NewLine, string.Empty).Length;
            int wordsCount = document.Content.CountWords();
            int paragraphsCount = document.GetChildElements(true, ElementType.Paragraph).Count();
            int pageCount = document.GetPaginator().Pages.Count;

            // Display file's count statistics.
            Console.WriteLine($"Characters count: {charactersCount}");
            Console.WriteLine($"     Words count: {wordsCount}");
            Console.WriteLine($"Paragraphs count: {paragraphsCount}");
            Console.WriteLine($"     Pages count: {pageCount}");
            Console.WriteLine();

            // Display file's text content.
            Console.WriteLine(text);

            document.Sections[0].PageSetup.PageMargins.Left = document.Sections[0].PageSetup.PageMargins.Right = 50;
            var elements = document.GetChildElements(true, ElementType.Picture).ToList();
            if (elements.Count > 0)
            {
                ((Picture)elements[0]).PictureStream = new MemoryStream(File.ReadAllBytes(@"C:\Project\Github\IFS-EIVO03\TestConsole\bin\Debug\buyer.jpg"));
            }
            int listIdx = 0;
            char chIdx = '壹';
            foreach (var p in document.GetChildElements(true, ElementType.Paragraph))
            {
                Paragraph paragraph = (Paragraph)p;
                paragraph.ParagraphFormat.LineSpacing = 1.21;
                if (paragraph.ListFormat?.ListLevelFormat != null)
                {
                    //paragraph.ListFormat.ListLevelFormat.NumberFormat = "%1.";
                    Console.WriteLine(paragraph.ListFormat.ListLevelFormat.NumberFormat);
                    //paragraph.ListFormat.ListLevelFormat.NumberStyle = NumberStyle.JapaneseCounting;
                }
            }
            document.Save("G:\\temp\\test.pdf");
        }

        private static void Test01()
        {
            Console.WriteLine("Hello, World!");

            using (WebClient client = new WebClient())
            {
                //client.Timeout = 43200000;
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var dataItem = File.ReadAllText("G:\\temp\\data.txt");
                String result = client.UploadString("https://localhost:5051/Home/DoSignature", dataItem);

                Console.WriteLine(result);
            }
        }
    }
}