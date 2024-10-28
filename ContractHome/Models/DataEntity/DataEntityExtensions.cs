using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Utility;
using System.Collections;
using BitMiracle.LibTiff.Classic;
using System.Security.Cryptography.X509Certificates;
using System.Data.Linq.SqlClient;
using ContractHome.Models.Helper;
using static ContractHome.Models.Helper.ContractServices;

namespace ContractHome.Models.DataEntity
{
    public static partial class DataEntityExtensions
    {
    }

    public partial class DCDataContext
    {
        public DCDataContext() :
                base(Settings.Default.DCDBConnection, mappingSource)
        {
            OnCreated();
        }

        partial void OnCreated()
        {
            this.CommandTimeout = 300;
        }

    }

    public partial class Contract
    {
        public static String ContractStore { get; } = "Contract".WebStoreTargetDailyPath().CheckStoredPath();
        public int CurrentStep => CDS_Document.CurrentStep ?? 0;
        public bool HasUserInProgress => (this.UserInProgress != null);
        public bool IsSameUserInProgress(int? userid) =>
            (userid != null) && (userid == this.UserInProgress);
    }

    public partial class UserRoleDefinition
    {
        public enum RoleEnum
        {
            SystemAdmin = 0,
            User = 1,
            MemberAdmin = 2,
            Operator = 3
        }
    }

    public partial class UserProfile
    {
        public int? RoleIndex { get; set; }
        public UserRole? CurrentUserRole { get; private set; }
        public void DetermineUserRole()
        {
            RoleIndex = UserRole.Any() ? 0 : -1;
            if (RoleIndex >= 0)
            {
                CurrentUserRole = UserRole[RoleIndex.Value];
            }
        }

        protected internal Dictionary<Object, Object>? _values;
        public object? this[object index]
        {
            get
            {
                return _values?[index];
            }
            set
            {
                if (_values == null)
                {
                    _values = new Dictionary<object, object>();
                }

                if (value == null)
                {
                    _values.Remove(index);
                }
                else
                {
                    _values[index] = value;
                }
            }
        }

        public static UserProfile PrepareNewItem(DCDataContext models)
        {
            UserProfile item = new UserProfile();
            models.UserProfile.InsertOnSubmit(item);
            return item;
        }

        public int CompanyID =>
            (OrganizationUser != null) ? OrganizationUser.Organization.CompanyID : 0;

        public string CompanyName =>
            (CompanyID != null && (CompanyID != 0)) ? Organization.CompanyName : string.Empty;

        public Organization? Organization =>
            ((CompanyID != null)&& (CompanyID != 0)) ? OrganizationUser.Organization : null;

        public bool IsUserWantToCheckReceiptNo => (this.IsOperator() && !string.IsNullOrEmpty(OperatorReceiptNo))
            ||(this.IsUser()&&!string.IsNullOrEmpty(Organization.ReceiptNo));
        public string GetReceiptNoByRole => (this.IsOperator()) ? OperatorReceiptNo??string.Empty : Organization.ReceiptNo??string.Empty;
    }

    public partial class CDS_Document
    {
        public enum ProcessTypeEnum
        {
            DOCX = 1,
            PDF = 2,
        }

        public enum StepEnum
        {
            Initial = 0,
            Config = 1,
            Undefind =2,
            FieldSet = 3,
            Establish = 5,
            Sealing = 6,
            Sealed = 7,
            DigitalSigning = 10,
            DigitalSigned = 11,
            Browsed = 16,
            Terminated = 17,
            Committed = 18,
            Revoked = 19,
        }

        public static readonly String[] StepNaming =
            {
                "開始",//0
                "設定",//1
                "無法定義",//2
                "欄位設定",//3
                "",//4
                "建立",//5
                "用印中",//6
                "用印完成",//7
                "--",//8
                "",//9
                "簽章中",//10
                "簽章完成",//11
                "",//12
                "",//13
                "",//14
                "",//15
                "瀏覽",//16
                "已終止",//17
                "完成",//18
                "撤銷",//19
                ""//20
            };

        public bool IsPendingState() => (this.CurrentStep==null) ? 
                false:PendingState.Contains((CDS_Document.StepEnum)this.CurrentStep!); 
       
        public static StepEnum[] PendingState =
        {
            StepEnum.Establish,
            StepEnum.Sealing,
            StepEnum.Sealed,
            StepEnum.DigitalSigning,
            StepEnum.DigitalSigned
        };

        public static StepEnum[] DocumentEditable =
        {
            StepEnum.Establish,
            StepEnum.Sealing,
            StepEnum.Sealed,
            StepEnum.DigitalSigning
        };

        public bool IsPDF => ProcessType == (int)ProcessTypeEnum.PDF;

        public DocumentProcessLog? CurrentLog => this.DocumentProcessLog
            .Where(d => d.StepID != (int)StepEnum.Browsed)
            .OrderByDescending(d => d.LogID).FirstOrDefault();
    }

    public partial class ContractingIntent
    {
        public enum ContractingIntentEnum
        {
            Initiator = 1,
            Contractor = 2,
        }
    }

    public partial class Organization
    {
        public static Organization PrepareNewItem(DCDataContext models)
        {
            Organization item = new Organization();
            models.Organization.InsertOnSubmit(item);
            return item;
        }
    }

}
