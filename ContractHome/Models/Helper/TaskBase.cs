using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using static ContractHome.Models.DataEntity.CDS_Document;

namespace ContractHome.Models.Helper
{
    public class TaskUserBase
    {
        public TaskUserBase(Contract contract,
            ContractingUser contractingUser,
            UserProfile userProfile = null)
        {

            var initiatContractSignatureRequest =
                contract.ContractUserSignatureRequest.Where(x => x.UserID == contractingUser.UserID).FirstOrDefault();
            ID = contractingUser.UserID;
            ContractID = contract.ContractID;
            KeyID = contractingUser.UserID.EncryptKey();
            Name = string.IsNullOrEmpty(contractingUser.UserProfile.OperatorNote)?$"{contractingUser.UserProfile.PID}": $"{ contractingUser.UserProfile.OperatorNote}";
            Step = contract.CDS_Document.CurrentStep;
            StampDate = (initiatContractSignatureRequest.StampDate.HasValue) ?
                initiatContractSignatureRequest?.StampDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
            SignerDate = initiatContractSignatureRequest.SignatureDate.HasValue ?
                initiatContractSignatureRequest?.SignatureDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty;
            SignerID = initiatContractSignatureRequest?.UserProfile?.PID ?? string.Empty;
            if ((Step == (int)StepEnum.Sealing) && (!string.IsNullOrEmpty(StampDate)))
            {
                Step = (int)StepEnum.Sealed; 
            }
            if ((Step == (int)StepEnum.DigitalSigning) && (!string.IsNullOrEmpty(SignerDate)))
            {
                Step = (int)StepEnum.DigitalSigned;
            }

            isInitiator = (contract.CreateUID==contractingUser.UserID);
            IsCurrentUser = (contractingUser.UserID == userProfile.UID); //用印/簽署權限? A:step符合, 作業為[自己]
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
        public bool IsCurrentUser { get; set; }

    }

    public class TaskUserRefs : TaskUserBase
    {
        [JsonProperty]
        public List<FieldPositionBase> SignaturePositions { get; set; }

        public TaskUserRefs(Contract contract,
            ContractingUser contractingUser,
            UserProfile userProfile,
            string queryItem = ""
            ) : base(contract, contractingUser, userProfile)
        {
            if (queryItem.Equals("SignaturePositions"))
            {
                SignaturePositions = contract.ContractSignaturePositionRequest
                .Where(x => x.OperatorID == contractingUser.UserID)
                .Select(x =>
                new FieldPositionBase(
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

    public class FieldPositionBase
    {
        public string ID { get; set; }

        public double? ScaleWidth { get; set; }

        public double? ScaleHeight { get; set; }

        public double? MarginTop { get; set; }

        public double? MarginLeft { get; set; }

        public int? Type { get; set; }

        public int? PageIndex { get; set; }


        public FieldPositionBase(string id, double? scaleWidth, double? scaleHeight,
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

    public class TaskRefs : TaskBase
    {
        [JsonProperty]
        public List<TaskUserRefs> Parties { get; set; }
        public TaskRefs(Contract contract, UserProfile userProfile, string queryItem="") : base(contract, queryItem)
        {
            Parties = contract.ContractingUser
                .Select(x => new TaskUserRefs(contract, x,userProfile, queryItem)).ToList();
        }
    }

    public class TaskBase
    {
        [JsonProperty]
        public string KeyID { get; set; }
        [JsonProperty]
        public string ContractNo { get; set; }
        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public bool? IsPassStamp { get; set; }
        [JsonProperty]
        public string CreatedDateTime { get; set; }
        [JsonProperty]
        public int? CurrentStep { get; set; }
        [JsonProperty]
        public int? PageCount { get; set; }
        [JsonProperty]
        public IEnumerable<ProcessLog>? ProcessLogs { get; set; }
        public TaskBase(Contract contract, string? queryItem = "", int? userCompanyID = null)
        {
            KeyID = contract.ContractID.EncryptKey();
            ContractNo = contract.ContractNo;
            Title = contract.Title;
            IsPassStamp = contract.IsPassStamp ?? false;
            CreatedDateTime = contract.CDS_Document.DocDate.ReportDateTimeString();
            PageCount = contract.GetPdfPageCount();
            CurrentStep = contract.CDS_Document.CurrentStep;
            
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
}
