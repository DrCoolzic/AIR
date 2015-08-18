using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using KFStreamPackage;
using System.Globalization;

namespace KFStreamPackage {

	class KFWriter {
		double _sckValue;
		double _ickValue;
		TextBox _infoBox;

		private static void addShort(List<byte> buffer, short value) {
			buffer.Add((byte)value);
			buffer.Add((byte)(value >> 8));

		}

		private static void addInt(List<byte> buffer, int value) {
			buffer.Add((byte)value);
			buffer.Add((byte)(value >> 8));
			buffer.Add((byte)(value >> 16));
			buffer.Add((byte)(value >> 24));
		}

		private void writeStreamInfo(List<byte> buffer, int streamPosition, int transferTime) {
			buffer.Add((byte)BHeader.BHOOB);
			buffer.Add((byte)OOBType.OOBStreamInfo);
			KFWriter.addShort(buffer, 8);
			KFWriter.addInt(buffer, streamPosition);
			KFWriter.addInt(buffer, transferTime);
		}

		private void writeStreamIndex(List<byte> buffer, int streamPosition, int sampleCounter, int indexCounter) {
			buffer.Add((byte)BHeader.BHOOB);
			buffer.Add((byte)OOBType.OOBIndex);
			addShort(buffer, 12);
			addInt(buffer, streamPosition);
			addInt(buffer, sampleCounter);
			addInt(buffer, indexCounter);
		}

		private void writeStreamEnd(List<byte> buffer, int streamPosition, int resultCode) {
			buffer.Add((byte)BHeader.BHOOB);
			buffer.Add((byte)OOBType.OOBStreamEnd);
			addShort(buffer, 8);
			addInt(buffer, streamPosition);
			addInt(buffer, resultCode);
		}

		private void writeHardwareInfo(List<byte> buffer, string info) {
			buffer.Add((byte)BHeader.BHOOB);
			buffer.Add((byte)OOBType.OOBHWInfo);
			short size = (short)(info.Count() + 1);
			addShort(buffer, size);
			for (int i = 0; i < info.Count(); i++)
				buffer.Add((byte)info[i]);
			buffer.Add((byte)0);
		}

		private void writeStreamEOF(List<byte> buffer) {
			buffer.Add((byte)BHeader.BHOOB);
			buffer.Add((byte)OOBType.OOBEof);
			addShort(buffer, 0x0D0D);
		}

		/// <summary>
		/// The KFStream Writer Constructor
		/// </summary>
		/// <param name="tb">The TextBox used to display information</param>
		public KFWriter(TextBox tb) {
			// set clock default values
			_sckValue = ((18432000.0 * 73.0) / 14.0) / 4.0;
			_ickValue = _sckValue / 8;
			_infoBox = tb;
		}


		/// <summary>
		/// Write a KF raw file from a xxxx
		/// </summary>
		/// <param name="fileName">Name of the Pasti file to read</param>
		/// <param name="fd">the Floppy parameter</param>
		/// <returns>The status</returns>
		public bool writeStream(string fileName, FluxData data, FluxDataRev[] rev) {
			FileStream fs;

			// encode file into buffer
			List<byte> buffer = new List<byte>();
			bool status;
			status = encodeStream(buffer, data, rev);

			try {
				fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			}
			catch (Exception exc) {
				Console.WriteLine("Error: {0}", exc.Message);
				return false;
			}
			fs.Write(buffer.ToArray(), 0 , buffer.Count);
			fs.Close();

			return status;		
		}

		private bool encodeStream(List<byte> buffer, FluxData data, FluxDataRev[] revolutions) {
			int streampos = 0;
			int indexCounter = 0;

			string info = "sck=" + Convert.ToString(_sckValue, new CultureInfo("en-US")) +
				",ick=" + Convert.ToString(_ickValue, new CultureInfo("en-US"));
			writeHardwareInfo(buffer, info);

			for (int rev = 0; rev < revolutions.Count(); rev++) {
				writeStreamInfo(buffer, streampos, 0);				
				writeStreamIndex(buffer, streampos, 0, indexCounter);
				indexCounter += (int)(revolutions[rev].revolutionTime * _ickValue / 1000000000.0);

				for (int fluxIndex = revolutions[rev].firstFluxIndex;
					fluxIndex < revolutions[rev].firstFluxIndex + revolutions[rev].fluxCount; 
					fluxIndex++) {
					int flux = (int)(data.fluxValue[fluxIndex] * _sckValue / 1000000000.0);
					while (flux > 0x10000) {
						buffer.Add(0x0B);
						flux -= 0x10000;
						streampos++;
					}	// Ovl16 - flux > 0xFFFF
					if (flux > 0x7FF) {
						buffer.Add(0xC);
						buffer.Add((byte)(flux >> 8));
						buffer.Add((byte)flux);
						streampos += 3;
					}	// flux3 0x0800 - 0xFFFF
					else {
						if (flux > 0xFF) {
							buffer.Add((byte)(flux >> 8));
							buffer.Add((byte)flux);
							streampos += 2;
						}	// flux2 0x0100 - 0x07FF
						else {
							if (flux > 0x0D) {
								buffer.Add((byte)flux);
								streampos++;
							}	// flux1 0x0E - 0xFF
							else {
								buffer.Add(0);
								buffer.Add((byte)flux);
								streampos += 2;
							}	// flux2 0x00 - 0x0D
						}	// flux < 0x100
					}	// flux < 0x800
				}	// write all flux for this revolution
			}	// all revolutions

			writeStreamEnd(buffer, streampos, 0);
			writeStreamIndex(buffer, streampos, 0, indexCounter);
			writeStreamEOF(buffer);
			return true;
		}
	}
}
