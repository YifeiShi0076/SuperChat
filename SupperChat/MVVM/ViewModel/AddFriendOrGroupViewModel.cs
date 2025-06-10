using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Service;
using System;
using System.Windows;
using System.Windows.Input;

namespace SupperChat.MVVM.ViewModel
{
	public class AddFriendOrGroupViewModel : ObservableObject
	{
		private readonly MainViewModel _mainViewModel;

		public UserModel CurrentUser { get; }

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

		public AddFriendOrGroupViewModel(UserModel currentUser, MainViewModel mainViewModel)
		{
			CurrentUser = currentUser;
			_mainViewModel = mainViewModel;

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

			var targetUsername = SearchText.Trim();

			var user = await ChatService.SearchUserAsync(targetUsername);
			if (user == null)
			{
				MessageBox.Show("未找到该用户！");
				return;
			}

			// 添加到 Redis 好友列表中（双向关系）
			bool success = await ChatService.AddFriendAsync(CurrentUser.Username, targetUsername);
			if (success)
			{
				// 订阅私聊频道
				SubscribeToPrivateChannel(CurrentUser.Username, targetUsername);

				// 通知主视图模型添加联系人
				OnFriendFound?.Invoke(user);

				MessageBox.Show("添加成功！");
			}
			else
			{
				MessageBox.Show("添加失败，可能已存在该好友！");
			}
		}

		private void SubscribeToPrivateChannel(string self, string other)
		{
			var channel1 = $"chat:{self}_{other}";
			var channel2 = $"chat:{other}_{self}";

			RedisService.Subscriber.Subscribe(channel1, (channel, message) =>
			{
				_mainViewModel.HandleIncomingMessage(channel, message, other);
			});

			RedisService.Subscriber.Subscribe(channel2, (channel, message) =>
			{
				_mainViewModel.HandleIncomingMessage(channel, message, other);
			});
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
