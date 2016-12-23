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
			string errorMessage = string.Format("An unhandled exception occurred! The app has attempted to recover and carry on, but you may experience some weirdness. Please create a new issue on github and attach the error.log file so that I can fix this in the future.", e.Exception.Message);
			File.WriteAllText("error.log", e.Exception.ToString());
			Message.Show(errorMessage, "Error", "Ok");
			e.Handled = true;
		}
	}
}
