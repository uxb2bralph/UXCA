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
            public int FavoriteSignerID => KeyID.DecryptKeyValue();
            /// <summary>
            /// 常用簽署人ID
            /// </summary>
            [JsonIgnore]
            public int SignerUID => SignerKeyID.DecryptKeyValue();
            /// <summary>
            /// 建立人ID
            /// </summary>
            [JsonIgnore]
            public int CreatorUID => CreatorKeyID.DecryptKeyValue();

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
    }
}
