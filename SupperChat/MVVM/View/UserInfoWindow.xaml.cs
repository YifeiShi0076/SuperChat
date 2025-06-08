using System.Windows;
using SupperChat.MVVM.Model;
using SupperChat.MVVM.View;

namespace SupperChat.Views
{
	public partial class UserInfoWindow : Window
	{
		public UserInfoWindow(UserModel user)
		{
			InitializeComponent();
			this.DataContext = user; // 直接绑定用户信息
		}

		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			// 回到登录窗口
			var loginWindow = new LoginWindow(); // 假设你的登录窗口叫 LoginWindow
			loginWindow.Show();

			// 关闭所有其他窗口
			foreach (Window window in Application.Current.Windows)
			{
				if (window != loginWindow)
				{
					window.Close();
				}
			}
		}
	}
}
