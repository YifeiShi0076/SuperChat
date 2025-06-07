using SupperChat.MVVM.View;
using SupperChat.MVVM.ViewModel;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SupperChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var connectWindow = new ConnectWindow();
			connectWindow.DataContext = new ConnectViewModel();
			connectWindow.Show();
		}

	}

}
