using CommonLib.Core.Utility;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Helper;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ContractHome.Services.Jobs
{
  public class JobTouchWebEveryday : JobAbstract, IRecurringJob
  {
    static readonly HttpClient client = new HttpClient();
    public JobTouchWebEveryday(IOptions<List<JobSetting>> jobSettings)
        : base(jobSettings)
    {
    }

    public async Task Execute()
    {
      if (IsEnable)
      {
        try
        {
          using HttpResponseMessage response = await client.GetAsync($"{Settings.Default.WebAppDomain}");
          response.EnsureSuccessStatusCode();
          string responseBody = await response.Content.ReadAsStringAsync();
          if (!responseBody.Contains("UX SIGN 數位簽章管理系統"))
            throw new HttpRequestException($"UX SIGN 網頁: {Settings.Default.WebAppDomain} 未正常回應");
          else
            FileLogger.Logger.Info($"JobTouchWebEveryday:{Settings.Default.WebAppDomain} 測試正常");
        }
        catch (HttpRequestException e)
        {
          FileLogger.Logger.Error(e);
        }
      }
    }
  }
}
