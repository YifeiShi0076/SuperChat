using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using StackExchange.Redis;
using SupperChat.MVVM.View;
using SupperChat.Service;

namespace SupperChat.MVVM.ViewModel
{
	public class MainViewModel : ObservableObject
	{
		private readonly UserModel _currentUser;

		public ObservableCollection<ContactModel> Contacts { get; set; }
		public RelayCommand SendCommand { get; set; }
		public RelayCommand AddContactCommand { get; set; }

		private ContactModel _selectedContact;
		public ContactModel		SelectedContact
		{
			get => _selectedContact;
			set
			{
				if (_selectedContact != value)
				{
					_selectedContact = value;
					OnPropertyChanged();

					if (_selectedContact != null)
					{
						_selectedContact.UnreadCount = 0; // 选中后未读清零
					}
				}
			}
		}

		private string _message;
		public string Message
		{
			get => _message;
			set { _message = value; OnPropertyChanged(); }
		}

		private ObservableCollection<MessageModel> _messages;
		public ObservableCollection<MessageModel> Messages
		{
			get => _messages;
			set { _messages = value; OnPropertyChanged(); }
		}

		


		public RelayCommand OpenAddContactWindowCommand { get; }
		public RelayCommand OpenCreateGroupWindowCommand { get; }
		public MainViewModel(UserModel currentUser)
		{
			_currentUser = currentUser;
			Contacts = new ObservableCollection<ContactModel>();
			Messages = new ObservableCollection<MessageModel>();

			SendCommand = new RelayCommand(async o => await SendMessage()); // 异步发送消息
			AddContactCommand = new RelayCommand(o => AddContactOrGroup()); // 添加联系人或群聊
			OpenAddContactWindowCommand = new RelayCommand(o => OpenAddContactWindow()); // 打开添加联系人窗口
			OpenCreateGroupWindowCommand = new RelayCommand(o => OpenCreateGroupWindow()); // 打开创建群聊窗口

			LoadContacts();
		}

		private void OpenCreateGroupWindow()
		{
			var createGroupWindow = new CreateGroupWindow(_currentUser, this);
			createGroupWindow.ShowDialog();
		}

		private void OpenAddContactWindow()
		{
			var addContactWindow = new AddFriendOrGroupWindow(_currentUser, this); // 传递当前VM
			addContactWindow.ShowDialog();
		}

		public bool AddFriend(UserModel user)
		{
			bool exists = Contacts.Any(c => c.Username == user.Username);
			if (exists)
				return false;

			var contact = new ContactModel
			{
				Username = user.Username,
				ImageSource = user.AvatarUrl,
				Messages = new ObservableCollection<MessageModel>(),
				IsGroup = false
			};

			Contacts.Add(contact);

			LoadPrivateHistoryAsync(contact);

			return true;
		}



		public bool AddGroup(GroupModel group)
		{
			bool exists = Contacts.Any(c => c.Username == group.GroupName);
			if (exists)
				return false;

			var contact = new ContactModel
			{
				Username = group.GroupName,
				ImageSource = group.AvatarUrl,
				Messages = new ObservableCollection<MessageModel>(),
				IsGroup = true
			};

			Contacts.Add(contact);

			// 🟢 加载群聊历史消息（异步，单独处理）
			LoadGroupHistoryAsync(contact);

			// 🟢 订阅群聊频道
			ChatService.SubscribeGroup(group.GroupName, message =>
			{
				if (message.SenderUsername == _currentUser.Username)
					return;

				var targetContact = Contacts.FirstOrDefault(c => c.Username == group.GroupName);
				if (targetContact != null)
				{
					targetContact.Messages.Add(message);

					if (SelectedContact != targetContact)  // 不是当前打开的聊天窗口
					{
						targetContact.UnreadCount++;
					}
				}
			});


			return true;
		}

		private async void LoadGroupHistoryAsync(ContactModel contact)
		{
			await LoadMoreMessagesAsync(contact); // 加载第一页
		}


		private async void LoadContacts()
		{
			var contacts = await ChatService.GetContactsAsync(_currentUser.Username);
			foreach (var contact in contacts)
			{
				Contacts.Add(contact);
			}
		}

		private async void LoadMessagesForSelectedContact()
		{
			if (SelectedContact != null)
			{
				Messages.Clear();
				var chatHistory = await ChatService.GetChatHistory(_currentUser.Username, SelectedContact.Username);
				foreach (var msg in chatHistory)
				{
					Messages.Add(msg);
				}

				// 开始订阅该联系人的频道
				ChatService.SubscribeToChannel(SelectedContact.Username, OnMessageReceived);
			}
		}

		private void OnMessageReceived(MessageModel message)
		{
			App.Current.Dispatcher.Invoke(() => Messages.Add(message));
		}

		private async Task SendMessage()
		{
			if (string.IsNullOrEmpty(Message) || SelectedContact == null) return;

			var newMessage = new MessageModel
			{
				Username = _currentUser.Nickname,
				SenderUsername = _currentUser.Username, // 🟢 加上发信账号
				UsernameColor = "#409aff",
				ImageSource = _currentUser.AvatarUrl,
				Message = Message,
				Time = DateTime.Now,
				IsNativeOrigin = true
			};

			// 简单判断：如果是群聊，发群消息，否则单聊
			if (SelectedContact.IsGroup)
			{
				await ChatService.SendGroupMessageAsync(SelectedContact.Username, newMessage);
			}
			else
			{
				await ChatService.SendPrivateMessageAsync(_currentUser.Username, SelectedContact.Username, newMessage);
			}


			SelectedContact.Messages.Add(newMessage);
			Message = string.Empty;
			await ChatService.AddSessionAsync(_currentUser.Username, SelectedContact.Username);

		}

		private async void LoadPrivateHistoryAsync(ContactModel contact)
		{
			await LoadMoreMessagesAsync(contact); // 加载第一页
		}


		public async Task LoadMoreMessagesAsync(ContactModel contact)
		{
			if (!contact.HasMoreMessages) return;

			List<MessageModel> history;

			if (contact.IsGroup)
			{
				history = await ChatService.GetGroupHistoryPageAsync(contact.Username, contact.CurrentPage, contact.PageSize);
			}
			else
			{
				history = await ChatService.GetPrivateHistoryPageAsync(_currentUser.Username, contact.Username, contact.CurrentPage, contact.PageSize);
			}

			if (history.Count == 0)
			{
				contact.HasMoreMessages = false;
				return;
			}

			// 插入到聊天记录最前面
			for (int i = history.Count - 1; i >= 0; i--)
			{
				contact.Messages.Insert(0, history[i]);
			}

			contact.CurrentPage++;
		}

		public async Task LoadSessionsAsync()
		{
			Contacts.Clear();

			var sessions = await ChatService.GetSessionsAsync(_currentUser.Username);

			foreach (var session in sessions)
			{
				if (await ChatService.IsGroupAsync(session)) // 判别是不是群聊
				{
					var groupInfo = await ChatService.GetGroupInfoAsync(session);
					if (groupInfo != null)
					{
						AddGroup(groupInfo);
					}
				}
				else
				{
					var userInfo = await ChatService.GetUserInfoAsync(session);
					if (userInfo != null)
					{
						AddFriend(userInfo);
					}
				}
			}
		}

		private void AddContactOrGroup()
		{
			// 这里可以弹窗输入新联系人名字，或者新建群聊
		}

		public UserModel CurrentUser => _currentUser;
	}
}
