using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ChatServer
{
	class Client : IDisposable
	{
		public string UserName { get; private set; }
		public NetworkStream Stream { get; private set; }

		private TcpClient tcpClient;
		private Server server;

		public Client(TcpClient tcpClient, Server server)
		{
			this.tcpClient = tcpClient;
			this.server = server;
		}
		public void Process()
		{
			Stream = tcpClient.GetStream();
			UserName = GetMessage();
			if (server.Clients.Any(x => x.UserName == UserName))
			{
				Stream.WriteByte(0);
				return;
			}
			else Stream.WriteByte(1);

			server.Clients.Add(this);
			XElement users = new XElement("UserNames");
			foreach (string name in server.Clients.Select(x => x.UserName).ToArray())
				users.Add(new XElement("UserName", name));
			byte[] xml = Encoding.Unicode.GetBytes(users.ToString());
			Stream.Write(xml, 0, xml.Length);
			server.Broadcast(new XElement("Message", new XAttribute("Action", "Join"), new XAttribute("From", UserName)).ToString(), UserName);

			while (true)
				try
				{
					string message = GetMessage();
					foreach (XElement element in XElement.Parse($"<r>{message}</r>").Elements())
					{
						string receiver = element.Attribute("To").Value;
						if (receiver == string.Empty)
							server.Broadcast(message, UserName);
						else server.Send(message, receiver);
					}
				}
				catch
				{
					server.Clients.Remove(this);
					server.Broadcast(new XElement("Message", new XAttribute("Action", "Leave"), new XAttribute("From", UserName)).ToString(), UserName);
					Dispose();
					break;
				}
		}
		private string GetMessage()
		{
			byte[] data = new byte[4096];
			using (MemoryStream ms = new MemoryStream())
			{
				do
				{
					int bytes = Stream.Read(data, 0, data.Length);
					ms.Write(data, 0, bytes);
				} while (Stream.DataAvailable);
				data = ms.ToArray();
			}

			if (data.Length == 0)
				throw new EndOfStreamException();
			return Encoding.Unicode.GetString(data, 0, data.Length);
		}
		public void Dispose()
		{
			Stream?.Close();
			tcpClient.Close();
		}
	}
}
