using SupperChat.Core;
using SupperChat.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupperChat.MVVM.ViewModel
{
	class MainViewModel:ObservableObject
	{
		public ObservableCollection<MessageModel> Messages { get; set; }
		public ObservableCollection<ContactModel> Contacts { get; set; }

		/* Command */
		public RelayCommand SendCommand { get; set; }
		private ContactModel _selectedContact;
		public ContactModel SelectedContact {
			get { return _selectedContact; }
			set { _selectedContact = value; 
				OnPropertyChanged();
			}
		}
		private string _message;
		public string Message { 
			get { return _message; }
			set { _message = value; 
				OnPropertyChanged(); 
			}
		}
		public MainViewModel()
		{
			Messages = new ObservableCollection<MessageModel>();
			Contacts = new ObservableCollection<ContactModel>();

			SendCommand = new RelayCommand(o =>
			{
				if (string.IsNullOrEmpty(Message)) return;
				Messages.Add(new MessageModel
				{
					Message = Message,
					FirstMessage = false
				});
				Message = string.Empty;
			});

			Messages.Add(new MessageModel
			{
				Username = "John Doe",
				UsernameColor = "#409aff",
				ImageSource = "../Icons/1.jpg",
				Message = "Hello, this is a test message from John!",
				Time = DateTime.Now,
				IsNativeOrigin = false,
				FirstMessage = true
			});

			for (int i = 0; i < 1; i++)
			{
				Messages.Add(new MessageModel
				{
					Username = "Bunny",
					UsernameColor = "#409aff",
					ImageSource = "../Icons/0.jpg",
					Message = "Hello, this is a test message from Bunny!",
					Time = DateTime.Now,
					IsNativeOrigin = true
				});
			}

			for (int i = 0; i < 1; i++)
			{
				Messages.Add(new MessageModel
				{
					Username = "Bunny",
					UsernameColor = "#409aff",
					ImageSource = "../Icons/0.jpg",
					Message = "Last Message.",
					Time = DateTime.Now,
					IsNativeOrigin = true
				});
			}

			for (int i = 0; i < 3; i++)
			{
				Contacts.Add(new ContactModel
				{
					Username = $"Allison{i}",
					ImageSource = "../Icons/1.jpg",
					Messages = Messages
				});
			}
		}
	}
}
