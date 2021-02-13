using System.Net.Sockets;
using System.Windows;

namespace ChatClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow(string userName, TcpClient client)
		{
			InitializeComponent();
			DataContext = new ViewModel(userName, client.GetStream());
			Closed += (s, e) => { client.GetStream().Close(); client.Close(); };
		}
	}
}
