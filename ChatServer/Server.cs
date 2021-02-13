using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace ChatServer
{
	class Server : IDisposable
	{
		public List<Client> Clients { get; set; }
		private TcpListener listener;
		public Server(int port)
		{
			listener = new TcpListener(IPAddress.Any, port);
			Clients = new List<Client>();
		}
		public void Start()
		{
			listener.Start();
			Console.WriteLine("Server started.");

			while (true)
				new Thread(new ThreadStart(new Client(listener.AcceptTcpClient(), this).Process)).Start();
		}
		public void Broadcast(string message, string name)
		{
			byte[] data = Encoding.Unicode.GetBytes(message);
			foreach (Client client in Clients)
				if (client.UserName != name)
					client.Stream.Write(data, 0, data.Length);
		}
		public void Send(string message, string name)
		{
			byte[] data = Encoding.Unicode.GetBytes(message);
			foreach (Client client in Clients)
				if (client.UserName == name)
				{
					client.Stream.Write(data, 0, data.Length);
					break;
				}
		}
		public void Dispose()
		{
			listener.Stop();
			foreach (Client client in Clients)
				client.Dispose();
			Console.WriteLine("Server stoped.");
		}
	}
}
