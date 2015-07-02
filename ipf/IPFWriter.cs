using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		/// <param name="tb">The TextBox used to display information</param>
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
			start = buffer.Count;
			writeByte(buffer, (byte)'C');			// File header
			writeByte(buffer, (byte)'A');
			writeByte(buffer, (byte)'P');
			writeByte(buffer, (byte)'S');
			writeInt32(buffer, 12);					// record length
			crc_pos = buffer.Count;
			writeInt32(buffer, 0);					// CRC

			crc = Utilities.crc32Buffer(buffer, 0, 12);
			writeInt32(buffer, crc_pos, crc);

			// in real life creating an info record may look like the commented code below

			//DateTime now = DateTime.Now;
			//int date = now.Year * 10000 + now.Month * 100 + now.Day;
			//int time = now.Hour * 10000000 + now.Minute * 100000 + now.Second * 1000 + now.Millisecond;

			//InfoRecord info = new InfoRecord();
			//info.mediaType = MediaType.Floppy_Disk;
			//info.encoderType = EncoderType.SPS;
			//info.encoderRev = 1;
			//info.fileKey = 0;
			//info.fileRev = 1;
			//info.origin = 0;
			//info.minTrack = 0;
			//info.maxTrack = 83;
			//info.minSide = 0;
			//info.maxSide = 1;
			//info.creationDate = (uint)date;
			//info.creationTime = (uint)time;
			//info.platforms = new Platform[4];
			//info.platforms[0] = Platform.Atari_ST;
			//info.diskNumber = 1;
			//info.creatorId = 0xAFAFAFAF;
			//info.reserved = new uint[3];

			// but to compare with original ipf file we just reuse the same content
			InfoRecord info = fd.info;

			// Write INFO Record
			start = buffer.Count;
			writeByte(buffer, (byte)'I');			// File header
			writeByte(buffer, (byte)'N');
			writeByte(buffer, (byte)'F');
			writeByte(buffer, (byte)'O');
			writeInt32(buffer, 96);					// record length
			crc_pos = buffer.Count;
			writeInt32(buffer, 0);					// CRC

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

			crc = Utilities.crc32Buffer(buffer, start, 96);
			writeInt32(buffer, crc_pos, crc);

			// write IMGE record
			uint dataKey = 1;
			for (uint track = 0; track < 84; track++) {
				for (uint side = 0; side < 2; side++) {
					Track t = fd.tracks[track, side];

					ImageRecord image = new ImageRecord();
					image.track = track;
					image.side = side;
					image.density = t.density;
					image.signalType = SignalType.cell_2us;
					image.trackBytes = t.trackBytes;
					image.startBytePos = t.startBitPos / 8;
					image.startBitPos = t.startBitPos;
					image.dataBits = t.dataBits;
					image.gapBits = t.gapBits;
					image.trackBits = t.dataBits + t.gapBits;
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

					crc = Utilities.crc32Buffer(buffer, start, 80);
					writeInt32(buffer, crc_pos, crc);
				}
			}

			dataKey = 1;
			for (uint track = 0; track < 84; track++) {
				for (uint side = 0; side < 2; side++) {
					Track t = fd.tracks[track, side];

					start = buffer.Count;
					writeByte(buffer, (byte)'D');			// File header
					writeByte(buffer, (byte)'A');
					writeByte(buffer, (byte)'T');
					writeByte(buffer, (byte)'A');
					writeInt32(buffer, 28);					// record length
					crc_pos = buffer.Count;
					writeInt32(buffer, 0);					// CRC

					// here we build the Extra DATA Block buffer
					List<byte> extraDataBlock = new List<byte>();
					
					// we first write the block descriptors
					foreach (Sector sector in t.sectors) {
						BlockDescriptor bd = new BlockDescriptor();
						bd.dataBits = sector.dataBits;
						bd.gapBits = sector.gapBits;
						bd.gapOffset = 0;
						bd.cellType = SignalType.cell_2us;
						bd.encoderType = BlockEncoderType.MFM;
						bd.blockFlags = sector.flags;
						bd.gapDefaultValue = 0;
						bd.dataOffset = 0;

						writeInt32(extraDataBlock, bd.dataBits);
						writeInt32(extraDataBlock, bd.gapBits);
						writeInt32(extraDataBlock, bd.gapOffset);
						writeInt32(extraDataBlock, (uint)bd.cellType);
						writeInt32(extraDataBlock, (uint)bd.encoderType);
						writeInt32(extraDataBlock, (uint)bd.blockFlags);
						writeInt32(extraDataBlock, bd.gapDefaultValue);
						writeInt32(extraDataBlock, bd.dataOffset);
					}

					// then we write the set of Gap stream elements	
					int gapOffset = t.sectors.Count * 32;
					int secNum = 0;
					foreach (Sector sector in t.sectors) {

						// correction of gapOffset in block descriptor
						if (!sector.flags.Equals(BlockFlags.None))
							writeInt32(extraDataBlock, (secNum * 32) + 8, (uint)gapOffset);

						if (sector.flags.HasFlag(BlockFlags.FwGap)) {
							foreach (GapElement ge in sector.gapElems) {
								if (ge.type != GapType.Forward) continue;
								writeGapElem(extraDataBlock, ge.gapBytes, ge.value);
								gapOffset += (ge.gapBytes == 0) ? 3 : ((ge.gapBytes * 8) < 256) ? 5 : 6;
							}
							writeByte(extraDataBlock, 0);
							gapOffset++;
						}

						if (sector.flags.HasFlag(BlockFlags.BwGap)) {
							foreach (GapElement ge in sector.gapElems) {
								if (ge.type != GapType.Backward) continue;
								writeGapElem(extraDataBlock, ge.gapBytes, ge.value);
								gapOffset += (ge.gapBytes == 0) ? 3 : ((ge.gapBytes * 8) < 256) ? 5 : 6;
							}
							writeByte(extraDataBlock, 0);
							gapOffset++;
						}

						secNum++;
					}

					// then we write the set of Data stream elements
					//uint dataOffset = (uint)(t.sectors.Count * 32) + gapOffset;
					int dataOffset = gapOffset;
					secNum = 0;
					foreach (Sector sector in t.sectors) {
						// correction of dataOffset in block descriptor
						writeInt32(extraDataBlock, (secNum * 32) + 28, (uint)dataOffset);

						foreach (DataElem de in sector.dataElems) {
							writeDataElem(extraDataBlock, de.type, de.dataBytes, de.value);
							dataOffset += ((de.dataBytes < 255) ? 2 : 3) + (int)de.dataBytes;
						}
						writeByte(extraDataBlock, 0);
						dataOffset++;
						secNum++;
					}

					// now we have the complete extra data segment 
					// we can write the end of the Data record
					DataRecord data = new DataRecord();
					data.length = (uint)extraDataBlock.Count;
					data.bitSize = data.length * 8;
					data.crc = Utilities.crc32Buffer(extraDataBlock, 0, (int)data.length);
					data.key = dataKey++;

					writeInt32(buffer, data.length);
					writeInt32(buffer, data.bitSize);
					writeInt32(buffer, data.crc);
					writeInt32(buffer, data.key);

					crc = Utilities.crc32Buffer(buffer, start, 28);
					writeInt32(buffer, crc_pos, crc);

					// and now copy the extra segment to the buffer
					foreach (byte b in extraDataBlock)
						buffer.Add(b);

				}	// all sides
			}	// all tracks

			return true;
		}


		private void writeGapElem(List<byte> buffer, uint byteCount, byte value) {
			uint bitCount = byteCount * 8;
			// write GAP length
			if (bitCount > 0) {
				if (bitCount < 256) {
					writeByte(buffer, 0x21);	// size=1 & type=1
					writeByte(buffer, (byte)bitCount);
				}
				else {
					writeByte(buffer, 0x41);	// size=2 & type=1
					writeByte(buffer, (byte)(bitCount / 256));
					writeByte(buffer, (byte)(bitCount & 0xFF));
				}
			}
			// Write GAP sample value
			writeByte(buffer, 0x22);	// size=1 & type=2
			writeByte(buffer, 8);		// sample size
			writeByte(buffer, value);
		}


		private void writeDataElem(List<byte> buffer, DataType type, uint byteCount, List<byte> value) {
			byte dataHead = (byte)((int)type + ((byteCount < 256) ? 0x20 : 0x40));
			writeByte(buffer, dataHead);
			if (byteCount < 256) {
				writeByte(buffer, (byte)byteCount);
			}
			else {
				writeByte(buffer, (byte)(byteCount / 256));
				writeByte(buffer, (byte)(byteCount & 0xFF));
			}
			// Write value
			Debug.Assert(byteCount == value.Count, "Lenght error in dataElem");
			for (int i = 0; i < byteCount; i++) {
				writeByte(buffer, value[i]);
			}
		}


		private static void writeInt32(List<byte> buffer, int buf_position, uint value) {
			buffer[buf_position] = (byte)((value >> 24) & 0xFF);
			buffer[buf_position + 1] = (byte)((value >> 16) & 0xFF);
			buffer[buf_position + 2] = (byte)((value >> 8) & 0xFF);
			buffer[buf_position + 3] = (byte)(value & 0xFF);
		}


		private static void writeInt32(List<byte> buffer, uint val) {
			buffer.Add((byte)((val >> 24) & 0xFF));
			buffer.Add((byte)((val >> 16) & 0xFF));
			buffer.Add((byte)((val >> 8) & 0xFF));
			buffer.Add((byte)(val & 0xFF));
		}


		private static void writeByte(List<byte> buffer, byte val) {
			buffer.Add(val);
		}

	}
}
