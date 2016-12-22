using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
	class Program
	{
		static void Main(string[] args)
		{
			var appName = args[0];

			while (true)
			{
				if (File.Exists(appName))
				{
					try
					{
						File.Delete(appName);
						break;
					}
					catch (Exception) { }
				}
				else
				{
					break;
				}
			}

			while (true)
			{
				try
				{
					File.Move("Downloaded.exe", appName);
					break;
				}
				catch (Exception) { }
			}

			Process process = Process.Start(appName, "Updated");
		}
	}
}
