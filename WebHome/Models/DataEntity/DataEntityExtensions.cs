using WebHome.Properties;

namespace WebHome.Models.DataEntity
{
    public static partial class DataEntityExtensions
    {
    }

    public partial class UXCADataContext
    {
        public UXCADataContext() :
                base(Settings.Default.UXCAConnection, mappingSource)
        {
            OnCreated();
        }

        partial void OnCreated()
        {
            this.CommandTimeout = 300;
        }

    }
}
