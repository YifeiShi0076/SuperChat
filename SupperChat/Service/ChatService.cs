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

		public static async Task<UserModel> SearchUserAsync(string username)
		{
			var key = $"user:{username}";

			if (!await db.KeyExistsAsync(key))
				return null; // 用户不存在

			var entries = await db.HashGetAllAsync(key);
			if (entries.Length == 0) return null;

			var user = new UserModel
			{
				Username = username,
				Nickname = entries.FirstOrDefault(x => x.Name == "Nickname").Value,
				AvatarUrl = entries.FirstOrDefault(x => x.Name == "AvatarUrl").Value,
				Signature = entries.FirstOrDefault(x => x.Name == "Signature").Value
			};

			return user;
		}

		public static async Task<GroupModel> SearchGroupAsync(string groupName)
		{
			var key = $"group:{groupName}";

			if (!await db.KeyExistsAsync(key))
				return null; // 群不存在

			var entries = await db.HashGetAllAsync(key);
			if (entries.Length == 0) return null;

			var group = new GroupModel
			{
				GroupName = groupName,
				AvatarUrl = entries.FirstOrDefault(x => x.Name == "AvatarUrl").Value
			};

			return group;
		}

		public static async Task<bool> CreateGroupAsync(string groupName, UserModel creator)
		{
			var key = $"group:{groupName}";

			if (await db.KeyExistsAsync(key))
				return false; // 群名已存在

			// 创建群信息
			var hashEntries = new HashEntry[]
			{
				new HashEntry("GroupName", groupName),
				new HashEntry("AvatarUrl", "/Icons/group_default.png"),
				new HashEntry("Creator", creator.Username)
			};
			await db.HashSetAsync(key, hashEntries);

			// 把当前用户加入群成员列表
			await db.SetAddAsync($"group_members:{groupName}", creator.Username);

			return true;
		}

		private static string GetGroupChannel(string groupName)
		{
			return $"group_channel:{groupName}";
		}

		public static async Task SendGroupMessageAsync(string groupName, MessageModel message)
		{
			var json = JsonSerializer.Serialize(message);
			await subscriber.PublishAsync(GetGroupChannel(groupName), json);

			// 保存聊天记录
			await db.ListRightPushAsync($"group_history:{groupName}", json);
		}


		public static void SubscribeGroup(string groupName, Action<MessageModel> messageReceivedCallback)
		{
			subscriber.Subscribe(GetGroupChannel(groupName), (ch, message) =>
			{
				var msg = JsonSerializer.Deserialize<MessageModel>(message);
				if (msg != null)
				{
					// 触发回调到界面
					App.Current.Dispatcher.Invoke(() => messageReceivedCallback(msg));
				}
			});
		}


		public static async Task<List<MessageModel>> GetGroupHistoryAsync(string groupName, int maxCount = 100)
		{
			var messages = new List<MessageModel>();

			var redisMessages = await db.ListRangeAsync($"group_history:{groupName}", -maxCount, -1);

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

		private static string GetPrivateHistoryKey(string userA, string userB)
		{
			var users = new List<string> { userA, userB };
			users.Sort(); // 字母顺序，保证一致性
			return $"private_history:{users[0]}:{users[1]}";
		}

		public static async Task<List<MessageModel>> GetPrivateHistoryAsync(string userA, string userB, int maxCount = 100)
		{
			var key = GetPrivateHistoryKey(userA, userB);

			var messages = new List<MessageModel>();

			var redisMessages = await db.ListRangeAsync(key, -maxCount, -1);

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

		public static async Task<List<MessageModel>> GetGroupHistoryPageAsync(string groupName, int page, int pageSize)
		{
			var messages = new List<MessageModel>();
			var start = -page * pageSize;
			var end = -(page - 1) * pageSize - 1;

			var redisMessages = await db.ListRangeAsync($"group_history:{groupName}", start, end);

			// 注意：Redis List 是从右插入的，聊天记录顺序是反的，需倒序
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

		public static async Task<List<MessageModel>> GetPrivateHistoryPageAsync(string userA, string userB, int page, int pageSize)
		{
			var key = GetPrivateHistoryKey(userA, userB);

			var messages = new List<MessageModel>();
			var start = -page * pageSize;
			var end = -(page - 1) * pageSize - 1;

			var redisMessages = await db.ListRangeAsync(key, start, end);

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


		public static async Task SendPrivateMessageAsync(string fromUser, string toUser, MessageModel message)
		{
			var json = JsonSerializer.Serialize(message);
			var key = GetPrivateHistoryKey(fromUser, toUser);

			await db.ListRightPushAsync(key, json);
		}

		// 添加好友
		public static async Task AddFriendAsync(string username, string friendUsername)
		{
			await db.SetAddAsync($"friends:{username}", friendUsername);
		}

		// 添加群聊
		public static async Task AddGroupAsync(string username, string groupName)
		{
			await db.SetAddAsync($"groups:{username}", groupName);
		}

		// 获取好友列表
		public static async Task<List<string>> GetFriendsAsync(string username)
		{
			var result = await db.SetMembersAsync($"friends:{username}");
			return result.Select(x => x.ToString()).ToList();
		}

		// 获取群聊列表
		public static async Task<List<string>> GetGroupsAsync(string username)
		{
			var result = await db.SetMembersAsync($"groups:{username}");
			return result.Select(x => x.ToString()).ToList();
		}

		public static async Task<List<string>> GetSessionsAsync(string username)
		{
			var result = await db.ListRangeAsync($"sessions:{username}", 0, -1);
			return result.Select(x => x.ToString()).ToList();
		}

		public static async Task<bool> IsGroupAsync(string name)
		{
			return await db.KeyExistsAsync($"group:{name}");
		}


		public static async Task<UserModel> GetUserInfoAsync(string username)
		{
			var key = $"user:{username}";  // 用户信息的 Redis key
			var hashEntries = await db.HashGetAllAsync(key);

			if (hashEntries.Length == 0)
				return null;  // 用户不存在

			var userInfo = new UserModel
			{
				Username = username,
				Nickname = hashEntries.FirstOrDefault(x => x.Name == "Nickname").Value,
				AvatarUrl = hashEntries.FirstOrDefault(x => x.Name == "AvatarUrl").Value,
				Signature = hashEntries.FirstOrDefault(x => x.Name == "Signature").Value
			};

			return userInfo;
		}


		public static async Task<GroupModel> GetGroupInfoAsync(string groupName)
		{
			var key = $"group:{groupName}";  // 群聊信息的 Redis key
			var hashEntries = await db.HashGetAllAsync(key);

			if (hashEntries.Length == 0)
				return null;  // 群聊不存在

			var groupInfo = new GroupModel
			{
				GroupName = groupName,
				AvatarUrl = hashEntries.FirstOrDefault(x => x.Name == "AvatarUrl").Value
				// 后续可以扩展更多，比如群公告、群人数
			};

			return groupInfo;
		}

		public static async Task AddSessionAsync(string username, string target)
		{
			// 先移除已存在的（防止重复）
			await db.ListRemoveAsync($"sessions:{username}", target);
			// 插到最前面
			await db.ListLeftPushAsync($"sessions:{username}", target);
		}

	}
}
