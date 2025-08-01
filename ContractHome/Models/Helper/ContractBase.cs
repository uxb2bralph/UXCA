using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using static ContractHome.Models.DataEntity.CDS_Document;

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

            ContractID = contract.ContractID;
            ID = contractingParty.CompanyID;
            KeyID = contractingParty.CompanyID.EncryptKey();
            Name = $"{contractingParty.Organization.CompanyName} ({contractingParty.Organization.ReceiptNo})";
            StampDate = string.Empty;
            SignerDate = string.Empty;
            SignerID = string.Empty;
            Step = contract.CDS_Document.CurrentStep;
            if (initiatContractSignatureRequest!=null)
            { 
                StampDate = (initiatContractSignatureRequest.StampDate.HasValue) ?
                    initiatContractSignatureRequest?.StampDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
                SignerDate = initiatContractSignatureRequest.SignatureDate.HasValue ?
                    initiatContractSignatureRequest?.SignatureDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
                SignerID = initiatContractSignatureRequest?.UserProfile?.PID ?? string.Empty;
                //if ((Step == (int)StepEnum.Sealing) && (!string.IsNullOrEmpty(StampDate)))
                //{
                //    Step = (int)StepEnum.Sealed; 
                //}
                //if ((Step == (int)StepEnum.DigitalSigning) && (!string.IsNullOrEmpty(SignerDate)))
                //{
                //    Step = (int)StepEnum.DigitalSigned;
                //}
                // 判斷是否跳過用印
                if (contract.IsPassStamp.HasValue && !contract.IsPassStamp.Value)
                {
                    // 先用印後簽署 判斷用印跟簽屬時間 設定 用印中 或 簽署中

                    // 未用印 未簽署
                    if (string.IsNullOrEmpty(StampDate) && string.IsNullOrEmpty(SignerDate))
                    {
                        Step = (int)StepEnum.Sealing;
                    }

                    // 已用印 未簽署
                    if (!string.IsNullOrEmpty(StampDate) && string.IsNullOrEmpty(SignerDate))
                    {
                        Step = (int)StepEnum.DigitalSigning;
                    }

                    // 已用印 已簽署
                    if (!string.IsNullOrEmpty(StampDate) && !string.IsNullOrEmpty(SignerDate))
                    {
                        Step = (int)StepEnum.DigitalSigned;
                    }

                } else
                {
                    // 跳過用印做簽署 判斷簽署時間
                    if (string.IsNullOrEmpty(SignerDate))
                    {
                        Step = (int)StepEnum.DigitalSigning;
                    }
                }

            }
            isInitiator = contractingParty.IsInitiator ?? false;
            // 假如合約在設定狀態 則設定甲方人員為設定狀態
            if (isInitiator && contract.CDS_Document.CurrentStep == (int)CDS_Document.StepEnum.Config)
            {
                Step = (int)CDS_Document.StepEnum.Config;
            }

            IsCurrentUserCompany = userCompanyID != null ? ID == userCompanyID : false;
        }

        [JsonIgnore]
        public int? ID { get; set; }

        [JsonProperty]
        public int? ContractID { get; set; }

        [JsonProperty]
        public int? Step { get; set; }

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
            Parties = contract.ContractingParty.OrderByDescending(x => x.IsInitiator)
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
        public string ContractCategory { get; set; } = string.Empty;
        [JsonProperty]
        public string CategoryName { get; set; }

        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public bool? IsPassStamp { get; set; }
        [JsonProperty]
        public string CreatedDateTime { get; set; }
        [JsonProperty]
        public int? CurrentStep { get; set; }

        [JsonProperty]
        public string NotifyUntilDate { get; set; }
        [JsonProperty]
        public bool IsExpiringSoon { get; set; } = false;

        [JsonProperty]
        public int? PageCount { get; set; }
        [JsonProperty]
        public IEnumerable<ProcessLog>? ProcessLogs { get; set; }
        public ContractBase(Contract contract, string? queryItem = "", int? userCompanyID = null)
        {
            KeyID = contract.ContractID.EncryptKey();
            ContractNo = contract.ContractNo;
            ContractCategory = contract.ContractCategoryID.EncryptKey();
            CategoryName = contract.GetCategoryName();
            Title = contract.Title;
            IsPassStamp = contract.IsPassStamp ?? false;
            CreatedDateTime = contract.CDS_Document.DocDate.ReportDateTimeString();
            PageCount = contract.GetPdfPageCount();
            CurrentStep = contract.CDS_Document.CurrentStep;

            NotifyUntilDate = contract.NotifyUntilDate?.ToString("yyyy/MM/dd") ?? "";
            IsExpiringSoon = contract.NotifyUntilDate.HasValue &&
                             contract.NotifyUntilDate <= DateTime.Now.Date.AddDays(2);

            if ((queryItem!=null)&&(queryItem.Equals("ProcessLog")))
            {
                ProcessLogs = contract.CDS_Document.DocumentProcessLog.Select(l =>
                    new ProcessLog(
                        time: l.LogDate.ReportDateTimeString(),
                        action: CDS_Document.StepNaming[l.StepID],
                        role: l.UserProfile != null ? l.UserProfile.PID : ""
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
