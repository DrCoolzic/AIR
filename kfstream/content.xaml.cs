using KFStreamPackage;
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

namespace KFStream {
	/// <summary>
	/// Interaction logic for content.xaml
	/// </summary>
	public partial class ContentWindow : Window {
		public ContentWindow() {
			InitializeComponent();
		}


		/// <summary>
		/// Display the flux array for the specified revolution
		/// </summary>
		public void displayFlux(FluxData data, FluxDataRev[] dataRev, int rev) {

			displayBuffer.FontFamily = new FontFamily("Consolas");
			displayBuffer.FontSize = 16;
			displayBuffer.FontStyle = FontStyles.Normal;
			displayBuffer.FontWeight = FontWeights.Normal;

			displayBuffer.AppendText(String.Format("Revolution {0} has {1} transitions --- time {2} µs\n\n",
				rev + 1, dataRev[rev].fluxCount, dataRev[rev].revolutionTime / 1000));
			int firstFlux = dataRev[rev].firstFluxIndex;
			int lastFlux = firstFlux + dataRev[rev].fluxCount;
			for (int i = firstFlux; i < lastFlux; i += 16) {
				displayBuffer.AppendText(String.Format("{0:D5}  ", i - firstFlux));
				for (int j = 0; j < 16; j++) {
					if ((i + j) < lastFlux) {
						int flux = data.fluxValue[i + j];
						displayBuffer.AppendText(String.Format("{0:D4} ", flux));
					}
				}
				displayBuffer.AppendText(String.Format("\n"));
			}
		}
	}
}

