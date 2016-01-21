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
			//string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			DateTime dateTimeOfBuild = new DateTime(2000, 1, 1)
								+ new TimeSpan(version.Build, 0, 0, 0)
								+ TimeSpan.FromSeconds(version.Revision * 2);
			Title = "Pasti STX File Reader-Writer " + version.Major + "." + version.Minor + " - " + dateTimeOfBuild;
		}


		private void btFileClick(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Pasti file|*.stx|All Files|*.*";
			bool? ok = ofd.ShowDialog();
			if (ok == true) {
				processFile(ofd.FileName);
			}
		}


		private void processFile(string file) {
			PastiReader pasti = new PastiReader(infoBox);
			_fd = new Floppy();
			fileName.Text = file;
			pasti.readPasti(file, _fd);
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
			if (_fd == null) {
				tbStatus.Text = "Nothing to display";
				return;
			}
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
			_trackWindow.displayTrackBuffer(_fd, trackNumber, sideNumber);
			_trackWindow.Closed += new EventHandler(trackWindowClosed);
			_trackWindowOpen = true;
			_trackWindow.Show();
		}

		void trackWindowClosed(object sender, EventArgs e) {
			_trackWindowOpen = false;
		}


		private void btSectorsClick(object sender, RoutedEventArgs e) {
			if (_fd == null) {
				tbStatus.Text = "Nothing to display";
				return;
			}
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
			_sectorWindow.displaySectorBuffer(_fd, trackNumber, sideNumber);
			_sectorWindow.Closed += new EventHandler(sectorWindowClosed);
			_sectorWindowOpen = true;
			_sectorWindow.Show();
		}


		void sectorWindowClosed(object sender, EventArgs e) {
			_sectorWindowOpen = false;
		}


		private void btAllSectorsClick(object sender, RoutedEventArgs e) {
			tbStatus.Clear();
			if (_fd == null) {
				tbStatus.Text = "Nothing to display";
				return;
			}

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Text file|*.txt|All Files|*.*";
			if (sfd.ShowDialog() == true) {
				displayAllSectorBuffer(_fd, sfd.FileName);
			}


		}

		/// <summary>
		/// Display all the sectors
		/// </summary>
		/// <param name="floppy">Contains the DB</param>
		/// <param name="filename">The name of the open file</param>
		public void displayAllSectorBuffer(Floppy floppy, string filename) {
			StringBuilder sb = new StringBuilder();

			for (int track = 0; track < 84; track++) {
				for (int side = 0; side < 2; side++) {
					Track t = floppy.tracks[track, side];
					if (t == null) continue;

					sb.Append(String.Format("Track {0:D2}.{1} has {2} sectors\n", track, side, t.sectorCount));
					for (int sect = 0; sect < t.sectorCount; sect++) {
						sb.Append(String.Format("Sector {0}", t.sectors[sect].id.number));
						sb.Append(String.Format(" T={0} H={1} N={2} S={3} CRC={4:X4}",
							t.sectors[sect].id.track, t.sectors[sect].id.side, t.sectors[sect].id.number, t.sectors[sect].id.size, t.sectors[sect].id.crc));

						if (t.sectors[sect].sectorData != null) {
							sb.Append(String.Format(" has {0} bytes\n", t.sectors[sect].sectorData.Count()));
							sb.Append(String.Format("       bitPosition {0}, Flags {1:X2}", t.sectors[sect].bitPosition, t.sectors[sect].fdcFlags));
							sb.Append(String.Format(" {0} {1}\n", ((t.sectors[sect].fdcFlags & SectorDesc.CRC_ERR) == 0) ?
								"Good CRC" : "Bad CRC", (t.sectors[sect].fuzzyData != null) ? "Has Fuzzy bytes" : ""));
							saveBuffer(t.sectors[sect].sectorData, sb);
							if (t.sectors[sect].fuzzyData != null) {
								sb.Append(String.Format("\nFuzzy bytes for sector {0}\n", t.sectors[sect].fuzzyData.Count()));
								saveBuffer(t.sectors[sect].fuzzyData, sb);
							}
							if (t.sectors[sect].timmingData != null) {
								sb.Append(String.Format("\nTiming values for sector {0}\n", t.sectors[sect].timmingData.Count()));
								byte[] timming = new byte[t.sectors[sect].timmingData.Count()];
								for (int i = 0; i < t.sectors[sect].timmingData.Count(); i++)
									timming[i] = (byte)t.sectors[sect].timmingData[i];
								saveBuffer(timming, sb);
							}

						}
						else
							sb.Append(" *** Sector has no data ***\n");
						sb.Append("\n");
					}
				}
			}
			using (StreamWriter outputfile = new StreamWriter(filename)) {
				outputfile.Write(sb.ToString());
			}
		}


		/// <summary>
		/// Save the content of the buffer
		/// </summary>
		/// <param name="buffer">Buffer to save</param>
		/// <param name="sb">Save in a string builder</param>
		public void saveBuffer(byte[] buffer, StringBuilder sb) {
			for (int i = 0; i < buffer.Count(); i += 16) {
				sb.Append(String.Format("{0:D5}  ", i));

				string ascii = "  ";
				for (int j = 0; j < 16; j++) {
					if ((i + j) < buffer.Count()) {
						sb.Append(String.Format("{0:X2} ", buffer[i + j]));
						char ch = Convert.ToChar(buffer[i + j]);
						ascii += Char.IsControl(ch) ? "." : ch.ToString();
					}
					else
						sb.Append(String.Format("   "));
				}	// one more line
				sb.Append(ascii);
				sb.Append(String.Format("\n"));
			}	// all lines
		}


		private void fileDrop(object sender, DragEventArgs e) {
			string[] droppedFiles = null;
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				droppedFiles = e.Data.GetData(DataFormats.FileDrop, true) as string[];
			}

			if ((null == droppedFiles) || (!droppedFiles.Any())) { return; }
			processFile(droppedFiles[0]);
		}


		private void dragEnter(object sender, DragEventArgs e) {
			e.Handled = true;
		}

	}
}
