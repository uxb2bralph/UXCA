using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Helper;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace ContractHome.Controllers
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
            StampDate = ((initiatContractSignatureRequest.StampDate.HasValue) ?
                initiatContractSignatureRequest?.StampDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty);
            SignerDate = ((initiatContractSignatureRequest.SignatureDate.HasValue) ?
                initiatContractSignatureRequest?.SignatureDate.Value.ToString("yyyy/MM/dd HH:mm") : string.Empty);
            SignerID = (initiatContractSignatureRequest?.UserProfile?.PID ?? string.Empty);
            isInitiator = contractingParty.IsInitiator ?? false;
            IsCurrentUserCompany = (userCompanyID != null) ? (ID == userCompanyID) : false;
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

    public class PartyRefs: PartyBase
    {
        [JsonProperty]
        public List<SignaturePositionBase> SignaturePositions { get; set; }

        public PartyRefs(Contract contract,
            ContractingParty contractingParty,
            int? userCompanyID = null) :base(contract, contractingParty, userCompanyID)
        {
            SignaturePositions = contract.ContractSignaturePositionRequest.Select(x =>
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

    public class ContractWithRefsResponse: ContractBase
    {
        [JsonProperty]
        public List<PartyRefs> Parties { get; set; }

        public ContractWithRefsResponse(Contract contract, int? userCompanyID = null):base(contract)
        {
            Parties = contract.ContractingParty.Select(x => new PartyRefs(contract, x, userCompanyID)).ToList();
        }
    }


    public class ContractBase
    {
        [JsonProperty]
        public Contract Contract { get; set; }
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


        public ContractBase(Contract contract, int? userCompanyID = null)
        {
            Contract = contract;
            KeyID = contract.ContractID.EncryptKey();
            ContractNo = contract.ContractNo;
            Title = contract.Title;
            IsJointContracting = contract.IsJointContracting ?? false;
            PageCount = contract.GetPdfPageCount();
            CurrentStep = contract.CDS_Document.CurrentStep;
        }
    }

}
