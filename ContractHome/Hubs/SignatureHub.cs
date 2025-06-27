using ContractHome.Helper;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ContractHome.Hubs
{
    public class SignatureHub : Hub
    {
        private static ConcurrentDictionary<string, string> GroupKeys = new();

        /// <summary>
        /// 連線
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 離線
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if (GroupKeys.TryRemove(Context.ConnectionId, out string groupKey))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupKey);
            }

            await base.OnDisconnectedAsync(ex);
        }

        /// <summary>
        /// 加入群組連線
        /// </summary>
        public async Task AddConnection(string key)
        {
            string groupKey = GetGroupKey(key.DecryptKeyValue().ToString());

            if (groupKey != string.Empty && GroupKeys.TryAdd(Context.ConnectionId, groupKey))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);
            }
        }

        /// <summary>
        /// 傳遞通知
        /// </summary>
        public async Task SendNotice(string key, int result, string resultMessage)
        {
            string groupKey = GetGroupKey(key.DecryptKeyValue().ToString());
            await Clients.Group(groupKey).SendAsync("ReceiveUpdateNotice", groupKey, result, resultMessage);
        }

        private string GetPId()
        {
            return Context.User?.Claims.FirstOrDefault()?.Value ?? string.Empty;
        }

        /// <summary>
        /// 取得群組Key PID_合約ID
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetGroupKey(string key)
        {
            if (GetPId() == string.Empty)
            {
                return string.Empty;
            }
            return GetPId() + "_" + key;
        }
    }
}
