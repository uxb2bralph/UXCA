using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonLib.Utility.Properties
{
    public partial class AppSettings : IDisposable
    {
        public static String AppRoot
        {
            get;
            private set;
        } = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

        static JObject _Settings;

        public AppSettings()
        {

        }

        protected void Save()
        {
            String fileName = "App.settings.json";
            String filePath = Path.Combine(AppRoot, "App_Data", fileName);
            String propertyName = typeof(AppSettings).Namespace;
            _Settings[propertyName] = JObject.FromObject(this);
            File.WriteAllText(filePath, _Settings.ToString());
        }

        protected static T Initialize<T>(String propertyName)
            where T : AppSettings, new()
        {
            lock(typeof(AppSettings))
            {
                T currentSettings;
                //String fileName = $"{Assembly.GetExecutingAssembly().GetName()}.settings.json";
                String fileName = "App.settings.json";
                String filePath = Path.Combine(AppRoot, "App_Data", fileName);
                if (File.Exists(filePath))
                {
                    _Settings = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(filePath));
                }
                else
                {
                    _Settings = new JObject();
                }

                //String propertyName = Assembly.GetExecutingAssembly().GetName().Name;
                if (_Settings[propertyName] != null)
                {
                    currentSettings = _Settings[propertyName].ToObject<T>();
                }
                else
                {
                    currentSettings = new T();
                    _Settings[propertyName] = JObject.FromObject(currentSettings);
                }

                Path.GetDirectoryName(filePath).CheckStoredPath();
                File.WriteAllText(filePath, _Settings.ToString());
                return currentSettings;
            }
        }

        public void Dispose()
        {
            dispose(true);
        }

        bool _disposed;
        protected void dispose(bool disposing)
        {
            if (!_disposed)
            {
                this.Save();
            }
            _disposed = true;
        }

        ~AppSettings()
        {
            dispose(false);
        }

    }
}
