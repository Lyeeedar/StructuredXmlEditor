using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
			this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
		}

		void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
			File.WriteAllText("error.log", e.Exception.ToString());
			Message.Show(errorMessage, "Error", "Ok");
			e.Handled = true;
		}
	}
}
