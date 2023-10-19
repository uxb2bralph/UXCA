using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Helper;
using ContractHome.Properties;
using GemBox.Document;
using IronPdf.Editing;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractHome.Helper
{
    public static class ContractConsoleExtensions
    {
        public static String StoreContractDocument(this IFormFile file)
        {
            String filePath = Path.Combine(Contract.ContractStore, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            file.SaveAs(filePath);
            return filePath;
        }

        public static bool CanAffixSeal(this GenericManager<DCDataContext> models, ContractSealRequest request, int uid)
        {
            if (models.GetTable<ContractingParty>().Where(p => p.ContractID == request.ContractID)
                        .Where(p => p.IntentID == request.SealTemplate.IntentID)
                        .Join(models.GetTable<OrganizationUser>().Where(o => o.UID == uid),
                            p => p.CompanyID, o => o.CompanyID, (p, o) => p)
                        .Any())
            {
                return true;
            }

            return false;
        }
        public static bool CanCommitSignature(this GenericManager<DCDataContext> models, ContractSignatureRequest request, int uid, out OrganizationUser? orgUser)
        {
            orgUser = null;
            if (!request.SignerID.HasValue)
            {
                orgUser = models.GetTable<OrganizationUser>().Where(p => p.CompanyID == request.CompanyID)
                                            .Where(p => p.UID == uid)
                                            .FirstOrDefault();
                return orgUser != null;
            }

            return false;
        }

        public static bool CanAffixSeal(this GenericManager<DCDataContext> models, ContractSignatureRequest request, int uid)
        {
            if (models.GetTable<OrganizationUser>()
                    .Where(o => o.UID == uid)
                    .Where(p => p.CompanyID == request.CompanyID)
                    .Any())
            {
                return true;
            }

            return false;
        }

        public static bool IsSealedByAll(this Contract contract)
        {
            return !contract.ContractSignatureRequest.Any(s => !s.StampDate.HasValue);
        }

        public static MemoryStream BuildContractWithSignature(this Contract contract, GenericManager<DCDataContext> models, bool preview = false)
        {
            if (contract.ContractSignature != null)
            {
                ContractSignatureRequest request = contract.ContractSignature.ContractSignatureRequest;
                if (request.ResponsePath != null && File.Exists(request.ResponsePath))
                {
                    if (request.RequestPath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        JObject content = JObject.Parse(File.ReadAllText(request.ResponsePath));
                        if ((String)content["code"] == "0")
                        {
                            return new MemoryStream(Convert.FromBase64String((String)content["msg"]));
                        }
                    }
                    else
                    {
                        return new MemoryStream(File.ReadAllBytes(request.ResponsePath));
                    }
                }
            }

            return contract.BuildContractWithSeal();
        }

        public static String BuildContractWithSignatureBase64(this Contract contract, GenericManager<DCDataContext> models, bool preview = false)
        {
            if (contract.ContractSignature != null)
            {
                ContractSignatureRequest request = contract.ContractSignature.ContractSignatureRequest;
                if (request.ResponsePath != null && File.Exists(request.ResponsePath))
                {
                    if (request.RequestPath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        JObject content = JObject.Parse(File.ReadAllText(request.ResponsePath));
                        if ((String)content["code"] == "0")
                        {
                            return (String)content["msg"];
                        }
                    }
                    else
                    {
                        return Convert.ToBase64String(File.ReadAllBytes(request.ResponsePath));
                    }
                }
            }

            using (MemoryStream stream = contract.BuildContractWithSeal())
            {
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static void TransitStep(this CDS_Document document, GenericManager<DCDataContext> models, int actorID, CDS_Document.StepEnum step)
        {
            document.DocumentProcessLog.Add(new DocumentProcessLog
            {
                LogDate = DateTime.Now,
                ActorID = actorID,
                StepID = (int)step,
            });

            document.CurrentStep = (int)step;
            models.SubmitChanges();
        }

        public static int GetPdfPageCount(this Contract contract)
        {
            String? content = null;
            if (contract == null)
            {
                return 0;
            }

            if (File.Exists(contract.FilePath))
            {
                content = File.ReadAllText(contract.FilePath);
            }
            else if (contract.ContractContent != null)
            {
                content = Encoding.ASCII.GetString(contract.ContractContent.ToArray());
            }

            if (content == null)
            {
                return 0;
            }

            var match = Regex.Match(content, "/Count(?([^\\r\\n])\\s)\\d+");
            if (match.Value != String.Empty)
            {
                return int.Parse(match.Value.Substring(7));
            }

            return 0;
        }

    }
}
