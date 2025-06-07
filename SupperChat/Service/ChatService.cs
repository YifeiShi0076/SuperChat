using StackExchange.Redis;
using SupperChat.MVVM.Model;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SupperChat.Services
{
    public static class ChatService
    {
        private static readonly IDatabase db = RedisService.Database;
        private static readonly ISubscriber subscriber = RedisService.Subscriber;

        public static async Task SendMessageAsync(string target, MessageModel message)
        {
            string json = JsonSerializer.Serialize(message);
            await subscriber.PublishAsync($"chat:{target}", json);

            // 存聊天记录到Redis List
            await db.ListRightPushAsync($"history:{target}", json);
        }

        public static async Task<List<MessageModel>> GetChatHistory(string user, string target)
        {
            var messages = new List<MessageModel>();
            var redisMessages = await db.ListRangeAsync($"history:{target}");
            foreach (var redisMessage in redisMessages)
            {
                var msg = JsonSerializer.Deserialize<MessageModel>(redisMessage);
                if (msg != null)
                {
                    messages.Add(msg);
                }
            }
            return messages;
        }

        public static void SubscribeToChannel(string channel, Action<MessageModel> messageReceivedCallback)
        {
            subscriber.Subscribe($"chat:{channel}", (ch, message) =>
            {
                var msg = JsonSerializer.Deserialize<MessageModel>(message);
                if (msg != null)
                {
                    messageReceivedCallback(msg);
                }
            });
        }

        public static async Task<List<ContactModel>> GetContactsAsync(string username)
        {
            // 可以扩展存好友列表，如存个 Set: contacts:username
            return new List<ContactModel>();  // 初次登录无联系人
        }
    }
}
