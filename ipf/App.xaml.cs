using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ipf {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		// used to save size and position
		private void Application_Exit(object sender, ExitEventArgs e) {
			ipf.Properties.Settings.Default.Save();
		}

	}


}
