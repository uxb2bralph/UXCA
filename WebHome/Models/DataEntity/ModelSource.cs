using CommonLib.DataAccess;

namespace WebHome.Models.DataEntity
{
    public class ModelSource<TEntity> : GenericManager<UXCADataContext, TEntity>
        where TEntity : class, new()
    {
        protected IQueryable<TEntity>? _items;

        public ModelSource() : base() { }
        public ModelSource(GenericManager<UXCADataContext> manager) : base(manager) { }

        public IQueryable<TEntity> Items
        {
            get
            {
                if (_items == null)
                    _items = this.EntityList;
                return _items;
            }
            set
            {
                _items = value;
            }
        }

        public UXCADataContext GetDataContext()
        {
            return (UXCADataContext)this._db;
        }

    }
}
