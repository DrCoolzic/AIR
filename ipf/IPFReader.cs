/*!
@file IPFReader.cs
<summary>This file provide a function to parse a Pasti file in structures</summary>	

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;

namespace ipf {


	/// <summary>
	/// The reader class
	/// </summary>	
	public class IPFReader {
		TextBox _infoBox;
		CheckBox _dataElem;

		/// <summary>The IPF reader constructor</summary>
		/// <param name="tb">The textbox used to display information</param>
		public IPFReader(TextBox tb, CheckBox de) {
			_infoBox = tb;
			_dataElem = de;
		}


		/// <summary>
		/// Read an IPF file and fills the Floppy structure
		/// </summary>
		/// <param name="fileName">Name of the IPF file to read</param>
		/// <param name="fd">the Floppy parameter</param>
		/// <returns>The status</returns>
		public bool readIPF(string fileName, Floppy fd) {
			FileStream fs;
			try {
				fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (Exception exc) {
				Console.WriteLine("Error: {0}", exc.Message);
				return false;
			}

			// read file into buffer
			byte[] sbuf = new byte[fs.Length];
			int bytes = fs.Read(sbuf, 0, (int)fs.Length);
			Debug.Assert(bytes == fs.Length);	// should be same
			fs.Close();
			bool status = decodeIPF(sbuf, fd);
			return status;
		}


		/// <summary>
		/// Decode the information in an IPF file and fill the Floppy structure accordingly
		/// </summary>
		/// <param name="buffer">Contains the data read from IPF file</param>
		/// <param name="fd">The Floppy structure to fill</param>
		/// <returns>The read status</returns>
		private bool decodeIPF(byte[] buffer, Floppy fd) {
			uint currentPos = 0;
			uint startPos = 0;
			string recordType;
			uint crc = 0;

			fd.tracks = new Track[84, 2];

			InfoRecord info = new InfoRecord();
			RecordHeader header = new RecordHeader();
			Dictionary<uint, ImageRecord> images = new Dictionary<uint, ImageRecord>();

			_infoBox.Clear();
			_infoBox.FontFamily = new FontFamily("Consolas");
			_infoBox.FontSize = 14;
			_infoBox.ScrollToHome();

			while (currentPos < buffer.Count()) {
				// Read Record header
				startPos = currentPos;
				recordType = readHeader(header, buffer, ref currentPos);
				crc = Utilities.crc32Header(buffer, startPos, header.length);
				displayHeader(header, startPos, crc);
				if (crc != header.crc) return false;

				switch (recordType) {
					case "CAPS":						
						if (startPos != 0) {
							_infoBox.AppendText("IPF file error: CAPS record must be first");
							return false;
						}							
						break;

					case "INFO":
						if (startPos != 12) {
							_infoBox.AppendText("IPF file error: INFO record must be second");
							return false;
						}
						readInfoRecord(info, buffer, ref currentPos);
						fd.info = info;
						_infoBox.AppendText(info.ToString());
						break;

					case "IMGE":
						ImageRecord image = readImageRecord(buffer, ref currentPos, fd);
						images.Add(image.dataKey, image);
						_infoBox.AppendText(image.ToString());
						break;

					case "DATA":
						DataRecord data = readDataRecord(buffer, ref currentPos);
						crc = Utilities.crc32Buffer(buffer, currentPos, data.length);
						if (!images.ContainsKey(data.key)) {
							_infoBox.AppendText("IPF file error: No matching image key in Data record");
							return false;
						}
						ImageRecord img = images[data.key];
						_infoBox.AppendText(data.ToString());
						_infoBox.AppendText(String.Format(" {0} [T{1:D2}.{2}] - ({3}-{4})\n",
							(crc == data.crc) ? "Good" : "Error", img.track, img.side, currentPos, currentPos + data.length - 1));

						if (data.length > 0) {
							startPos = currentPos;
							readExtraDataSegment(buffer, currentPos, info.encoderType, img, fd);
							currentPos = startPos + data.length;
						}	// extra data segment
						break;

					case "CTEI":
						CteiRecord ctei = readCteiRecord(buffer, ref currentPos);
						_infoBox.AppendText(ctei.ToString());
						break;

					case "CTEX":
						CtexRecord ctex = readCtexRecord(buffer, ref currentPos);
						_infoBox.AppendText(ctex.ToString());
						break;

					default:
						displayHeader(header, startPos, crc);
						_infoBox.AppendText("IPF file error: Unknown record");
						return false;
				}

			}

			return true;
		}


		private static string readHeader(RecordHeader record, byte[] buffer, ref uint currentPos) {
			record.type[0] = readChar(buffer, ref currentPos);
			record.type[1] = readChar(buffer, ref currentPos);
			record.type[2] = readChar(buffer, ref currentPos);
			record.type[3] = readChar(buffer, ref currentPos);
			record.length = readUInt32(buffer, ref currentPos);
			record.crc = readUInt32(buffer, ref currentPos);
			return new string(record.type);
		}


		private void displayHeader(RecordHeader record, uint startPos, uint crc) {
			_infoBox.AppendText(record.ToString());
			_infoBox.AppendText(String.Format(" {0} - ({1}-{2})\n",
				(crc == record.crc) ? "Good" : "Error", startPos, startPos + record.length - 1));
		}


		private static void  readInfoRecord(InfoRecord info, byte[] buffer, ref uint bpos) {
			info.mediaType = (MediaType)readUInt32(buffer, ref bpos);
			info.encoderType = (EncoderType)readUInt32(buffer, ref bpos);
			info.encoderRev = readUInt32(buffer, ref bpos);
			info.fileKey = readUInt32(buffer, ref bpos);
			info.fileRev = readUInt32(buffer, ref bpos);
			info.origin = readUInt32(buffer, ref bpos);
			info.minTrack = readUInt32(buffer, ref bpos);
			info.maxTrack = readUInt32(buffer, ref bpos);
			info.minSide = readUInt32(buffer, ref bpos);
			info.maxSide = readUInt32(buffer, ref bpos);
			info.creationDate = readUInt32(buffer, ref bpos);
			info.creationTime = readUInt32(buffer, ref bpos);
			for (int i = 0; i < 4; i++)
				info.platforms[i] = (Platform)readUInt32(buffer, ref bpos);
			info.diskNumber = readUInt32(buffer, ref bpos);
			info.creatorId = readUInt32(buffer, ref bpos);
			for (int i = 0; i < 3; i++)
				info.reserved[i] = readUInt32(buffer, ref bpos);
		}


		private static ImageRecord readImageRecord(byte[] buffer, ref uint bpos, Floppy fd) {
			ImageRecord image = new ImageRecord();
			image.track = readUInt32(buffer, ref bpos);
			image.side = readUInt32(buffer, ref bpos);
			image.density = (Density)readUInt32(buffer, ref bpos);
			image.signalType = (SignalType)readUInt32(buffer, ref bpos);
			image.trackBytes = readUInt32(buffer, ref bpos);
			image.startBytePos = readUInt32(buffer, ref bpos);
			image.startBitPos = readUInt32(buffer, ref bpos);
			image.dataBits = readUInt32(buffer, ref bpos);
			image.gapBits = readUInt32(buffer, ref bpos);
			image.trackBits = readUInt32(buffer, ref bpos);
			image.blockCount = readUInt32(buffer, ref bpos);
			image.encoder = readUInt32(buffer, ref bpos);
			image.trackFlags = (TrackFlags)readUInt32(buffer, ref bpos);
			image.dataKey = readUInt32(buffer, ref bpos);
			for (int i = 0; i < 3; i++)
				image.reserved[i] = readUInt32(buffer, ref bpos);

			Debug.Assert(image.trackBits == (image.dataBits + image.gapBits), "Track bit does not equal dataBits + GapBits");
			Debug.Assert(image.startBytePos == (image.startBitPos / 8), "StratByte does not equal StratBit rounded");

			Track track = new Track();
			track.trackBytes = image.trackBytes;
			track.density = image.density;
			track.startBitPos = image.startBitPos;
			track.dataBits = image.dataBits;
			track.gapBits = image.gapBits;
			track.blockCount = image.blockCount;
			track.trackFlags = image.trackFlags;
			fd.tracks[image.track, image.side] = track;

			return image;
		}


		private static DataRecord readDataRecord(byte[] buffer, ref uint currentPos) {
			DataRecord data = new DataRecord();
			data.length = readUInt32(buffer, ref currentPos);
			data.bitSize = readUInt32(buffer, ref currentPos);
			data.crc = readUInt32(buffer, ref currentPos);
			data.key = readUInt32(buffer, ref currentPos);
			return data;
		}


		private static BlockDescriptor readBlockDescriptor(byte[] buffer, ref uint currentPos) {
			BlockDescriptor bd = new BlockDescriptor();
			bd.dataBits = readUInt32(buffer, ref currentPos);
			bd.gapBits = readUInt32(buffer, ref currentPos);
			bd.gapOffset = readUInt32(buffer, ref currentPos);
			bd.cellType = (SignalType)readUInt32(buffer, ref currentPos);
			bd.encoderType = (BlockEncoderType)readUInt32(buffer, ref currentPos);
			bd.blockFlags = (BlockFlags)readUInt32(buffer, ref currentPos);
			bd.gapDefaultValue = readUInt32(buffer, ref currentPos);
			bd.dataOffset = readUInt32(buffer, ref currentPos);
			return bd;
		}


		private void readExtraDataSegment(byte[] buffer, uint currentPos, EncoderType ipfEncoder, ImageRecord img, Floppy fd) {
			StringBuilder dataString = new StringBuilder();
			uint startDataArea = currentPos;
			// read block descriptors
			for (uint i = 0; i < img.blockCount; i++) {
				uint startPos = currentPos;
				BlockDescriptor bd = readBlockDescriptor(buffer, ref currentPos);

				Sector sector = new Sector();
				sector.dataBits = bd.dataBits;
				sector.gapBits = bd.gapBits;
				sector.flags = bd.blockFlags;

				string bdString = null;
				switch (ipfEncoder) {
					case EncoderType.CAPS:
						bdString = String.Format(" + Block={0} Data={1} ({2}) Gap={3} ({4}) {5} GapDef={6} DataOff={7:D4}",
							i, bd.dataBits, bd.dataBytes, bd.gapBits, bd.gapBytes, bd.encoderType, bd.gapDefaultValue, bd.dataOffset);
						// in caps mode we do not have gap element TODO
						break;
					case EncoderType.SPS:
						bdString = String.Format(" + Block={0} Data={1} Off={2} Gap={3} Off={4} {5} {6} GapDef={7} Flags={8}",
							i, bd.dataBits, bd.dataOffset, bd.gapBits, bd.gapOffset, bd.cellType.ToString(), bd.encoderType, bd.gapDefaultValue, bd.blockFlags);
						break;
				}
				dataString.Append(String.Format("{0} ({1}-{2})\n", bdString, startPos, currentPos - 1));
				
				// read GAP elements
				if ((ipfEncoder == EncoderType.SPS) && (bd.gapBits > 0)) {
					uint gapPos = startDataArea + bd.gapOffset;
					uint gapBitsSpecified = 0;
					byte gapHead;
					if (_dataElem.IsChecked == true)
						dataString.Append(String.Format("   --- GAP --- {0} bits @{1}\n",
							bd.gapBits, gapPos));

					if (bd.blockFlags.HasFlag(BlockFlags.FwGap)) {
						uint gapBytes = 0;
						while ((gapHead = buffer[gapPos++]) != 0) {
							uint startGapElemPos = gapPos - 1;
							GapElement gap;
							uint gapSize = 0;
							int gapSizeWidth = gapHead >> 5;
							GapElemType gapType = (GapElemType)(gapHead & 0x1F);
							for (int n = 0; n < gapSizeWidth; n++) {
								gapSize = (gapSize << 8) + buffer[gapPos++];
							}
							if (_dataElem.IsChecked == true)
								if (_dataElem.IsChecked == true)
									dataString.Append(String.Format("   Forward  GSW={0} {1} {2} bits",
										gapSizeWidth, gapType.ToString(), gapSize));
							if (gapType == GapElemType.SampleLength) {
								List<byte> gapSample = new List<byte>();
								for (int n = 0; n < gapSize / 8; n++)
									gapSample.Add(buffer[gapPos++]);
								if (_dataElem.IsChecked == true) dataString.Append(" Value=");
								foreach (byte b in gapSample)
									if (_dataElem.IsChecked == true)
										dataString.Append(String.Format("{0:X2} ", b));
								gap = new GapElement();
								gap.type = GapType.Forward;
								gap.gapBytes = gapBytes;
								gap.value = gapSample[0];
								sector.gapElems.Add(gap);
								gapBytes = 0;
							}
							else {
								gapBitsSpecified += gapSize;
								gapBytes = gapSize / 8;
							}
							if (_dataElem.IsChecked == true) dataString.Append(String.Format(" @{0}\n", startGapElemPos));							
						}						
					}	// forward

					if (bd.blockFlags.HasFlag(BlockFlags.BwGap)) {
						uint gapBytes = 0;
						while ((gapHead = buffer[gapPos++]) != 0) {
							uint startGapElemPos = gapPos - 1;
							GapElement gap;
							uint gapSize = 0;
							int gapSizeWidth = gapHead >> 5;
							GapElemType gapType = (GapElemType)(gapHead & 0x1F);
							for (int n = 0; n < gapSizeWidth; n++) {
								gapSize = (gapSize << 8) + buffer[gapPos++];
							}
							if (_dataElem.IsChecked == true) 
								dataString.Append(String.Format("   Backward GSW={0} {1} {2} bits",
									gapSizeWidth, gapType.ToString(), gapSize));
							if (gapType == GapElemType.SampleLength) {
								List<byte> gapSample = new List<byte>();
								for (int n = 0; n < gapSize / 8; n++)
									gapSample.Add(buffer[gapPos++]);
								if (_dataElem.IsChecked == true) dataString.Append(" Value=");	
								foreach (byte b in gapSample)
									if (_dataElem.IsChecked == true)
										dataString.Append(String.Format("{0:X2} ", b));
								gap = new GapElement();
								gap.type = GapType.Backward;
								gap.gapBytes = gapBytes;
								gap.value = gapSample[0];
								sector.gapElems.Add(gap);
								gapBytes = 0;
							}
							else {
								gapBytes = gapSize / 8;
								gapBitsSpecified += gapSize;								
							}
							if (_dataElem.IsChecked == true) dataString.Append(String.Format(" @{0}\n", startGapElemPos));						
						}
					}	// backward

					if (_dataElem.IsChecked == true) {
						if (gapBitsSpecified * 2 != bd.gapBits)
							dataString.Append(String.Format("   Incomplete gap specification {0} bits out of {1}\n",
								gapBitsSpecified * 2, bd.gapBits));
						else
							dataString.Append(String.Format("   complete gap specification {0} bits\n", bd.gapBits));
					}
				}	// GAP elements exist

				// read data elements
				if (bd.dataBits > 0) {
					uint dataPos = startDataArea + bd.dataOffset;
					byte dataHead;
					if (_dataElem.IsChecked == true)
						dataString.Append(String.Format("   --- DATA --- {0} bits @{1}\n",
							bd.dataBits, dataPos));
					while ((dataHead = buffer[dataPos++]) != 0) {
						DataElem data = new DataElem();
						uint dataSize = 0;
						int dataSizeWidth = dataHead >> 5;
						DataType dataType = (DataType)(dataHead & 0x1F);
						for (int n = 0; n < dataSizeWidth; n++) {
							dataSize = (dataSize << 8) + buffer[dataPos++];
						}

						if (bd.blockFlags.HasFlag(BlockFlags.DataInBit)) {
							if (_dataElem.IsChecked == true)
								dataString.Append(String.Format("   {0} DSW={1} sample {2} bits @{3}",
									dataType.ToString(), dataSizeWidth, dataSize, dataPos));
						}
						else {

							if (_dataElem.IsChecked == true) {
								dataString.Append(String.Format("   {0} DSW={1} sample {2} bytes @{3}",
									dataType.ToString(), dataSizeWidth, dataSize, dataPos));
							dataSize *= 8;							}
						}

						List<byte> dataSample = new List<byte>();
						for (int n = 0; n < dataSize / 8; n++)
							dataSample.Add(buffer[dataPos++]);
						foreach (byte b in dataSample)
							if (_dataElem.IsChecked == true) dataString.Append(String.Format(" {0:X2}", b));

						data.dataBytes = (uint)dataSample.Count;
						data.type = dataType;
						data.value = dataSample;
						sector.dataElems.Add(data);

						if (_dataElem.IsChecked == true) dataString.Append("\n");
					}	// data elem
				}	// data elem exist
				fd.tracks[img.track, img.side].sectors.Add(sector);
			}	// for all block descriptor
			_infoBox.AppendText(dataString.ToString());
		}


		private static CteiRecord readCteiRecord(byte[] buffer, ref uint bpos) {
			CteiRecord ctei = new CteiRecord();
			ctei.releaseCrc = readUInt32(buffer, ref bpos);
			ctei.analyzerRev = readUInt32(buffer, ref bpos);
			for (int i = 0; i < ctei.reserved.Count(); i++)
				ctei.reserved[i] = readUInt32(buffer, ref bpos);
			return ctei;
		}


		private static CtexRecord readCtexRecord(byte[] buffer, ref uint bpos) {
			CtexRecord ctex = new CtexRecord();
			ctex.track = readUInt32(buffer, ref bpos);
			ctex.side = readUInt32(buffer, ref bpos);
			ctex.density = (Density)readUInt32(buffer, ref bpos);
			ctex.formatId = readUInt32(buffer, ref bpos);
			ctex.fix = readUInt32(buffer, ref bpos);
			ctex.trackSize = readUInt32(buffer, ref bpos);
			for (int i = 0; i < ctex.reserved.Count(); i++)
				ctex.reserved[i] = readUInt32(buffer, ref bpos);
			return ctex;
		}


		private static uint readUInt32(byte[] buf, ref uint pos) {
			uint val = (uint)buf[pos + 3] + (uint)(buf[pos + 2] << 8) + (uint)(buf[pos + 1] << 16) + (uint)(buf[pos] << 24);
			pos += 4;
			return val;
		}


		private static char readChar(byte[] buf, ref uint pos) {
			return (char)buf[pos++];
		}

	}

}