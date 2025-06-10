using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Service;
using System.Windows;

namespace SupperChat.MVVM.ViewModel
{
	public class CreateGroupViewModel : ObservableObject
	{
		private readonly UserModel _currentUser;
		private readonly MainViewModel _mainViewModel;

		private string _groupName;
		public string GroupName
		{
			get => _groupName;
			set { _groupName = value; OnPropertyChanged(); }
		}

		public CreateGroupViewModel(UserModel currentUser, MainViewModel mainViewModel)
		{
			_currentUser = currentUser;
			_mainViewModel = mainViewModel;
		}

		public async void CreateGroup()
		{
			if (string.IsNullOrWhiteSpace(GroupName))
			{
				MessageBox.Show("群聊名称不能为空！");
				return;
			}

			bool success = await ChatService.CreateGroupAsync(GroupName.Trim(), _currentUser);

			if (success)
			{
				MessageBox.Show("创建群聊成功！");
				_mainViewModel.AddGroup(new GroupModel
				{
					GroupName = GroupName.Trim(),
					AvatarUrl = "./Icons/group_default.png" // 可以自定义默认群头像
				});
			}
			else
			{
				MessageBox.Show("群聊名称已存在！");
			}
		}
	}
}
