using System;

namespace ChatServer
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				using (Server server = new Server(8888))
					server.Start();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
