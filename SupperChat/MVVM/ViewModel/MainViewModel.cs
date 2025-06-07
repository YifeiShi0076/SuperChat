using SupperChat.Core;
using SupperChat.MVVM.Model;
using SupperChat.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SupperChat.MVVM.ViewModel
{
	public class MainViewModel : ObservableObject
	{
		private readonly UserModel _currentUser;

		public ObservableCollection<ContactModel> Contacts { get; set; }
		public RelayCommand SendCommand { get; set; }
		public RelayCommand AddContactCommand { get; set; }

		private ContactModel _selectedContact;
		public ContactModel SelectedContact
		{
			get => _selectedContact;
			set
			{
				_selectedContact = value;
				OnPropertyChanged();
				LoadMessagesForSelectedContact();
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

		public MainViewModel(UserModel currentUser)
		{
			_currentUser = currentUser;
			Contacts = new ObservableCollection<ContactModel>();
			Messages = new ObservableCollection<MessageModel>();

			SendCommand = new RelayCommand(async o => await SendMessage());
			AddContactCommand = new RelayCommand(o => AddContactOrGroup());

			LoadContacts();
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
				UsernameColor = "#409aff",
				ImageSource = _currentUser.AvatarUrl,
				Message = Message,
				Time = DateTime.Now,
				IsNativeOrigin = true
			};

			await ChatService.SendMessageAsync(SelectedContact.Username, newMessage);

			Messages.Add(newMessage);
			Message = string.Empty;
		}

		private void AddContactOrGroup()
		{
			// 这里可以弹窗输入新联系人名字，或者新建群聊
		}

		public UserModel CurrentUser => _currentUser;
	}
}
