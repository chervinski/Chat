using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace ChatClient
{
	class ViewModel
	{
		private static readonly string downloadDirectory = "downloads";
		private NetworkStream stream;

		public ObservableCollection<TabItem> Channels { get; set; }
		public ObservableCollection<string> Users { get; set; }
		public TabItem SelectedChannel { get; set; }

		public Command LoadedCommand { get; set; }
		public Command AddChannelCommand { get; set; }
		public Command SendTextCommand { get; set; }
		public Command SendFileCommand { get; set; }

		public ViewModel(string userName, NetworkStream stream)
		{
			this.stream = stream;
			Channels = new ObservableCollection<TabItem>();
			Users = new ObservableCollection<string>();

			LoadedCommand = new Command(async (x) => await ListenAsync());

			AddChannelCommand = new Command((x) => {
				if (x.ToString() != userName)
					CreateChannel(x.ToString());
			});

			SendTextCommand = new Command((x) => {
				TextBox textBox = x as TextBox;
				if (SelectedChannel != null && textBox.Text != string.Empty)
				{
					string s = new XElement("Message", textBox.Text,
						new XAttribute("Action", "Text"),
						new XAttribute("From", userName),
						new XAttribute("To", SelectedChannel == Channels[0] ? string.Empty : SelectedChannel.Header.ToString())).ToString();
					byte[] data = Encoding.Unicode.GetBytes(new XElement("Message", textBox.Text,
						new XAttribute("Action", "Text"),
						new XAttribute("From", userName),
						new XAttribute("To", SelectedChannel == Channels[0] ? string.Empty : SelectedChannel.Header.ToString())).ToString());

					stream.Write(data, 0, data.Length);
					Print(SelectedChannel.Content as RichTextBox, new Run(userName + ": ") { FontWeight = FontWeights.Bold, Foreground = Brushes.DeepSkyBlue }, new Run(textBox.Text));
					textBox.Text = string.Empty;
				}
			});

			SendFileCommand = new Command((x) => {
				if (SelectedChannel != null)
				{
					OpenFileDialog dialog = new OpenFileDialog();
					if (dialog.ShowDialog() == true)
						try
						{
							string fileName = Path.GetFileName(dialog.FileName);
							byte[] data = Encoding.Unicode.GetBytes(new XElement("Message", Convert.ToBase64String(File.ReadAllBytes(dialog.FileName)),
								new XAttribute("Action", "File"),
								new XAttribute("FileName", fileName),
								new XAttribute("From", userName),
								new XAttribute("To", SelectedChannel == Channels[0] ? string.Empty : SelectedChannel.Header.ToString())).ToString());

							stream.Write(data, 0, data.Length);
							Print(SelectedChannel.Content as RichTextBox, new Run(userName + ": ") { FontWeight = FontWeights.Bold, Foreground = Brushes.DeepSkyBlue }, new Run(fileName) { TextDecorations = TextDecorations.Underline, Foreground = Brushes.LightSkyBlue });
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message);
						}
				}
			});
		}
		public async Task ListenAsync()
		{
			CreateChannel("Public");
			try
			{
				while (true)
					using (MemoryStream ms = new MemoryStream())
					{
						byte[] data = new byte[4096];
						do
						{
							int bytes = await stream.ReadAsync(data, 0, data.Length);
							ms.Write(data, 0, bytes);
						} while (stream.DataAvailable);

						try
						{
							foreach (XElement element in XElement.Parse($"<r>{Encoding.Unicode.GetString(ms.ToArray())}</r>").Elements())
								if (element.Name == "Message")
								{
									string sender = element.Attribute("From").Value;
									switch (element.Attribute("Action").Value)
									{
										case "Join":
											Users.Add(sender);
											Print(Channels[0].Content as RichTextBox, new Run(sender + " joined the chat.") { FontWeight = FontWeights.Bold, Foreground = Brushes.Green });
											break;
										case "Leave":
											RemoveChannel(sender);
											Users.Remove(sender);
											Print(Channels[0].Content as RichTextBox, new Run(sender + " left the chat.") { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
											break;
										case "Text":
											Print((element.Attribute("To").Value == string.Empty ? Channels[0] : CreateChannel(sender)).Content as RichTextBox, new Run(sender + ": ") { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkBlue }, new Run(element.Value));
											break;
										case "File":
											{
												string fileName = element.Attribute("FileName").Value;
												Button button = new Button() { Content = fileName, Padding = new Thickness(5), Margin = new Thickness(5, 0, 0, 0) };

												try
												{
													Directory.CreateDirectory(downloadDirectory);
													File.WriteAllBytes(Path.Combine(downloadDirectory, fileName), Convert.FromBase64String(element.Value));
													button.Click += (s, e) =>
													{
														Button b = s as Button;
														try
														{
															Process.Start(Path.Combine(downloadDirectory, b.Content.ToString()));
														}
														catch (Exception ex)
														{
															MessageBox.Show(ex.Message);
															b.IsEnabled = false;
														}
													};
												}
												catch (Exception ex)
												{
													MessageBox.Show(ex.Message);
													button.IsEnabled = false;
												}

												Print((element.Attribute("To").Value == string.Empty ? Channels[0] : CreateChannel(sender)).Content as RichTextBox, new Run(sender + ": ") { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkBlue }, button);
												break;
											}
									}
								}
								else if (element.Name == "UserNames")
								{
									Users.Clear();
									foreach (XElement userName in element.Elements())
										Users.Add(userName.Value);
								}
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message);
						}
					}
			}
			catch { }
		}
		private TabItem CreateChannel(string name)
		{
			TabItem channel = Channels.Skip(1).FirstOrDefault(x => x.Header.ToString() == name);
			if (channel == null)
			{
				RichTextBox rtb = new RichTextBox() {
					IsReadOnly = true,
					IsDocumentEnabled = true,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto
				};

				Style style = new Style(typeof(Paragraph));
				style.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(3)));
				rtb.Resources.Add(typeof(Paragraph), style);

				rtb.Document.Blocks.Clear();
				Channels.Add(channel = new TabItem() { Header = name, Content = rtb });
			}
			return channel;
		}
		private bool RemoveChannel(string name)
		{
			TabItem channel = Channels.Skip(1).FirstOrDefault(x => x.Header.ToString() == name);
			return channel != null ? Channels.Remove(channel) : false;
		}
		private void Print(RichTextBox rtb, params Inline[] inlines)
		{
			Paragraph paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run(DateTime.Now.ToString("t") + '\t') { FontStyle = FontStyles.Italic });

			foreach (Inline inline in inlines)
				paragraph.Inlines.Add(inline);

			rtb.Document.Blocks.Add(paragraph);
			rtb.ScrollToEnd();
		}
		private void Print(RichTextBox rtb, Inline inline, params UIElement[] elements)
		{
			Paragraph paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run(DateTime.Now.ToString("t") + '\t') { FontStyle = FontStyles.Italic });

			paragraph.Inlines.Add(inline);
			foreach (UIElement element in elements)
				paragraph.Inlines.Add(element);

			rtb.Document.Blocks.Add(paragraph);
			rtb.ScrollToEnd();
		}
	}
}
