using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Helper;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace ContractHome.Services.ContractService
{
    /// <summary>
    /// 搜尋合約Model
    /// </summary>
    public class ContractSearchDtos
    {
        /// <summary>
        /// Base Model
        /// </summary>
        public class ContractBaseModel
        {
            public string KeyID { get; set; } = string.Empty;
        }

        public class ContractSearchModel : ContractBaseModel
        {
            public string? ContractNo { get; set; } = String.Empty;
            /// <summary>
            /// 授權分類
            /// </summary>
            public List<int> ContractCategoryID { get; set; } = [];

            public CDS_Document.StepEnum[]? QueryStep { get; set; }
            public string? ContractDateFrom { get; set; } = string.Empty;
            public string? ContractDateTo { get; set; } = string.Empty;
            public string? Initiator { get; set; } = string.Empty;

            public int? InitiatorID => (string.IsNullOrEmpty(Initiator)) ? null : Initiator.DecryptKeyValue();

            public string? Contractor { get; set; } = string.Empty;

            public int? ContractorID => (string.IsNullOrEmpty(Contractor)) ? null : Contractor.DecryptKeyValue();
            public int PageSize { get; set; } = 10;
            public int PageIndex { get; set; } = 1;
            public string[]? SortName { get; set; }
            public int?[]? SortType { get; set; }

            public int SearchUID { get; set; } = 0;

            public int SearchCompanyID { get; set; } = 0;
        }

        public class  ContractListDataModel
        {
            public IEnumerable<ContractInfoMode> Contracts { get; set; } = [];

            public int TotalRecordCount { get; set; } = 0;
        }

        /// <summary>
        /// 合約資訊Model
        /// </summary>
        public class ContractInfoMode : ContractBaseModel
        {
            [JsonIgnore]
            public int ContractID { get; set; }
            public string ContractNo { get; set; } = string.Empty;

            public string ContractCategory { get; set; } = string.Empty;

            [JsonIgnore]
            public int ContractCategoryID { get; set; }

            public string CategoryName { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public bool? IsPassStamp { get; set; }

            [JsonIgnore]
            public DateTime CreatedDateTimeO { get; set; }
            public string CreatedDateTime => CreatedDateTimeO.ToString("yyyy/MM/dd HH:mm:ss");

            public int? CurrentStep { get; set; }

            [JsonIgnore]
            public DateTime? NotifyUntilDateO { get; set; }
            public string NotifyUntilDate => NotifyUntilDateO?.ToString("yyyy/MM/dd") ?? "";

            public bool IsExpiringSoon => NotifyUntilDateO.HasValue &&
                                          NotifyUntilDateO <= DateTime.Now.Date.AddDays(2);

            public int? PageCount { get; set; }

            public IEnumerable<ProcessLog> ProcessLogs { get; set; } = [];

            public IEnumerable<PartyRefs> Parties { get; set; } = [];
        }
        /// <summary>
        /// 簽屬流程紀錄
        /// </summary>
        public class ProcessLog
        {
            public string Time { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        /// <summary>
        /// 簽署人狀態
        /// </summary>
        public class PartyRefs : ContractBaseModel
        {
            [JsonIgnore]
            public int CompanyID => KeyID.DecryptKeyValue();

            public string CompanyName { get; set; } = string.Empty;
            [JsonIgnore]
            public int? ContractID { get; set; }

            public int? Step { get; set; }
             
            public string StampDate { get; set; } = string.Empty;

            public string SignerDate { get; set; } = string.Empty;

            public string SignerID { get; set; } = string.Empty;

            public bool? IsInitiator { get; set; }

            public bool IsCurrentUserCompany { get; set; }

        }
    }
}
