using CommonLib.Core.Utility;
using CommonLib.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonLib.Core.Helper
{
    public static class PersistenceExtensions
    {
        public static void Push(this Object data, String objectName = null, String storedPath = null)
        {
            if (data == null)
            {
                return;
            }

            storedPath = Path.Combine(storedPath ?? FileLogger.Logger.LogPath, data.GetType().Name).CheckStoredPath();
            String fileName = Path.Combine(storedPath, objectName ?? Guid.NewGuid().ToString());
            if (File.Exists(fileName))
            {
                return;
            }

            File.WriteAllText(fileName, JsonConvert.SerializeObject(data));

        }

        public static T Popup<T>(this String intentName, String storedPath = null, bool popupIfAny = true)
        {
            String stored = Path.Combine(storedPath ?? FileLogger.Logger.LogPath, typeof(T).Name).CheckStoredPath();
            String objectContent = null;

            String popupContent(String objectPath)
            {
                lock (typeof(PersistenceExtensions))
                {
                    String content = null;
                    if (File.Exists(objectPath))
                    {
                        content = File.ReadAllText(objectPath);
                        File.Delete(objectPath);
                    }
                    return content;
                }
            }

            intentName = intentName.GetEfficientString();
            if (intentName != null)
            {
                intentName = Path.Combine(stored, intentName);
                objectContent = popupContent(intentName);
            }

            if (objectContent != null)
                return JsonConvert.DeserializeObject<T>(objectContent);

            if (!popupIfAny)
                return default;

            foreach (var fileName in Directory.EnumerateFiles(stored))
            {
                objectContent = popupContent(fileName);
            }

            if (objectContent != null)
                return JsonConvert.DeserializeObject<T>(objectContent);

            return default;

        }
    }

}
