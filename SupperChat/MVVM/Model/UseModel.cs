namespace SupperChat.MVVM.Model
{
	public class UserModel
	{
		public string Username { get; set; }
		public string Password { get; set; }  // 密码会加密存储
		public string Nickname { get; set; }
		public string AvatarUrl { get; set; }
		public string Signature { get; set; }
	}
}
