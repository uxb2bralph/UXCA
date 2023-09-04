using WebHome.Models.ViewModel;

namespace WebHome.Helper.PKI
{
    public static class PKISignatureStore
    {
        private static Dictionary<String, SignatureViewModel> _Store;
        static PKISignatureStore() 
        {
            _Store = new Dictionary<String, SignatureViewModel>();
        }

        public static void PushSignature(SignatureViewModel item)
        {
            lock (_Store)
            {
                if (item.KeyID != null)
                {
                    if (!_Store.ContainsKey(item.KeyID))
                    {
                        _Store.Add(item.KeyID, item);
                    }
                    else
                    {
                        _Store[item.KeyID] = item;
                    }

                }
            }
        }

        public static SignatureViewModel? PopSignature(String keyID)
        {
            lock (_Store)
            {
                if (_Store.ContainsKey(keyID))
                {
                    var item = _Store[keyID];
                    _Store.Remove(keyID);
                    return item;
                }

                return null;
            }
        }

        public static KeyValuePair<String, SignatureViewModel>[] PeekAll()
        {
            return _Store.ToArray();
        }
    }
}
