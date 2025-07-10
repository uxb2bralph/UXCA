namespace ContractHome.Properties
{
  public class Settings : CommonLib.Utility.Properties.AppSettings
  {
    static Settings _default;
    public static Settings Default => _default;
    static Settings()
    {
      _default = Initialize<Settings>(typeof(Settings).Namespace);
    }
    public static void Reload()
    {
      _default = Initialize<Settings>(typeof(Settings).Namespace);
    }

    public String ApplicationPath { get; set; } = "";
    public double SessionTimeoutInMinutes { get; set; } = 20;
    public double LoginExpireMinutes { get; set; } = 1440 * 7;
    public String LoginUrl { get; set; } = "/Account/Login";
    public String LogoutUrl { get; set; } = "/Account/Logout";
    public String WebAppDomain { get; set; } = "https://localhost:5153";
    public String ContractListUrl { get; set; } = $"https://localhost:5153/ContractConsole/ListToStampIndex";
    public String DCDBConnection { get; set; } = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=DigitalContract;Data Source=.\\SQLEXPRESS";
    public double? LeftMargins { get; set; }
    public double LineSpacing { get; set; } = 0;
    public String GemboxKey { get; set; } = string.Empty;
    public String IronPdfKey { get; set; } = "IRONSUITE.FUNCHZU.GMAIL.COM.7482-8507734A46-AY4VINHSZSWEFW-PD6BDSWGLX4Y-7NJAGO7GBPT6-FUVG36G55LYM-B4NWY2DJOVXH-SPINCQA2NIB3-ASO2GP-TJLIVI24JIGKUA-DEPLOYMENT.TRIAL-MTRY4R.TRIAL.EXPIRES.22.SEP.2023";
    public String TemplateContractDocx { get; set; } = "Sample.docx";
    public double? SealImageDPI { get; set; }
    public String StoreRoot { get; set; } = "WebStore";
    public String DefaultUILanguage { get; set; } = "zh-TW";
    public bool IsIdentityCertCheck { get; set; } = false;

    public string HttpChunkUploadUrl { get; set; } = "http://192.168.7.124:5150/Try/ChunkDownload";
    public String[][] ConnectionList { get; set; } =
    new[]
    {
            new [] { "電子簽章", "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=DigitalContract;Data Source=.\\SQLEXPRESS" }
    };
    public CHTSigningService CHTSigning { get; set; } = new CHTSigningService();

  }

  public class CHTSigningService
  {
    public int SystemTokenID { get; set; } = 1;
    public String AP_SignPDF_Encrypt { get; set; } = "https://eguitest.uxifs.com/CryptoKeyManageCVS2API/3partypdfencsign/GRA_TPSSC_IV_CVS2_AP.do";
    public String AP_SignPDF { get; set; } = "https://eguitest.uxifs.com/CryptoKeyManageCVS2API/3partypdfsign/GRA_TPSSC_IV_CVS2_AP.do";
    public String User_SignPDF_Encrypt { get; set; } = "https://eguitest.uxifs.com/CryptoKeyManageCVS2API/3partypdfencsign/GRA_TPSSC_IV_CVS2.do";
    public String User_SignPDF { get; set; } = "https://eguitest.uxifs.com/CryptoKeyManageCVS2API/3partypdfsign/GRA_TPSSC_IV_CVS2.do";
    public String UserRequestTicket { get; set; } = "https://graapplywebtest.azurewebsites.net/aatl/v1/ticket/user";
    public String AuthorizeUserTicket { get; set; } = "https://graapplywebtest.azurewebsites.net/aatl/V2/SignVerify?authToken=";
    public String RequireIssue { get; set; } = "https://graapplywebtest.azurewebsites.net/aatl/v1/certificate/partner/issue";
    public String ReturnUrl { get; set; } = "https://portal.uxifs.com/ContractHome/Account/Login";
  }
}
