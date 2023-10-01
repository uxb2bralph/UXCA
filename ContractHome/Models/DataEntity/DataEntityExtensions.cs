using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Utility;
using System.Collections;

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
    }

    public partial class UserRoleDefinition
    {
        public enum RoleEnum
        {
            SystemAdmin = 0,
            User = 1,
            MemberAdmin = 2,
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

        protected internal Dictionary<Object,Object>? _values;
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
    }

    public partial class CDS_Document
    {
        public enum ProcessTypeEnum
        {
            DOCX = 1,
            PDF = 2,
        }

        public bool IsPDF => ProcessType == (int)ProcessTypeEnum.PDF;
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
