using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public App()
		{
			if (!Debugger.IsAttached) this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

			VersionInfo.DeleteUpdater();
		}

		void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			string errorMessage = "An unhandled exception occurred!\n\n" + e.Exception.Message + "\n\nThe app has attempted to recover and carry on, but you may experience some weirdness. Report error?";
			File.WriteAllText("error.log", e.Exception.ToString());

			var choice = Message.Show(errorMessage, "Error", "Report", "Ignore");
			if (choice == "Report")
			{
				Email.SendEmail("Crash Report", "Editor crashed on " + DateTime.Now + ".\nEditor Version: " + VersionInfo.Version, e.Exception.ToString());
				Message.Show("Error Reported, I shall fix as soon as possible.", "Error reported", "Ok");
			}

			e.Handled = true;
		}
	}
}
