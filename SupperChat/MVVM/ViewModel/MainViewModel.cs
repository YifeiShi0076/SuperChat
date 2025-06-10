using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Service;
using System.Collections.ObjectModel;
using SupperChat.MVVM.View;
using StackExchange.Redis;
using System.Windows;
using Newtonsoft.Json;

namespace SupperChat.MVVM.ViewModel
{
	public class MainViewModel : ObservableObject
	{
		private UserModel _currentUser;
		public UserModel CurrentUser
		{
			get => _currentUser;
			set { _currentUser = value; OnPropertyChanged(); }
		}

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
						LoadMessagesForSelectedContact();
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

			LoadFriends();
			// LoadContacts();
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
			bool exists = Contacts.Any(c => c.Contactname == user.Username);
			if (exists)
				return false;

			var contact = new ContactModel
			{
				Contactname = user.Username,
				Nickname = user.Nickname,
				ImageSource = user.AvatarUrl,
				Messages = new ObservableCollection<MessageModel>(),
				IsGroup = false
			};

			Contacts.Add(contact);

			LoadPrivateHistoryAsync(contact);

			// 添加订阅逻辑
			SubscribeToPrivateChannel(_currentUser.Username, user.Username);

			return true;
		}

		private void SubscribeToPrivateChannel(string self, string other)
		{
			var channel1 = $"chat:{self}_{other}";
			var channel2 = $"chat:{other}_{self}";

			RedisService.Subscriber.Subscribe(channel1, (channel, message) =>
			{
				HandleIncomingMessage(channel, message, other);
			});

			RedisService.Subscriber.Subscribe(channel2, (channel, message) =>
			{
				HandleIncomingMessage(channel, message, other);
			});
		}

		public void HandleIncomingMessage(RedisChannel channel, RedisValue message, string senderUsername)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				var msg = JsonConvert.DeserializeObject<MessageModel>(message);

				if (msg.SenderUsername == _currentUser.Username)
					return;                 // 自己的消息不再处理

				var contact = Contacts.FirstOrDefault(c => c.Contactname == senderUsername);
				if (contact == null)
				{
					// 如果该联系人不在列表中，说明是首次消息，添加
					contact = new ContactModel
					{
						Contactname = senderUsername,
						ImageSource = msg.ImageSource, // 可以根据情况优化
						Messages = new ObservableCollection<MessageModel>(),
						IsGroup = false
					};
					Contacts.Add(contact);
				}

				contact.Messages.Add(msg);

				// 未读消息计数等逻辑可扩展
			});
		}

		public bool AddGroup(GroupModel group)
		{
			bool exists = Contacts.Any(c => c.Contactname == group.GroupName);
			if (exists)
				return false;

			var contact = new ContactModel
			{
				Contactname = group.GroupName,
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

				var targetContact = Contacts.FirstOrDefault(c => c.Contactname == group.GroupName);
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

		private async void LoadFriends()
		{
			var friends = await ChatService.GetFriendsAsync(CurrentUser.Username);

			Contacts.Clear(); // 先清空当前列表

			foreach (var friendUsername in friends)
			{
				var userInfo = await ChatService.SearchUserAsync(friendUsername);
				if (userInfo != null)
				{
					Contacts.Add(new ContactModel
					{
						Contactname = userInfo.Username,
						Nickname = userInfo.Nickname,
						ImageSource = userInfo.AvatarUrl,
						Messages = new ObservableCollection<MessageModel>(),
						IsGroup = false
					});
				}
			}
		}
		private async void LoadMessagesForSelectedContact()
		{
			if (SelectedContact != null)
			{
				Messages.Clear();
				List<MessageModel> history;

				if (SelectedContact.IsGroup)
				{
					history = await ChatService.GetGroupHistoryPageAsync(
							SelectedContact.Contactname,
							SelectedContact.CurrentPage,
							SelectedContact.PageSize);
				}
				else
				{
					history = await ChatService.GetPrivateHistoryPageAsync(
							_currentUser.Username,
							SelectedContact.Contactname,
							SelectedContact.CurrentPage,
							SelectedContact.PageSize);
				}

				foreach (var msg in history)
				{
					Messages.Add(msg);
				}

				// 开始订阅该联系人的频道
				ChatService.SubscribeToChannel(SelectedContact.Contactname, OnMessageReceived);
			}
		}

		private void OnMessageReceived(MessageModel message)
		{
			message.IsNativeOrigin = message.SenderUsername == _currentUser.Username;
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
				await ChatService.SendGroupMessageAsync(SelectedContact.Contactname, newMessage);
			}
			else
			{
				await ChatService.SendPrivateMessageAsync(_currentUser.Username, SelectedContact.Contactname, newMessage);
			}


			SelectedContact.Messages.Add(newMessage);
			Message = string.Empty;
			await ChatService.AddSessionAsync(_currentUser.Username, SelectedContact.Contactname);

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
				history = await ChatService.GetGroupHistoryPageAsync(contact.Contactname, contact.CurrentPage, contact.PageSize);
			}
			else
			{
				history = await ChatService.GetPrivateHistoryPageAsync(_currentUser.Username, contact.Contactname, contact.CurrentPage, contact.PageSize);
			}

			if (history.Count == 0)
			{
				contact.HasMoreMessages = false;
				return;
			}

			// 插入到聊天记录最前面
			for (int i = history.Count - 1; i >= 0; i--)
			{
				var msg = history[i];
				msg.IsNativeOrigin = msg.SenderUsername == _currentUser.Username;
				contact.Messages.Insert(0, msg);
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

		public async Task LoadFriendsAsync()
		{
			var friends = await ChatService.GetFriendsAsync(CurrentUser.Username);

			foreach (var friendUsername in friends)
			{
				var userInfo = await ChatService.SearchUserAsync(friendUsername);
				if (userInfo != null)
				{
					Contacts.Add(new ContactModel
					{
						Contactname = userInfo.Username,
						Nickname = userInfo.Nickname,
						ImageSource = userInfo.AvatarUrl,
						Signature = userInfo.Signature,
						Messages = new ObservableCollection<MessageModel>(),
						IsGroup = false
					});
				}
			}
		}


		
	}
}
