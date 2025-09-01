using ContractHome.Helper;
using System.Text.Json.Serialization;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerDtos
    {
        public class FavoriteSignerBaseModel
        {
            public string KeyID { get; set; } = string.Empty;
        }

        /// <summary>
        /// 常用簽署人
        /// </summary>
        public class FavoriteSignerInfoModel : FavoriteSignerBaseModel
        {
            /// <summary>
            /// 流水號
            /// </summary>
            [JsonIgnore]
            public int FavoriteSignerID => (!string.IsNullOrEmpty(KeyID)) ? KeyID.DecryptKeyValue() : -1;
            /// <summary>
            /// 常用簽署人ID
            /// </summary>
            [JsonIgnore]
            public int SignerUID => (!string.IsNullOrEmpty(SignerKeyID)) ? SignerKeyID.DecryptKeyValue() : -1;
            /// <summary>
            /// 建立人ID
            /// </summary>
            [JsonIgnore]
            public int CreatorUID { get; set; } = -1;

            public string SignerKeyID { get; set; } = string.Empty;

            public string CreatorKeyID { get; set; } = string.Empty;

            /// <summary>
            /// 簽署人mail
            /// </summary>
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// 公司名稱
            /// </summary>
            public string CompanyName { get; set; } = string.Empty;

            /// <summary>
            /// 統編
            /// </summary>
            public string ReceiptNo { get; set; } = string.Empty;
        }

        public class CompanyInfoModel : FavoriteSignerBaseModel
        {
            [JsonIgnore]
            public int CompanyID => KeyID.DecryptKeyValue();
            /// <summary>
            /// 公司名稱
            /// </summary>
            public string CompanyName { get; set; } = string.Empty;
            /// <summary>
            /// 統編
            /// </summary>
            public string ReceiptNo { get; set; } = string.Empty;
        }

        public class SignerInfoModel : FavoriteSignerBaseModel
        {
            [JsonIgnore]
            public int SignerID => KeyID.DecryptKeyValue();
            /// <summary>
            /// 簽署人mail
            /// </summary>
            public string Email { get; set; } = string.Empty;
            /// <summary>
            /// 公司名稱
            /// </summary>
            public string CompanyName { get; set; } = string.Empty;
            /// <summary>
            /// 統編
            /// </summary>
            public string ReceiptNo { get; set; } = string.Empty;
        }

        public class  QueryInfoModel 
        {
            /// <summary>
            /// 公司名稱
            /// </summary>
            public string Keyword { get; set; } = string.Empty;

            public string ReceiptNo { get; set; } = string.Empty;
        }
    }
}
