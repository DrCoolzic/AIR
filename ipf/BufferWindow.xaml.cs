using System;
using System.Collections.Generic;
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

namespace ipf {
	/// <summary>
	/// Interaction logic for BufferWindow.xaml
	/// </summary>
	public partial class BufferWindow : Window {
		public BufferWindow() {
			InitializeComponent();
		}

		/// <summary>
		/// Draw the content of the buffer
		/// </summary>
		/// <param name="floppy">Pointer to the floppy disk structure</param>
		/// <param name="track">The track number of the track to display</param>
		/// <param name="side">The side of the track to display</param>
		public void displayBlocksBuffer(Floppy floppy, int track, int side) {
			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;

			Track t = floppy.tracks[track, side];
			displayBuffer.AppendText(String.Format("Track {0:D2}.{1} has {2} blocks\n", track, side, t.blockCount));
			uint trackGapSum = 0;
			uint trackDataSum = 0;

			for (int sect = 0; sect < t.blockCount; sect++) {
				uint gapSum = 0;
				uint dataSum = 0;
				Sector s = t.sectors[sect];
				foreach (GapElement gap in s.gapElems)
					gapSum += gap.gapBytes;
				foreach (DataElem data in s.dataElems)
					dataSum += (data.type == DataType.Sync) ? (data.dataBytes / 2) : data.dataBytes;
				displayBuffer.AppendText(String.Format("Sector {0} Total={1} bytes (Gap={2} Data={3}) in Block Descriptor: (Gap={4} Data={5})\n", 
					sect, gapSum + dataSum, gapSum, dataSum, s.gapBits * 8, s.dataBits * 8));

				foreach (GapElement gap in s.gapElems) {
					displayBuffer.AppendText(String.Format("   - {0} Gap {1} Value={2:X2}\n",
						(gap.type == GapType.Forward) ? "Forward" : "Backward", (gap.gapBytes == 0) ? "Repeat" : gap.gapBytes.ToString() + " bytes", gap.value));
				}
				if (gapSum != (s.gapBits * 8))
					displayBuffer.AppendText(String.Format("   = Gap filling required: only {0} bytes specified out of {1}\n", gapSum, s.gapBits * 8));

				foreach (DataElem data in s.dataElems) {
					displayBuffer.AppendText(String.Format("   + {0} {1} bytes\n",
						data.type.ToString(), (data.type == DataType.Sync) ? (data.dataBytes / 2) : data.dataBytes));
					drawBuffer(data.value, data.type == DataType.Sync);
				}
				trackDataSum += dataSum;
				trackGapSum += gapSum;
			}
			displayBuffer.AppendText(String.Format("Track {0:D2}.{1} Specified Bytes={2} ({3}) Data={4} ({5}) Gap={6} ({7})\n", track, side, 
				trackDataSum + trackGapSum, (trackDataSum+trackGapSum)*8, trackDataSum, trackDataSum*8, trackGapSum, trackGapSum*8));
		}




		/// <summary>
		/// Display the content of	 TrackBuffer buffer in the displayBuffer TextBox
		/// </summary>
		/// <param name="buffer">The buffer to display</param>
		public void drawBuffer(List<byte> buffer, bool sync) {
			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;

			for (int i = 0; i < buffer.Count(); i += 16) {
				displayBuffer.AppendText(String.Format("   {0:D4}  ", i));

				string ascii = "  ";
				for (int j = 0; j < 16; j++) {
					if ((i + j) < buffer.Count()) {
						displayBuffer.AppendText(String.Format("{0:X2}{1}", buffer[i + j], (sync && (j % 2 == 0)) ? "" : " "));
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
