/*!
@file BufferWindow.xaml.cs
 
<summary>Open a Window and displays buffer information</summary>

<div class="jlg">Copyright (C) 2011 Jean Louis-Guerin\n\n
This file is part of the Atari Universal FD Image Tool project.\n\n
The Atari Universal FD Image Tool project may be used and distributed without restriction provided
that this copyright statement is not removed from the file and that any
derivative work contains the original copyright notice and the associated
disclaimer.\n\n
The Atari Universal FD Image Tool project is free software; you can redistribute it
and/or modify  it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 3
of the License, or (at your option) any later version.\n\n
The Atari Universal FD Image Tool project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\n
See the GNU General Public License for more details.\n\n
You should have received a copy of the GNU General Public License
along with the Atari Universal FD Image Tool project; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA\n</div>

@author Jean Louis-Guerin
*/

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
using System.Windows.Shapes;


namespace Pasti {
	/// <summary>
	/// Interaction logic for TrackBuffer.xaml
	/// </summary>
	public partial class BufferWindow : Window {
		/// <summary>
		/// Create a new BufferWindow object.
		/// </summary>
		public BufferWindow() {
			InitializeComponent();
		}

		/// <summary>
		/// Draw the content of the buffer
		/// </summary>
		/// <param name="floppy">Pointer to the floppy disk structure</param>
		/// <param name="track">The track number of the track to display</param>
		/// <param name="side">The side of the track to display</param>
		public void displaySectorBuffer(Floppy floppy, int track, int side) {

			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;

			Track t = floppy.tracks[track, side];

			displayBuffer.AppendText(String.Format("Track {0:D2}.{1} has {2} sectors\n", track, side, t.sectorCount));
			for (int sect = 0; sect < t.sectorCount; sect++) {
				displayBuffer.AppendText(String.Format("Sector {0}", t.sectors[sect].id.number));
				displayBuffer.AppendText(String.Format(" T={0} H={1} N={2} S={3} CRC={4:X4}",
					t.sectors[sect].id.track, t.sectors[sect].id.side, t.sectors[sect].id.number, t.sectors[sect].id.size, t.sectors[sect].id.crc));

				if (t.sectors[sect].sectorData != null) {
					displayBuffer.AppendText(String.Format(" has {0} bytes\n", t.sectors[sect].sectorData.Count()));
					displayBuffer.AppendText(String.Format("       bitPosition {0}, Flags {1:X2}", t.sectors[sect].bitPosition, t.sectors[sect].fdcFlags));
					displayBuffer.AppendText(String.Format(" {0} {1}\n", ((t.sectors[sect].fdcFlags & SectorDesc.CRC_ERR) == 0) ?
						"Good CRC" : "Bad CRC", (t.sectors[sect].fuzzyData != null) ? "Has Fuzzy bytes" : ""));
					drawBuffer(t.sectors[sect].sectorData);
					if (t.sectors[sect].fuzzyData != null) {
						displayBuffer.AppendText(String.Format("\nFuzzy bytes for sector {0}\n", t.sectors[sect].fuzzyData.Count()));
						drawBuffer(t.sectors[sect].fuzzyData);
					}
					if (t.sectors[sect].timmingData != null) {
						displayBuffer.AppendText(String.Format("\nTiming values for sector {0}\n", t.sectors[sect].timmingData.Count()));
						byte[] timming = new byte[t.sectors[sect].timmingData.Count()];
						for (int i = 0; i < t.sectors[sect].timmingData.Count(); i++)
							timming[i] = (byte)t.sectors[sect].timmingData[i];
						drawBuffer(timming);
					}

				}
				else
					displayBuffer.AppendText(" *** Sector has no data ***\n");
				displayBuffer.AppendText("\n");
			}
		}




		/// <summary>
		/// Display the content of the Track Information in a textbox
		/// </summary>
		/// <param name="track">Specifies the track from the disk</param>
		/// <param name="side">Specifies the side from the disk</param>
		/// <param name="floppy">The floppy disk structure</param>
		public void displayTrackBuffer(Floppy floppy, int track, int side) {
			Track t = floppy.tracks[track, side];
			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;

			displayBuffer.AppendText(String.Format("Track {0:D2}.{1} {2} bytes with {3} sectors\n",
					track, side, t.byteCount, t.sectors.Count()));
			if (t.trackData != null)
				drawBuffer(t.trackData);
			else
				displayBuffer.AppendText("Track has no Track Image Data Record");
		}

		/// <summary>
		/// Display the content of	 TrackBuffer buffer in the displayBuffer TextBox
		/// </summary>
		/// <param name="buffer">The buffer to display</param>
		/// <param name="message">an optional message to display before the buffer</param>
		public void drawBuffer(byte[] buffer, string message = null) {
			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;
			if (message != null)
				displayBuffer.AppendText(message);

			for (int i = 0; i < buffer.Count(); i += 16) {
				displayBuffer.AppendText(String.Format("{0:D5}  ", i));

				string ascii = "  ";
				for (int j = 0; j < 16; j++) {
					if ((i + j) < buffer.Count()) {
						displayBuffer.AppendText(String.Format("{0:X2} ", buffer[i + j]));
						char ch = Convert.ToChar(buffer[i + j]);
						ascii += Char.IsControl(ch) ? "." : ch.ToString();
					}
					else
						displayBuffer.AppendText(String.Format("   "));
				}	// one more line
				displayBuffer.AppendText(ascii);
				displayBuffer.AppendText(String.Format("\n"));
			}	// all lines
		}

	}
}
