using CommonLib.DataAccess;

namespace ContractHome.Models.DataEntity
{
    public class ModelSource : GenericManager<DCDataContext>
    {

        public ModelSource() : base() { }
        public ModelSource(GenericManager<DCDataContext> manager) : base(manager) { }

        public DCDataContext GetDataContext()
        {
            return (DCDataContext)this._db;
        }

    }
}
