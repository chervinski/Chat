using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace ChatClient
{
	/// <summary>
	/// Interaction logic for EnterWindow.xaml
	/// </summary>
	public partial class EnterWindow : Window
	{
		public EnterWindow()
		{
			InitializeComponent();
		}
		private void Enter_Click(object sender, RoutedEventArgs e)
		{
			TcpClient client = null;
			try
			{
				if (name.Text == string.Empty || name.Text.All(x => char.IsWhiteSpace(x)))
					throw new Exception("Empty name is not allowed.");
				if (name.Text.Length > 32)
					throw new Exception("Name is too long.");

				client = new TcpClient("127.0.0.1", 8888);
				NetworkStream stream = client.GetStream();
				byte[] data = Encoding.Unicode.GetBytes(name.Text);
				stream.Write(data, 0, data.Length);

				if (stream.ReadByte() == 0)
					throw new Exception("This name is already taken.");

				new MainWindow(name.Text, client).Show();
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				client?.Close();
			}
		}
	}
}
