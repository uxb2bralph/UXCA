namespace WebHome.Helper
{
    public static class ExtensionMethods
    {
        public static async Task<byte[]> GetRequestBytesAsync(this HttpRequest Request)
        {
            Request.Body.Position = 0;
            using (MemoryStream fs = new MemoryStream())
            {
                await Request.Body.CopyToAsync(fs);
                Request.Body.Position = 0;
                return fs.ToArray();
            }
        }

        public static async Task<String> GetRequestBodyAsync(this HttpRequest Request)
        {
            Request.Body.Position = 0;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                var data = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                return data;
            }
        }
    }
}
