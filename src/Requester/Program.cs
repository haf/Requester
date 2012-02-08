using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Topshelf;
using log4net.Config;

namespace Requester
{
	class Program
	{
		private const string _urlFile = "url.txt";
		private const string _intervalFile = "interval.txt";
		private CancellationToken _token;
		private CancellationTokenSource _tokenSource;
		private TimeSpan _timeout;

		static void Main(string[] args)
		{
			BasicConfigurator.Configure();

			if (args.Length < 2)
			{
				PrintHelp();
				Environment.Exit(-1);
				return;
			}

			var isInstallOrUninstall = args[0].Contains("install");
			var url = args[isInstallOrUninstall ? 1 : 0];
			var interval = args[isInstallOrUninstall? 2 : 1];

			int res;
			if (!int.TryParse(interval, out res))
			{
				PrintHelp();
				Environment.Exit(-1);
				return;
			}

			if (!File.Exists(_urlFile))
			{
				File.WriteAllText(_urlFile, "http://google.com");
			}

			if (!File.Exists(_intervalFile))
			{
				File.WriteAllText(_intervalFile, "40");
			}

			Thread.CurrentThread.Name = "Requester Entrypoint Thread";

			HostFactory.Run(x =>
				{
					x.Service<Program>(s =>
						{
							s.ConstructUsing(name => new Program());
							s.WhenStarted(p => p.Start(File.ReadAllText(_urlFile), int.Parse(File.ReadAllText(_intervalFile))));
							s.WhenStopped(p => p.Stop());
						});
					x.RunAsLocalSystem();
					x.BeforeInstall(() =>
						{
							File.WriteAllText(_urlFile, args[1]);
							File.WriteAllText("inteval.txt", interval);
						});
					x.SetDescription(string.Format("Requests an url every interval"));
					x.SetDisplayName("Requester");
					x.SetServiceName("Requester");
				});
		}

		private static void PrintHelp()
		{
			Console.WriteLine(
@"Too few args: requester install <url> <interval>

Sample usage; request
 url: ""https://github.com""
 interval: 300 seconds

requester install https://github.com 300
");
		}

		private void Start(string url, int interval)
		{
			_tokenSource = new CancellationTokenSource();
			_token = _tokenSource.Token;

			while (!_token.IsCancellationRequested)
			{
				//httpClient.DefaultRequestHeaders
				//    .Add("Authorization", string.Format("Basic {0}",
				//        Convert.ToBase64String(Encoding.UTF8.GetBytes("{0}:{1}".FormatWith(_username, _password)))));

				try
				{
					new HttpClient().GetAsync(url).Wait(20000, _token);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				Thread.Sleep(TimeSpan.FromSeconds(interval));
			}
		}

		private void Stop()
		{
			_tokenSource.Cancel();
		}
	}
}
