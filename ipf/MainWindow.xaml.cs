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

namespace ipf {
	//http://www.thomaslevesque.com/2008/11/18/wpf-binding-to-application-settings-using-a-markup-extension/
	public class SettingBindingExtension : Binding {
		public SettingBindingExtension() {
			Initialize();
		}

		public SettingBindingExtension(string path)
			: base(path) {
			Initialize();
		}

		private void Initialize() {
			this.Source = ipf.Properties.Settings.Default;
			this.Mode = BindingMode.TwoWay;
		}
	}

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
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			string progName = "IPF File Reader / Writer " + version;
			Title = progName;
			//tbVersion.Text = progName;
		}



		private void btFileClick(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "IPF/IPX file|*.ipf;*ipx|All Files|*.*";
			bool? ok = ofd.ShowDialog();
			if (ok == true) {
				fileName.Text = ofd.FileName;
				processFile(ofd.FileName);
			}
			
		}


		private void processFile(string file) {
			IPFReader ipf = new IPFReader(infoBox, cbDataElem);
			_fd = new Floppy();
			ipf.readIPF(file, _fd);
		}

		private void btWriteClick(object sender, RoutedEventArgs e) {
			if (_fd == null) {
				MessageBox.Show("Nothing to write", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "IPF file|*.ipf|All Files|*.*";

			bool? ok = sfd.ShowDialog();
			if (ok == true) {
				fileName.Text = sfd.FileName;
				//if (File.Exists(sfd.FileName)) {
				//	MessageBoxResult result = MessageBox.Show("Do you want to overwrite?", "File already exist", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No); 
				//	if (result == MessageBoxResult.Cancel) return;
				//	if (result == MessageBoxResult.No) goto askOutput;					
				//}				
				IPFWriter ipf = new IPFWriter(infoBox);
				ipf.writeIPF(sfd.FileName, _fd);
			}

		}


		private void btBlocksClick(object sender, RoutedEventArgs e) {
			if (_fd == null) {
				tbStatus.Text = "Nothing to display";
				return;
			}
			int trackNumber;
			int sideNumber;
			tbStatus.Clear();
			if (Int32.TryParse(tbTrack.Text, out trackNumber) == false) {
				tbStatus.Text = "Invalid track number - please correct and try again";
				return;
			}
			if (Int32.TryParse(tbSide.Text, out sideNumber) == false) {
				tbStatus.Text = "Invalid side number - please correct and try again";
				return;
			}
			if ((trackNumber < 0) || (trackNumber > 84) || (sideNumber < 0) || (sideNumber > 1)) {
				tbStatus.Text = "Track or Side out of range - please correct and try again";
				return;
			}

			if (_sectorWindowOpen)
				_sectorWindow.Close();

			_sectorWindow = new BufferWindow();
			_sectorWindow.displayBlocksBuffer(_fd, trackNumber, sideNumber);
			_sectorWindow.Closed += new EventHandler(sectorWindowClosed);
			_sectorWindowOpen = true;
			_sectorWindow.Show();

		}


		void sectorWindowClosed(object sender, EventArgs e) {
			_sectorWindowOpen = false;
		}


		private void mainWinClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (_sectorWindowOpen) _sectorWindow.Close();
			if (_trackWindowOpen) _trackWindow.Close();
		}


		private void fileDropped(object sender, DragEventArgs e) {
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
