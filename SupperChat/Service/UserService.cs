using StackExchange.Redis;
using SupperChat.MVVM.Model;
using SupperChat.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SupperChat.Service
{
	public class UserService
	{
		private static IDatabase db => RedisService.Database;

		private static string UserKey(string username) => $"user:{username}";

		// MD5加密
		private static string EncryptPassword(string password)
		{
			using (var md5 = MD5.Create())
			{
				var bytes = Encoding.UTF8.GetBytes(password);
				var hashBytes = md5.ComputeHash(bytes);
				return Convert.ToHexString(hashBytes);
			}
		}

		// 注册用户
		public static async Task<bool> Register(UserModel user)
		{
			var key = UserKey(user.Username);

			if (await db.KeyExistsAsync(key))
			{
				return false; // 用户已存在
			}

			var hashEntries = new HashEntry[]
			{
		new HashEntry("Password", EncryptPassword(user.Password)),
		new HashEntry("Nickname", user.Nickname ?? user.Username),
		new HashEntry("AvatarUrl", user.AvatarUrl ?? ""),
		new HashEntry("Signature", user.Signature ?? "这个人很懒，什么也没有留下。")
			};

			// 正确保存为 Hash 类型
			await db.HashSetAsync(key, hashEntries);

			return true;
		}


		// 登录验证
		public static async Task<bool> Login(string username, string password)
		{
			var key = UserKey(username);

			if (!await db.KeyExistsAsync(key))
				return false; // 用户不存在

			var storedPassword = await db.HashGetAsync(key, "Password");

			return storedPassword == EncryptPassword(password);
		}
		public static async Task<UserModel> GetUserInfo(string username)
		{
			var key = UserKey(username);

			var entries = await db.HashGetAllAsync(key);
			if (entries.Length == 0) return null;

			var user = new UserModel
			{
				Username = username,
				Password = "",  // 不返回密码
				Nickname = entries.FirstOrDefault(x => x.Name == "Nickname").Value,
				AvatarUrl = entries.FirstOrDefault(x => x.Name == "AvatarUrl").Value,
				Signature = entries.FirstOrDefault(x => x.Name == "Signature").Value
			};

			return user;
		}
	}
}
