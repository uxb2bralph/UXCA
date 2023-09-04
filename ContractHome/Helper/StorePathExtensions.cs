using ContractHome.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Utility;

namespace ContractHome.Helper
{
    public static class StorePathExtensions
    {
        static StorePathExtensions()
        {
            WebStore.CheckStoredPath();
        }
        public static String WebStore
        {
            get;
            private set;
        } = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), Settings.Default.StoreRoot);

        public static String DailyStorePath(this DateTime date)
        {
            String dailyPath = date.GetDateStylePath();
            Path.Combine(WebStore, dailyPath).CheckStoredPath();
            return Path.Combine(Settings.Default.StoreRoot, dailyPath);
        }

        public static String PrefixStorePath(this String prefix)
        {
            return Path.Combine(WebStore, prefix).CheckStoredPath();
        }


        public static String DailyStorePath(this DateTime date, out string absolutePath)
        {
            String dailyPath = date.GetDateStylePath();
            absolutePath = Path.Combine(WebStore, dailyPath);
            absolutePath.CheckStoredPath();
            return Path.Combine(Settings.Default.StoreRoot, dailyPath);
        }

        public static String DailyStorePath(this DateTime date, String fileName, out string absolutePath)
        {
            String dailyPath = date.GetDateStylePath();
            String path = Path.Combine(WebStore, dailyPath);
            path.CheckStoredPath();
            absolutePath = Path.Combine(path, fileName);
            return Path.Combine(Settings.Default.StoreRoot, dailyPath, fileName);
        }

        public static String StoreTargetPath(this String storePath)
        {
            return Path.Combine(Settings.AppRoot, storePath);
        }

        public static String WebStoreTargetPath(this String storePath)
        {
            return Path.Combine(WebStore, storePath);
        }

        public static String WebStoreTargetDailyPath(this String storePath)
        {
            return Path.Combine(WebStore, storePath, DateTime.Today.GetDateStylePath());
        }
    }
}
