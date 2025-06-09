using System.Windows;
using System.Windows.Input;
using SupperChat.Core;
using SupperChat.MVVM.View;
using SupperChat.Service;

namespace SupperChat.MVVM.ViewModel
{
	class ConnectViewModel : ObservableObject
	{
		private string _host;
		public string Host
		{
			get => _host;
			set { _host = value; OnPropertyChanged(); }
		}

		private string _port;
		public string Port
		{
			get => _port;
			set { _port = value; OnPropertyChanged(); }
		}

		public ICommand ConnectCommand { get; }

		public ConnectViewModel()
		{
			ConnectCommand = new RelayCommand(ConnectToRedis);
		}

		private void ConnectToRedis(object obj)
		{
			if (!int.TryParse(Port, out int port))
			{
				MessageBox.Show("端口格式错误，请输入有效的端口号！");
				return;
			}

			if (RedisService.Connect(Host, port))
			{
				MessageBox.Show("连接成功！");

				// 打开登录窗口
				var loginWindow = new LoginWindow();
				loginWindow.Show();

				// 关闭当前连接窗口
				Application.Current.Windows[0]?.Close();
			}
			else
			{
				MessageBox.Show("连接失败，请检查地址和端口！");
			}
		}
	}
}
