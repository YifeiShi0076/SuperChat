using SupperChat.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SupperChat.MVVM.View
{
	/// <summary>
	/// LoginWindow.xaml 的交互逻辑
	/// </summary>
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
			DataContext = new LoginViewModel();

		}
		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (DataContext is LoginViewModel vm)
			{
				vm.SetPassword(((PasswordBox)sender).Password);
			}
		}

		private void SwitchRegisterLogin(object sender, MouseButtonEventArgs e)
		{
			if (DataContext is LoginViewModel vm)
			{
				vm.SwitchRegistering();
			}
		}


	}
}
