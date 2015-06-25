using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ipf {
	class IPFWriter {
		private TextBox _infoBox;

		/// <summary>
		/// The IPF Writer Constructor
		/// </summary>
		/// <param name="tb">The textbox used to display information</param>
		public IPFWriter(TextBox tb) {
			_infoBox = tb;
		}


		/// <summary>
		/// Write an IPF file from the Floppy structure
		/// </summary>
		/// <param name="fileName">Name of the Pasti file to read</param>
		/// <param name="fd">the Floppy parameter</param>
		/// <returns>The status</returns>
		public bool writeIPF(string fileName, Floppy fd) {
			FileStream fs;

			// encode file into buffer
			List<byte> buffer = new List<byte>();
			bool status = encodeIPF(buffer, fd);

			try {
				fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			}
			catch (Exception exc) {
				Console.WriteLine("Error: {0}", exc.Message);
				return false;
			}
			fs.Write(buffer.ToArray(), 0, buffer.Count);
			fs.Close();

			return status;
		}

		private bool encodeIPF(List<byte> buffer, Floppy fd) {
			uint crc;
			int start;
			int crc_pos;

			// write CAPS Record
			crc = 0;
			start = buffer.Count;
			writeByte(buffer, (byte)'C');			// File header
			writeByte(buffer, (byte)'A');
			writeByte(buffer, (byte)'P');
			writeByte(buffer, (byte)'S');
			writeInt32(buffer, 12);
			crc_pos = buffer.Count;
			writeInt32(buffer, 0);
			for (int i = start; i < buffer.Count; i++)
				crc = Utilities.crc32(crc, buffer[i]);
			WriteCRC(buffer, crc_pos, crc);

			DateTime now = DateTime.Now;
			int date = now.Year * 10000 + now.Month * 100 + now.Day;
			int time = now.Hour * 10000000 + now.Minute * 100000 + now.Second * 1000 + now.Millisecond;

			InfoRecord info = new InfoRecord();
			info.mediaType = MediaType.Floppy_Disk;
			info.encoderType = EncoderType.SPS;
			info.encoderRev = 1;
			info.fileKey = 0;
			info.fileRev = 1;
			info.origin = 0;
			info.minTrack = 0;
			info.maxTrack = 83;
			info.minSide = 0;
			info.maxSide = 1;
			info.creationDate = (uint)date;
			info.creationTime = (uint)time;
			info.platforms = new Platform[4];
			info.platforms[0] = Platform.Atari_ST;
			info.diskNumber = 1;
			info.creatorId = 0xAFAFAFAF;
			info.reserved = new uint[3];

			// Write INFO Record
			start = buffer.Count;
			writeByte(buffer, (byte)'I');			// File header
			writeByte(buffer, (byte)'N');
			writeByte(buffer, (byte)'F');
			writeByte(buffer, (byte)'O');
			writeInt32(buffer, 96);
			crc_pos = buffer.Count;
			writeInt32(buffer, 0);

			writeInt32(buffer, (uint)info.mediaType);
			writeInt32(buffer, (uint)info.encoderType);
			writeInt32(buffer, info.encoderRev);
			writeInt32(buffer, info.fileKey);
			writeInt32(buffer, info.fileRev);
			writeInt32(buffer, info.origin);
			writeInt32(buffer, info.minTrack);
			writeInt32(buffer, info.maxTrack);
			writeInt32(buffer, info.minSide);
			writeInt32(buffer, info.maxSide);
			writeInt32(buffer, info.creationDate);
			writeInt32(buffer, info.creationTime);
			for (int i = 0; i < 4; i++)
				writeInt32(buffer, (uint)info.platforms[i]);
			writeInt32(buffer, info.diskNumber);
			writeInt32(buffer, info.creatorId);
			for (int i = 0; i < 3; i++)
				writeInt32(buffer, info.reserved[i]);

			crc = 0;
			for (int i = start; i < buffer.Count; i++)
				crc = Utilities.crc32(crc, buffer[i]);
			WriteCRC(buffer, crc_pos, crc);

			uint dataKey = 1;
			for (uint track = 0; track < 84; track++)
				for (uint side = 0; side < 2; side++) {
					Track t = fd.tracks[track, side];

					ImageRecord image = new ImageRecord();
					image.track = track;
					image.side = side;
					image.density = t.density;
					image.signalType = SignalType.cell_2us;
					image.trackBytes = t.trackBytes * 2;
					image.startBytePos = t.startPos * 2;
					image.startBitPos = t.startPos * 16;
					image.dataBits = t.dataBytes * 16;
					image.gapBits = t.gapBytes * 16;
					image.trackBits = t.trackBytes * 16;
					image.blockCount = t.blockCount;
					image.encoder = 0;
					image.trackFlags = t.trackFlags;
					image.dataKey = dataKey++;
					image.reserved = new uint[4];

					// Write IMGE Record
					start = buffer.Count;
					writeByte(buffer, (byte)'I');			// File header
					writeByte(buffer, (byte)'M');
					writeByte(buffer, (byte)'G');
					writeByte(buffer, (byte)'E');
					writeInt32(buffer, 80);
					crc_pos = buffer.Count;
					writeInt32(buffer, 0);

					writeInt32(buffer, image.track);
					writeInt32(buffer, image.side);
					writeInt32(buffer, (uint)image.density);
					writeInt32(buffer, (uint)image.signalType);
					writeInt32(buffer, image.trackBytes);
					writeInt32(buffer, image.startBytePos);
					writeInt32(buffer, image.startBitPos);
					writeInt32(buffer, image.dataBits);
					writeInt32(buffer, image.gapBits);
					writeInt32(buffer, image.trackBits);
					writeInt32(buffer, image.blockCount);
					writeInt32(buffer, image.encoder);
					writeInt32(buffer, (uint)image.trackFlags);
					writeInt32(buffer, image.dataKey);
					for (int i = 0; i < 3; i++)
						writeInt32(buffer, image.reserved[i]);

					crc = 0;
					for (int i = start; i < buffer.Count; i++)
						crc = Utilities.crc32(crc, buffer[i]);
					WriteCRC(buffer, crc_pos, crc);
				}

			return true;
		}

		private static void WriteCRC(List<byte> buffer, int crc_pos, uint crc) {
			buffer[crc_pos] = (byte)((crc >> 24) & 0xFF);
			buffer[crc_pos + 1] = (byte)((crc >> 16) & 0xFF);
			buffer[crc_pos + 2] = (byte)((crc >> 8) & 0xFF);
			buffer[crc_pos + 3] = (byte)(crc & 0xFF);
		}

		private static void writeInt32(List<byte> buffer, uint val) {
			buffer.Add((byte)((val >> 24) & 0xFF));
			buffer.Add((byte)((val >> 16) & 0xFF));
			buffer.Add((byte)((val >> 8) & 0xFF));
			buffer.Add((byte)(val & 0xFF));
		}


		//private static void writeInt16(List<byte> buffer, ushort val) {
		//	buffer.Add((byte)(val & 0xFF));
		//	buffer.Add((byte)(val >> 8));
		//}

		private static void writeByte(List<byte> buffer, byte val) {
			buffer.Add(val);
		}



	}
}
