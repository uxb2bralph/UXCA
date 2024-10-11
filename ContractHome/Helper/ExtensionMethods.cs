using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System.Text;
using static ContractHome.Models.Helper.ContractServices;


namespace ContractHome.Helper
{
    public static partial class ExtensionMethods
    {
        public static byte[] DecryptKey(this QueryViewModel viewModel)
        {
            return AppResource.Instance.DecryptSalted(Convert.FromBase64String(viewModel.KeyID));
        }

        public static byte[] DecryptKey(this QueryViewModel viewModel,out long ticks)
        {
            return AppResource.Instance.DecryptSalted(Convert.FromBase64String(viewModel.KeyID),out ticks);
        }

        public static void EncryptKey(this QueryViewModel viewModel,byte[] data)
        {
            viewModel.KeyID = Convert.ToBase64String(AppResource.Instance.EncryptSalted(data));
        }

        public static String EncryptKey(this byte[] data)
        {
            return Convert.ToBase64String(AppResource.Instance.EncryptSalted(data));
        }

        public static String EncryptData(this String data)
        {
            return Encoding.Unicode.GetBytes(data).EncryptKey();
        }

        public static String DecryptData(this String data)
        {
            return Encoding.Unicode.GetString(AppResource.Instance.DecryptSalted(Convert.FromBase64String(data)));
        }

        public static String DecryptData(this String data,out long ticks)
        {
            return Encoding.Unicode.GetString(AppResource.Instance.DecryptSalted(Convert.FromBase64String(data), out ticks));
        }

        public static int DecryptKeyValue(this QueryViewModel viewModel)
        {
            return BitConverter.ToInt32(viewModel.DecryptKey(), 0);
        }

        public static int DecryptKeyValue(this QueryViewModel viewModel,out long ticks)
        {
            return BitConverter.ToInt32(viewModel.DecryptKey(out ticks), 0);
        }

        public static int DecryptKeyValue(this string? keyValue)
        {
            if (string.IsNullOrEmpty(keyValue))
                return 0;

            return BitConverter.ToInt32(AppResource.Instance.DecryptSalted(Convert.FromBase64String(keyValue)), 0);
        }

        public static String EncryptKey(this int keyID)
        {
            return BitConverter.GetBytes(keyID).EncryptKey();
        }

        public static string EncryptKey(this int? keyID)
        {
            int id = keyID??0;
            return BitConverter.GetBytes(id).EncryptKey();
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
                action(item);
        }

        public static string ReportDateTimeString(this DateTime date)
        {
            return $"{date:yyyy/MM/dd HH:mm:ss}";
        }

        public static DateTime ConvertToDateTime(this string dateTimeString, string dateTimeFormat)
        {
            DateTime outputDateTimeValue;
            DateTime.TryParseExact(dateTimeString, dateTimeFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out outputDateTimeValue);
            return outputDateTimeValue;
        }

        public static DateTime StartOfDay(this DateTime theDate)
        {
            return theDate.Date;
        }

        public static DateTime EndOfDay(this DateTime theDate)
        {
            return theDate.Date.AddDays(1).AddTicks(-1);
        }

        public static DigitalSignCerts DigitalSignBy(this Organization organization)
        {
            if (organization.CHT_Token != null)
            {
                return DigitalSignCerts.Enterprise;
            }
            else if (organization.OrganizationToken?.PKCS12 != null)
            {
                return DigitalSignCerts.UXB2B;
            }
            else
            {
                return DigitalSignCerts.Exchange;
            }
        }

        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
