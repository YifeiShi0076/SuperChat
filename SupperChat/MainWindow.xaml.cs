using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SupperChat.MVVM.Model;
using SupperChat.MVVM.ViewModel;
using SupperChat.Views;

namespace SupperChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private readonly MainViewModel _viewModel;
		private bool _isAtBottom = true;

		public MainWindow(UserModel userInfo)
        {
			InitializeComponent();
			_viewModel = new MainViewModel(userInfo);
			this.DataContext = _viewModel;

			this.Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await _viewModel.LoadSessionsAsync();
		}

		private void Border_MouseDown(object sender, MouseButtonEventArgs e)
		{
            if(e.LeftButton == MouseButtonState.Pressed)
                DragMove();

		}

		private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.MainWindow.WindowState = WindowState.Minimized;
		}

		private void ButtonMaximize_Click(object sender, RoutedEventArgs e)
		{
			if(Application.Current.MainWindow.WindowState == WindowState.Maximized)
			{
				Application.Current.MainWindow.WindowState = WindowState.Normal;
			}
			else
			{
				Application.Current.MainWindow.WindowState = WindowState.Maximized;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if (AddButton.ContextMenu != null)
			{
				AddButton.ContextMenu.PlacementTarget = AddButton;
				AddButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
				AddButton.ContextMenu.IsOpen = true;
			}
		}

		private void MessagesListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			// ✅ 1. 顶部检测 — 加载更多历史记录
			if (e.VerticalOffset == 0 && e.ExtentHeightChange == 0)
			{
				// 滚动条到顶，加载上一页历史记录
				if (DataContext is MainViewModel vm && vm.SelectedContact != null)
				{
					_ = vm.LoadMoreMessagesAsync(vm.SelectedContact); // 注意异步
				}
			}

			 // ✅ 2.底部检测 — 是否在聊天底部
			if (e.ExtentHeight - e.ViewportHeight - e.VerticalOffset < 10)
			{
				// 已经在底部
				_isAtBottom = true;
				NewMessageButton.Visibility = Visibility.Collapsed; // 在底部时不显示新消息按钮
			}
			else
			{
				_isAtBottom = false;
			}
		}


		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var vm = this.DataContext as MainViewModel;
			if (vm?.SelectedContact != null && vm?.SelectedContact.Messages != null)
			{
				vm.SelectedContact.Messages.CollectionChanged += Messages_CollectionChanged;
			}
		}

		private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				if (e.NewItems != null && e.NewItems.Count > 0)
				{
					var lastMessage = e.NewItems[e.NewItems.Count - 1] as MessageModel;
					if (lastMessage != null)
					{
						if (lastMessage.IsNativeOrigin)
						{
							// 自己发的消息，肯定滚动
							ScrollToBottom();
						}
						else
						{
							if (_isAtBottom)
							{
								// 已经在底部，自动滚动到底
								ScrollToBottom();
							}
							else
							{
								// 不在底部，显示新消息按钮
								NewMessageButton.Visibility = Visibility.Visible;
							}
						}
					}
				}
			}
		}

		private void NewMessageButton_Click(object sender, RoutedEventArgs e)
		{
			ScrollToBottom();
			NewMessageButton.Visibility = Visibility.Collapsed; // 滚到底后隐藏按钮
		}
		private void ScrollToBottom()
		{
			if (MessageListView.Items.Count > 0)
			{
				MessageListView.ScrollIntoView(MessageListView.Items[MessageListView.Items.Count - 1]);
			}
		}
		private void UserAvatar_Click(object sender, MouseButtonEventArgs e)
		{
			var userInfoWindow = new UserInfoWindow(_viewModel.CurrentUser); // 传递当前用户信息
			userInfoWindow.ShowDialog(); // 模态打开
		}

	}
}