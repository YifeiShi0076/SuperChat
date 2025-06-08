using SupperChat.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupperChat.MVVM.Model
{
	public class ContactModel:ObservableObject
	{
		public string Username { get; set; }
		public string ImageSource { get; set; }
		public ObservableCollection<MessageModel> Messages { get; set; }
		public string LastMessage => Messages != null && Messages.Any() ? Messages.Last().Message : "";
		public bool IsGroup { get; set; }
		public int CurrentPage { get; set; } = 1; // 当前已加载的页数
		public int PageSize { get; set; } = 20;   // 每页条数
		public bool HasMoreMessages { get; set; } = true; // 是否还有更多聊天记录
		private int _unreadCount;
		public int UnreadCount
		{
			get => _unreadCount;
			set { _unreadCount = value; OnPropertyChanged(); }
		}


	}
}
