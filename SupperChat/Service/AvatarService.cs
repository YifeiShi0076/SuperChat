using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace SupperChat.Services
{
	public static class AvatarService
	{
		public static string GenerateRandomAvatar(string nickname)
		{
			int size = 256;
			var bmp = new Bitmap(size, size);

			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.Clear(Color.White);

				Random rand = new Random();
				var bgColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));

				using (Brush brush = new SolidBrush(bgColor))
				{
					g.FillEllipse(brush, 0, 0, size, size);
				}

				string firstChar = string.IsNullOrEmpty(nickname) ? "?" : nickname.Trim()[0].ToString().ToUpper();

				using (Font font = new Font(new FontFamily("Arial"), size / 2, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
				{
					var textSize = g.MeasureString(firstChar, font);
					var textPosition = new PointF(
						(size - textSize.Width) / 2,
						(size - textSize.Height) / 2
					);

					using (Brush textBrush = new SolidBrush(Color.White))
					{
						g.DrawString(firstChar, font, textBrush, textPosition);
					}
				}
			}

			// 改到程序所在目录
			string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
			string avatarFolder = Path.Combine(exeFolder, "Avatars");

			if (!Directory.Exists(avatarFolder))
			{
				Directory.CreateDirectory(avatarFolder);
			}

			string filePath = Path.Combine(avatarFolder, Guid.NewGuid().ToString() + ".png");

			bmp.Save(filePath, ImageFormat.Png);
			bmp.Dispose();

			return filePath;
		}
	}
}
