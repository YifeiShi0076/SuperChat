using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Service;
using System.Windows;
using System.Windows.Input;

namespace SupperChat.MVVM.ViewModel
{
	public class AddFriendOrGroupViewModel : ObservableObject
	{
		public ICommand SearchFriendCommand { get; }
		public ICommand SearchGroupCommand { get; }
		public Action<UserModel> OnFriendFound { get; set; }
		public Action<GroupModel> OnGroupFound { get; set; }


		private string _searchText;
		public string SearchText
		{
			get => _searchText;
			set { _searchText = value; OnPropertyChanged(); }
		}

		private int _selectedTab;
		public int SelectedTab
		{
			get => _selectedTab;
			set { _selectedTab = value; OnPropertyChanged(); }
		}

		public AddFriendOrGroupViewModel()
		{
			SearchFriendCommand = new RelayCommand(o => SearchFriend());
			SearchGroupCommand = new RelayCommand(o => SearchGroup());
		}

		private async void SearchFriend()
		{
			if (string.IsNullOrWhiteSpace(SearchText))
			{
				MessageBox.Show("请输入用户名！");
				return;
			}

			var user = await ChatService.SearchUserAsync(SearchText.Trim());
			if (user == null)
			{
				MessageBox.Show("未找到该用户！");
				return;
			}

			// 找到了用户，回调通知 MainViewModel 添加到联系人列表
			OnFriendFound?.Invoke(user);

			MessageBox.Show("添加成功！");
		}

		private async void SearchGroup()
		{
			if (string.IsNullOrWhiteSpace(SearchText))
			{
				MessageBox.Show("请输入群聊名！");
				return;
			}

			var group = await ChatService.SearchGroupAsync(SearchText.Trim());
			if (group == null)
			{
				MessageBox.Show("未找到该群聊！");
				return;
			}

			OnGroupFound?.Invoke(group);

			MessageBox.Show("添加成功！");
		}

	}
}
