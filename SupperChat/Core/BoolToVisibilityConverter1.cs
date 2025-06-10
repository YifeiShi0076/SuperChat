using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SupperChat.Core  
{
	public class BoolToVisibilityConverter1 : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool flag = (bool)value;
			if (parameter?.ToString()?.ToLower() == "false")
				flag = !flag;

			return flag ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}
