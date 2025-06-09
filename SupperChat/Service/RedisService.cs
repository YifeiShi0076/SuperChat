using StackExchange.Redis;
using System;

namespace SupperChat.Service
{
	public class RedisService
	{
		private static ConnectionMultiplexer _redis;
		public static IDatabase Database => _redis?.GetDatabase();
		public static ISubscriber Subscriber => _redis?.GetSubscriber();

		public static bool Connect(string host, int port)
		{
			try
			{
				var configurationOptions = new ConfigurationOptions
				{
					EndPoints = { { host, port } },
					AbortOnConnectFail = false
				};
				_redis = ConnectionMultiplexer.Connect(configurationOptions);
				return _redis.IsConnected;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Redis连接失败：" + ex.Message);
				return false;
			}
		}

		public static void Disconnect()
		{
			_redis?.Dispose();
			_redis = null;
		}
	}
}
