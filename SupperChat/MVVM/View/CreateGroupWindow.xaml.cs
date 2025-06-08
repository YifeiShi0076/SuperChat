using SupperChat.MVVM.ViewModel;
using SupperChat.MVVM.Model;
using System.Windows;

namespace SupperChat.MVVM.View
{
	public partial class CreateGroupWindow : Window
	{
		private readonly CreateGroupViewModel _viewModel;

		public CreateGroupWindow(UserModel currentUser, MainViewModel mainViewModel)
		{
			InitializeComponent();
			_viewModel = new CreateGroupViewModel(currentUser, mainViewModel);
			this.DataContext = _viewModel;
		}

		private void CreateButton_Click(object sender, RoutedEventArgs e)
		{
			_viewModel.CreateGroup();
			this.Close();
		}
	}
}
