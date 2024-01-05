using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace ContractHome.Models.Helper
{
    public class PartyBase
    {
        public PartyBase(Contract contract,
            ContractingParty contractingParty,
            int? userCompanyID = null)
        {

            var initiatContractSignatureRequest =
                contract.ContractSignatureRequest.Where(x => x.CompanyID == contractingParty.CompanyID).FirstOrDefault();

            ID = contractingParty.CompanyID;
            KeyID = contractingParty.CompanyID.EncryptKey();
            Name = contractingParty.Organization.CompanyName;
            StampDate = string.Empty;
            SignerDate = string.Empty;
            SignerID = string.Empty;
            if (initiatContractSignatureRequest!=null)
            { 
                StampDate = (initiatContractSignatureRequest.StampDate.HasValue) ?
                    initiatContractSignatureRequest?.StampDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
                SignerDate = initiatContractSignatureRequest.SignatureDate.HasValue ?
                    initiatContractSignatureRequest?.SignatureDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
                SignerID = initiatContractSignatureRequest?.UserProfile?.PID ?? string.Empty;
            }
            isInitiator = contractingParty.IsInitiator ?? false;
            IsCurrentUserCompany = userCompanyID != null ? ID == userCompanyID : false;
        }

        [JsonIgnore]
        public int? ID { get; set; }

        [JsonProperty]
        public string KeyID { get; set; }

        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string? StampDate { get; set; }
        [JsonProperty]
        public string? SignerDate { get; set; }
        [JsonProperty]
        public string? SignerID { get; set; }
        [JsonProperty]
        public bool isInitiator { get; set; }
        [JsonProperty]
        public bool IsCurrentUserCompany { get; set; }


    }

    public class PartyRefs : PartyBase
    {
        [JsonProperty]
        public List<SignaturePositionBase> SignaturePositions { get; set; }

        public PartyRefs(Contract contract,
            ContractingParty contractingParty,
            string queryItem = "",
            int? userCompanyID = null) : base(contract, contractingParty, userCompanyID)
        {
            if (queryItem.Equals("SignaturePositions"))
            {
                SignaturePositions = contract.ContractSignaturePositionRequest
                .Where(x=>x.ContractorID==contractingParty.CompanyID)
                .Select(x =>
                new SignaturePositionBase(
                    id: x.PositionID,
                    scaleWidth: x.ScaleWidth,
                    scaleHeight: x.ScaleHeight,
                    marginTop: x.MarginTop,
                    marginLeft: x.MarginLeft,
                    type: x.Type,
                    pageIndex: x.PageIndex
                )).ToList();
            }
        }
    }

    public class SignaturePositionBase
    {
        public string ID { get; set; }

        public double? ScaleWidth { get; set; }

        public double? ScaleHeight { get; set; }

        public double? MarginTop { get; set; }

        public double? MarginLeft { get; set; }

        public int? Type { get; set; }

        public int? PageIndex { get; set; }


        public SignaturePositionBase(string id, double? scaleWidth, double? scaleHeight,
            double? marginTop, double? marginLeft, int? type, int? pageIndex)
        {
            ID = id;
            ScaleWidth = scaleWidth;
            ScaleHeight = scaleHeight;
            MarginTop = marginTop;
            MarginLeft = marginLeft;
            Type = type;
            PageIndex = pageIndex;
        }
    }

    public class ContractRefs : ContractBase
    {
        [JsonProperty]
        public List<PartyRefs> Parties { get; set; }
        public ContractRefs(Contract contract, string queryItem="", int? userCompanyID = null) : base(contract, queryItem)
        {
            Parties = contract.ContractingParty
                .Select(x => new PartyRefs(contract, x, queryItem, userCompanyID)).ToList();
        }
    }

    public class ContractBase
    {
        [JsonProperty]
        public string KeyID { get; set; }
        [JsonProperty]
        public string ContractNo { get; set; }
        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public bool? IsJointContracting { get; set; }
        [JsonProperty]
        public int? CurrentStep { get; set; }
        [JsonProperty]
        public int? PageCount { get; set; }
        [JsonProperty]
        public IEnumerable<ProcessLog>? ProcessLogs { get; set; }
        public ContractBase(Contract contract, string? queryItem = "", int? userCompanyID = null)
        {
            KeyID = contract.ContractID.EncryptKey();
            ContractNo = contract.ContractNo;
            Title = contract.Title;
            IsJointContracting = contract.IsJointContracting ?? false;
            PageCount = contract.GetPdfPageCount();
            CurrentStep = contract.CDS_Document.CurrentStep;
            if (queryItem.Equals("ProcessLog"))
            {
                ProcessLogs = contract.CDS_Document.DocumentProcessLog.Select(l =>
                    new ProcessLog(
                        time: $"{l.LogDate:yyyy/MM/dd HH:mm:ss}",
                        action: CDS_Document.StepNaming[l.StepID],
                        role: l.UserProfile?.UserName
                    ));
            }
        }
    }

    public class ProcessLog
    {
        public ProcessLog(string time, string action, string role)
        {
            this.time = time;
            this.action = action;
            this.role = role;
        }

        public string time { get; set; }
        public string action { get; set; }
        public string role { get; set; }
    }
}
