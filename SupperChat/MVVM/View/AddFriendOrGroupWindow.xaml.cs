using SupperChat.MVVM.Model;
using SupperChat.MVVM.ViewModel;
using System.Windows;

namespace SupperChat.MVVM.View
{
	public partial class AddFriendOrGroupWindow : Window
	{
		public AddFriendOrGroupWindow(UserModel currentUser, MainViewModel mainViewModel)
		{
			InitializeComponent();

			var vm = new AddFriendOrGroupViewModel(currentUser, mainViewModel);
			this.DataContext = vm;

			vm.OnFriendFound = user =>
			{
				bool success = mainViewModel.AddFriend(user);
				if (success)
				{
					MessageBox.Show("添加好友成功！");
					this.Close();
				}
				else
				{
					MessageBox.Show("该好友已存在！");
				}
			};

			vm.OnGroupFound = group =>
			{
				bool success = mainViewModel.AddGroup(group);
				if (success)
				{
					MessageBox.Show("添加群聊成功！");
					this.Close();
				}
				else
				{
					MessageBox.Show("该群聊已存在！");
				}
			};
		}
	}
}
