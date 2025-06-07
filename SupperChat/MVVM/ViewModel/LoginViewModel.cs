using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Service;
using SupperChat.Services;

namespace SupperChat.MVVM.ViewModel
{
	class LoginViewModel : ObservableObject
	{
		private bool _isRegistering;
		public bool IsRegistering
		{
			get => _isRegistering;
			set { _isRegistering = value; OnPropertyChanged(); 
				OnPropertyChanged(nameof(TitleText));
				OnPropertyChanged(nameof(ButtonText));
				OnPropertyChanged(nameof(SwitchTipText)); 
				OnPropertyChanged(nameof(SwitchLinkText)); }
		}

		public string TitleText => IsRegistering ? "注册" : "登录";
		public string ButtonText => IsRegistering ? "注册" : "登录";
		public string SwitchTipText => IsRegistering ? "已有账号？" : "没有账号？";
		public string SwitchLinkText => IsRegistering ? "马上登录" : "马上注册";

		private string _username;
		private string _password;
		private string _nickname;
		private string _signature;

		public string Username
		{
			get => _username;
			set { _username = value; OnPropertyChanged(); }
		}

		public string Password
		{
			get => _password;
			set { _password = value; OnPropertyChanged(); }
		}

		public string Nickname
		{
			get => _nickname;
			set { _nickname = value; OnPropertyChanged(); }
		}

		public string Signature
		{
			get => _signature;
			set { _signature = value; OnPropertyChanged(); }
		}

		public ICommand SubmitCommand { get; }

		public LoginViewModel()
		{
			SubmitCommand = new RelayCommand(async o => await Submit());
			IsRegistering = false;
		}

		public void SetPassword(string pwd)
		{
			Password = pwd;
		}

		public void SwitchRegistering()
		{
			IsRegistering = !IsRegistering;
		}

		private async Task Submit()
		{
			if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
			{
				MessageBox.Show("用户名和密码不能为空！");
				return;
			}

			if (IsRegistering)
			{
				var user = new UserModel
				{
					Username = Username,
					Password = Password,
					Nickname = Nickname,
					Signature = Signature
				};

				if (await UserService.Register(user))
				{
					MessageBox.Show("注册成功！");
					SwitchRegistering(); // 注册成功自动切回登录
				}
				else
				{
					MessageBox.Show("用户名已存在！");
				}
			}
			else
			{
				if (await UserService.Login(Username, Password))
				{
					MessageBox.Show("登录成功！");

					// 打开主聊天窗口
					var mainWindow = new MainWindow();
					mainWindow.Show();

					// 关闭登录窗口
					Application.Current.Windows[0]?.Close();
				}
				else
				{
					MessageBox.Show("用户名或密码错误！");
				}
			}
		}
	}
}
