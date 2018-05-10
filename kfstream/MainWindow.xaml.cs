using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using KFStreamPackage;

namespace KFStream {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		FluxData _fluxData;
		FluxDataRev[] _fluxDataRev;
		ContentWindow _contentWindow = null;
		bool _contentOpen = false;

		public MainWindow() {
			InitializeComponent();
			Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			DateTime dateTimeOfBuild = new DateTime(2000, 1, 1)
								+ new TimeSpan(version.Build, 0, 0, 0)
								+ TimeSpan.FromSeconds(version.Revision * 2);
			Title = "KF Reader/Writer " + version.Major + "." + version.Minor + " - " + dateTimeOfBuild;
		}


		private void btReadClick(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "KF Raw file|*.raw|All Files|*.*";
			bool? ok = ofd.ShowDialog();
			if (ok == true) {
				fileName.Text = ofd.FileName;
				processFile(ofd.FileName);
			}
		}


		private void btWriteClick(object sender, RoutedEventArgs e) {
			if (_fluxData == null) {
				tbStatus.Text = "Nothing to write!";
				return;
			}

			tbStatus.Clear();
			infoBox.Clear();

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "KF Raw file|*.raw|All Files|*.*";

			bool? ok = sfd.ShowDialog();
			if (ok == true) {
				fileName.Text = sfd.FileName;
				KFWriter kf = new KFWriter(infoBox);
				bool status = kf.writeStream(sfd.FileName, _fluxData, _fluxDataRev);
				infoBox.AppendText(String.Format("file {0} written {1}\n", sfd.FileName, status ? "OK" : " with errors"));
			}
		}


		private void processFile(string file) {
			tbStatus.Clear();
			infoBox.Clear();

			infoBox.AppendText(String.Format("Reading stream file: {0}\n", fileName));
			ProcessStream s = new ProcessStream();
			StreamStatus status = s.readStreamTrack(file, infoBox);
			infoBox.AppendText(String.Format("file read {0}\n", status == StreamStatus.sdsOk ? "correctly" : "incorrectly status = " + status.ToString()));
			if (status != StreamStatus.sdsOk) return;

			KFReader kfr = s.Reader;
			 _fluxData = s.Data;
			_fluxDataRev = s.Rev;

			double totalFlux = 0;
			for (int i = 0; i < _fluxData.totalFluxCount; i++)
				totalFlux += _fluxData.fluxValue[i];
			infoBox.AppendText(string.Format("\nFlux Count={0} - total time {1} ms for {2} revolutions\n",
					_fluxData.totalFluxCount, totalFlux / 1000, _fluxDataRev.Count()));

			double tick = 1000000.0 / kfr.SampleClock;
			infoBox.AppendText(string.Format("Min flux={0:f2} µs / Max flux={1:f2} µs\n",
					kfr.StreamStat.fluxMin * tick, kfr.StreamStat.fluxMax * tick));

			for (int rev = 0; rev < _fluxDataRev.Count(); rev++) {
				infoBox.AppendText(String.Format("   Rev {0} has {1} transitions - revolution time {2:F3} ms\n", rev, _fluxDataRev[rev].fluxCount, _fluxDataRev[rev].revolutionTime / 1000000.0));
			}
			
			infoBox.AppendText(string.Format("\nDisk Speed (min-avg-max):  {0:F3} - {1:F3} - {2:F3} RPM\n",
				kfr.StreamStat.minrpm, kfr.StreamStat.avgrpm, kfr.StreamStat.maxrpm));

			string hardware = kfr.InfoString;
			if (hardware != "") {
				string[] info = hardware.Split(',');
				infoBox.AppendText("\nHardware information \n");
				for (int i = 0; i < info.Count(); i++)
					infoBox.AppendText(string.Format("   {0}\n", info[i]));
			}
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

		private void btTransitions(object sender, RoutedEventArgs e) {
			int revNumber;
			if (_fluxData == null) {
				tbStatus.Text = "Nothing to display";
				return;
			}

			tbStatus.Clear();
			if (Int32.TryParse(tbRev.Text, out revNumber) == false) {
				tbStatus.Text = "Invalid track number - please correct and try again";
				return;
			}

			// test rev number
			if ((revNumber > _fluxDataRev.Count()) || (revNumber < 1)) {
				tbStatus.Text = "Not that many revolutions - please correct and try again";
				return;
			}

			if (_contentOpen)
				_contentWindow.Close();

			_contentWindow = new ContentWindow();
			_contentWindow.displayFlux(_fluxData, _fluxDataRev, revNumber - 1);
			_contentWindow.Closed += new EventHandler(contentWindowClosed);
			_contentOpen = true;
			_contentWindow.Show();
		}

		void contentWindowClosed(object sender, EventArgs e) {
			_contentOpen = false;
		}


	}
}
