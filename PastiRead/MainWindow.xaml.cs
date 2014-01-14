/*!
@file MainWindow.xaml.cs
<summary>Main WPF window to call the reader</summary>

<div class="jlg">Copyright (C) 2014 Jean Louis-Guerin\n\n
This file is part of the Atari Image Reader (AIR) project.\n
The Atari Image Reader project may be used and distributed without restriction provided
that this copyright statement is not removed from the file and that any
derivative work contains the original copyright notice and the associated
disclaimer.\n
The Atari Image Reader project is free software; you can redistribute it
and/or modify  it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.\n
The Atari Image Reader project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU General Public License for more details.\n\n
You should have received a copy of the GNU General Public License
along with the Atari Universal FD Image Tool project; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA\n</div>

@author Jean Louis-Guerin
*/

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pasti {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		Floppy _fd;
		BufferWindow _trackWindow = null;
		BufferWindow _sectorWindow = null;
		bool _trackWindowOpen = false;
		bool _sectorWindowOpen = false;

		public MainWindow() {
			InitializeComponent();
		}

		private void btFileClick(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Pasti file|*.stx|All Files|*.*";
			bool? ok = ofd.ShowDialog();
			if (ok == true) {
				fileName.Text = ofd.FileName;
				PastiReader pasti = new PastiReader(infoBox);
				_fd = new Floppy();
				pasti.readPasti(ofd.FileName, _fd);
			}

		}

		private void btWriteClick(object sender, RoutedEventArgs e) {

			if (_fd == null) {
				MessageBox.Show("Nothing to write", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Pasti file|*.stx|All Files|*.*";
//			askOutput:

			bool? ok = sfd.ShowDialog();
			if (ok == true) {
				fileName.Text = sfd.FileName;
				//if (File.Exists(sfd.FileName)) {
				//	MessageBoxResult result = MessageBox.Show("Do you want to overwrite?", "File already exist", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No); 
				//	if (result == MessageBoxResult.Cancel) return;
				//	if (result == MessageBoxResult.No) goto askOutput;					
				//}				
				PastiWriter pasti = new PastiWriter(infoBox);
				pasti.writePasti(sfd.FileName, _fd);
			}
		}

		private void btTrackClick(object sender, RoutedEventArgs e) {
			int trackNumber;
			int sideNumber;
			tbStatus.Clear();
			if (Int32.TryParse(tbTrack.Text, out trackNumber) == false)
				tbStatus.Text = "Invalid track number - please correct and try again";
			if (Int32.TryParse(tbSide.Text, out sideNumber) == false)
				tbStatus.Text = "Invalid side number - please correct and try again";
			
			if ((trackNumber < 0) || (trackNumber > 84) || (sideNumber < 0) || (sideNumber > 1))
				tbStatus.Text = "Track or Side out of range - please correct and try again";
			if (_fd == null)
				tbStatus.Text = "Nothing to display";

			if (_trackWindowOpen)
				_trackWindow.Close();

			_trackWindow = new BufferWindow();
			_trackWindow.drawTrackBuffer(_fd, trackNumber, sideNumber);
			_trackWindow.Closed += new EventHandler(trackWindowClosed);
			_trackWindowOpen = true;
			_trackWindow.Show();
		}

		void trackWindowClosed(object sender, EventArgs e) {
			_trackWindowOpen = false;
		}


		private void btSectorsClick(object sender, RoutedEventArgs e) {
			int trackNumber;
			int sideNumber;
			tbStatus.Clear();
			if (Int32.TryParse(tbTrack.Text, out trackNumber) == false)
				tbStatus.Text = "Invalid track number - please correct and try again";
			if (Int32.TryParse(tbSide.Text, out sideNumber) == false)
				tbStatus.Text = "Invalid side number - please correct and try again";

			if ((trackNumber < 0) || (trackNumber > 84) || (sideNumber < 0) || (sideNumber > 1))
				tbStatus.Text = "Track or Side out of range - please correct and try again";
			if (_fd == null)
				tbStatus.Text = "Nothing to display";

			if (_sectorWindowOpen)
				_sectorWindow.Close();

			_sectorWindow = new BufferWindow();
			_sectorWindow.drawSectorBuffer(_fd, trackNumber, sideNumber);
			_sectorWindow.Closed += new EventHandler(sectorWindowClosed);
			_sectorWindowOpen = true;
			_sectorWindow.Show();
		}

		void sectorWindowClosed(object sender, EventArgs e) {
			_sectorWindowOpen = false;
		}

	}
}
