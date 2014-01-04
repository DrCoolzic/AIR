/*!
@file PastiRead.cs
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
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pasti {
	/// <summary>
	/// The reader class
	/// </summary>
	public static class PastiReader {
		/// <summary>
		/// Status returned when reading Pasti file
		/// </summary>
		public enum PastiStatus {
			/// <summary>File read correctly</summary>
			Ok = 0,
			/// <summary>Could not open file for reading or file not found</summary>
			FileNotFound,
			/// <summary>The header in the file is not RSY</summary>
			NotPastiFile,
			/// <summary>Not version 3</summary>
			UnsupportedVersion
		}


		/// <summary>Contains information about one sector</summary>
		/// <remarks>This class is used to store all information required to read or write a specific sector</remarks>
		public class Sector {
			/// <summary>buffer for the sector data</summary> 
			public byte[] data;
			/// <summary>buffer for fuzzy mask bytes if necessary</summary> 
			public byte[] fuzzy;
			/// <summary>buffer for timing bytes if necessary</summary>
			public ushort[] timing;
			/// <summary>position in the track of the sector address field in bits</summary>
			public ushort bitPosition;
			/// <summary>read time of the track in ms</summary>
			public ushort readTime;
			/// <summary>Address field content</summary>
			public Address address = new Address();
			/// <summary>Status returned by the FDC</summary>
			public byte fdcFlags;
			/// <summary></summary>
		}


		/// <summary>Contains information about one Track</summary>
		public class Track {
			/// <summary>Array of Sectors</summary>
			public Sector[] sectors;
			/// <summary>Number of sectors for this track</summary>
			public uint sectorCount;
			/// <summary>buffer for the track data if necessary</summary>
			public byte[] trackData;
			/// <summary>First sync byte offset: This is the offset in byte of the first 0xA1 sync byte found in the track.</summary> 
			/// <remarks>This is usually the sync byte in front of the first address field.</remarks>
			public ushort firstSyncOffset;
			/// <summary>Number of bytes in the track</summary>
			public uint byteCount;
			/// <summary>track number</summary>
			public uint number;
			/// <summary>track side</summary>
			public uint side;
		}


		/// <summary>Contains information about a complete Floppy disk</summary>
		/// <remarks>Complete floppy information (i.e. all tracks)</remarks>
		public class Floppy {
			/// <summary>Array of Track</summary>
			public Track[] tracks;
			/// <summary>number of tracks</summary>
			public uint trackCount;
			/// <summary>Pasti version</summary>
			public uint version;
			/// <summary>Pasti imaging tool</summary>
			public uint tool;
		}

		/// <summary>
		/// Read a Pasti file and fills the Floppy structure
		/// </summary>
		/// <param name="fileName">Name of the Pasti file to read</param>
		/// <param name="fd">the Floppy parameter</param>
		/// <returns>The status</returns>
		public static PastiStatus readPasti(string fileName, Floppy fd, TextBox info) {
			FileStream fs;
			try {
				fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (Exception exc) {
				Console.WriteLine("Error: {0}", exc.Message);
				return PastiStatus.FileNotFound;
			}

			// read file into buffer
			byte[] sbuf = new byte[fs.Length];
			int bytes = fs.Read(sbuf, 0, (int)fs.Length);
			Debug.Assert(bytes == fs.Length);	// should be same
			fs.Close();
			PastiStatus status = decodePasti(sbuf, fd, info);
			return status;
		}


		/// <summary>
		/// Decode the information in a Pasti file and fill the Floppy structure accordingly
		/// </summary>
		/// <param name="buffer">Contains the data read from Pasti file</param>
		/// <param name="fd">The Floppy structure to fill</param>
		/// <returns>The read status</returns>
		private static PastiStatus decodePasti(byte[] buffer, Floppy fd, TextBox info) {
			uint bpos = 0;
			uint startPos = 0;
			uint maxBufPos = 0;

			FileDesc file = new FileDesc();
			startPos = bpos;
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			if (file.pastiFileId != "RSY\0")
				return PastiStatus.NotPastiFile;

			file.version = readInt16(buffer, ref bpos);
			if (file.version != 3)
				return PastiStatus.UnsupportedVersion;

			file.tool = readInt16(buffer, ref bpos);
			file.reserved_1 = readInt16(buffer, ref bpos);
			file.trackCount = readByte(buffer, ref bpos);
			file.revision = readByte(buffer, ref bpos);
			file.reserved_2 = readInt32(buffer, ref bpos);

			info.Clear();
			info.FontFamily = new FontFamily("Consolas");
			info.FontSize = 14;
			info.AppendText(file.ToString());
			info.AppendText(String.Format(" - ({0}-{1})\n", startPos, bpos));
			info.ScrollToHome();

			fd.trackCount = file.trackCount; // store the number of tracks
			fd.version = (uint)(file.version * 10 + file.revision);
			fd.tool = file.tool;
			fd.tracks = new Track[fd.trackCount]; // create an array of track
						
			// read all track records
			TrackDesc track = new TrackDesc();
			for (int tnum = 0; tnum < fd.trackCount; tnum++) {
				uint track_record_start = bpos;
				track.recordSize = readInt32(buffer, ref bpos);
				track.fuzzyCount = readInt32(buffer, ref bpos);
				track.sectorCount = readInt16(buffer, ref bpos);
				track.trackFlags = readInt16(buffer, ref bpos);
				track.trackLength = readInt16(buffer, ref bpos);
				track.trackNumber = readByte(buffer, ref bpos);
				track.trackType = readByte(buffer, ref bpos);
				maxBufPos = bpos;

				info.AppendText(track.ToString());
				info.AppendText(String.Format(" ({0}-{1})\n", track_record_start, bpos));

				fd.tracks[tnum] = new Track();
				// create array of sector for this track
				fd.tracks[tnum].sectors = new Sector[track.sectorCount];
				fd.tracks[tnum].sectorCount = track.sectorCount;
				fd.tracks[tnum].byteCount = track.trackLength;
				fd.tracks[tnum].side = (uint)(track.trackNumber >> 7);
				fd.tracks[tnum].number = (uint)(track.trackNumber & 0x7F);

				// read sector descriptors if specified
				if ((track.trackFlags & TrackDesc.TRK_SECT) != 0) {
					SectorDesc[] sectors = new SectorDesc[track.sectorCount];
					for (int snum = 0; snum < track.sectorCount; snum++) {
						// read sector descriptor
						sectors[snum] = new SectorDesc();
						startPos = bpos;
						sectors[snum].dataOffset = readInt32(buffer, ref bpos);
						sectors[snum].bitPosition = readInt16(buffer, ref bpos);
						sectors[snum].readTime = readInt16(buffer, ref bpos);
						sectors[snum].address.track = readByte(buffer, ref bpos);
						sectors[snum].address.head = readByte(buffer, ref bpos);
						sectors[snum].address.number = readByte(buffer, ref bpos);
						sectors[snum].address.size = readByte(buffer, ref bpos);
						sectors[snum].address.crc = readInt16(buffer, ref bpos);
						sectors[snum].fdcFlags = readByte(buffer, ref bpos);
						sectors[snum].reserved = readByte(buffer, ref bpos);

						info.AppendText(sectors[snum].ToString());
						info.AppendText(String.Format(" ({0}-{1})\n", startPos, bpos));

						fd.tracks[tnum].sectors[snum] = new Sector();
						fd.tracks[tnum].sectors[snum].fdcFlags = sectors[snum].fdcFlags;
						fd.tracks[tnum].sectors[snum].bitPosition = sectors[snum].bitPosition;
						fd.tracks[tnum].sectors[snum].readTime = sectors[snum].readTime;
						fd.tracks[tnum].sectors[snum].address = sectors[snum].address;
						fd.tracks[tnum].sectors[snum].fdcFlags = sectors[snum].fdcFlags;
					} // for all sectors
 						
					byte[] fuzzyMask = null;
					// if necessary read fuzzy bytes
					if (track.fuzzyCount != 0) {
						startPos = bpos;
						fuzzyMask = new byte[track.fuzzyCount];
						for (int i = 0; i < track.fuzzyCount; i++)
							fuzzyMask[i] = readByte(buffer, ref bpos);
						info.AppendText(String.Format("   Reading Fuzzy {0} bytes ({1}-{2})\n", track.fuzzyCount, startPos, bpos));
					}

					// the track data are located after the sector descriptor records & fuzzy record
					uint track_data_start = bpos; // store the position of data in track record
					info.AppendText(String.Format("   Start of Track data {0}\n", track_data_start));

					// read track data if specified
					if ((track.trackFlags & TrackDesc.TRK_IMAGE) != 0) {
						// track with sync offset
						startPos = bpos;
						if ((track.trackFlags & TrackDesc.TRK_SYNC) != 0)
							fd.tracks[tnum].firstSyncOffset = readInt16(buffer, ref bpos);
						else
							fd.tracks[tnum].firstSyncOffset = 0;
						
						fd.tracks[tnum].byteCount = readInt16(buffer, ref bpos);
						// read track data
						fd.tracks[tnum].trackData = new byte[fd.tracks[tnum].byteCount];
						for (int i = 0; i < fd.tracks[tnum].byteCount; i++)
							fd.tracks[tnum].trackData[i] = readByte(buffer, ref bpos);
						maxBufPos = Math.Max(maxBufPos, bpos);
						info.AppendText(String.Format("      Reading Track {0} bytes SyncPos={1} ({2}-{3})\n", fd.tracks[tnum].byteCount, fd.tracks[tnum].firstSyncOffset, startPos, bpos));
					} // read track_data

					// read all sectors data
					bool bitWidthFound = false;
					for (int snum = 0; snum < track.sectorCount; snum++) {
						if (((sectors[snum].fdcFlags & SectorDesc.BIT_WIDTH) != 0) &&  (file.revision == 2))
							bitWidthFound = true;

						// create sector buffer and read info if necessary
						if ( (sectors[snum].fdcFlags & SectorDesc.SECT_RNF) == 0) {
							fd.tracks[tnum].sectors[snum].data = new byte[128 << sectors[snum].address.size];
							bpos = track_data_start + sectors[snum].dataOffset;
							startPos = bpos;							
							for (int i = 0; i < 128 << sectors[snum].address.size; i++)
								fd.tracks[tnum].sectors[snum].data[i] = readByte(buffer, ref bpos);
							maxBufPos = Math.Max(maxBufPos, bpos);
							info.AppendText(String.Format("      Reading Sector {0} {1} bytes ({2}-{3})\n", snum, 128 << sectors[snum].address.size, startPos, bpos));
						} // read sector info
					}	// for all sectors

					// if we have timing record we read
					ushort[] timing = null;
					if (bitWidthFound) {
						bpos = maxBufPos;
						startPos = bpos;
						ushort timingFlags = readInt16(buffer, ref bpos);
						ushort timingSize = readInt16(buffer, ref bpos);
						int entries = (timingSize - 4) / 2;
						timing = new ushort[entries];
						for (int i = 0; i < entries; i++) {
							timing[i] = (ushort)(readByte(buffer, ref bpos) << 8);
							timing[i] = readByte(buffer, ref bpos);
						}
						maxBufPos = Math.Max(maxBufPos, bpos);
						info.AppendText(String.Format("      Reading Timing {0} bytes Flag={1} ({2}-{3})\n", timingSize, timingFlags, startPos, bpos));
					}	// sector has timing info

					// now we transfer the fuzzy bytes and timing bytes if necessary
					int startFuzzy = 0;
					int startTiming = 0;
					for (int snum = 0; snum < track.sectorCount; snum++) {
						// fuzzy bytes
						if ((fd.tracks[tnum].sectors[snum].fdcFlags & SectorDesc.FUZZY_BITS) != 0) {
							int fsize = 128 << sectors[snum].address.size;
							fd.tracks[tnum].sectors[snum].fuzzy = new byte[fsize];
							for (int i = 0; i < fsize; i++)
								fd.tracks[tnum].sectors[snum].fuzzy[i] = fuzzyMask[i + startFuzzy];
							startFuzzy += fsize;
							info.AppendText(String.Format("      Transferring {0} Fuzzy bytes to sect {1}\n", fsize, snum));
						}
						// timing bytes
						if (((sectors[snum].fdcFlags & SectorDesc.BIT_WIDTH) != 0) && (file.revision == 2)) {
							int tsize = (128 << sectors[snum].address.size) / 16;
							fd.tracks[tnum].sectors[snum].timing = new ushort[tsize];
							for (int i = 0; i < tsize; i++)
								fd.tracks[tnum].sectors[snum].timing[i] = timing[i + startTiming];
							startTiming += tsize;
							info.AppendText(String.Format("      Transferring {0} Timing values to sect {1}\n", tsize, snum));
						}
					}
					if (timing != null)
						Debug.Assert(timing.Length == startTiming, "invalid timing size");
					if (fuzzyMask != null)
						Debug.Assert(fuzzyMask.Length == startFuzzy, "invalid fuzzy size");
				} // TRK_SECT provided

				else {
					// read all standard sectors
					for (int snum = 0; snum < track.sectorCount; snum++) {
						startPos = bpos;
						fd.tracks[tnum].sectors[snum] = new Sector();
						fd.tracks[tnum].sectors[snum].data = new byte[512];
						for (int i = 0; i < 512; i++)
							fd.tracks[tnum].sectors[snum].data[i] = readByte(buffer, ref bpos);

						fd.tracks[tnum].sectors[snum].fdcFlags = 0;
						fd.tracks[tnum].sectors[snum].bitPosition = 0;
						fd.tracks[tnum].sectors[snum].readTime = 0;
						fd.tracks[tnum].sectors[snum].fuzzy = null;
						fd.tracks[tnum].sectors[snum].timing = null;
						fd.tracks[tnum].sectors[snum].address.track = (byte)(track.trackNumber & 0x7F);
						fd.tracks[tnum].sectors[snum].address.head = (byte)(((track.trackNumber & 0x80) == 0x80) ? 1 : 0);
						fd.tracks[tnum].sectors[snum].address.number = (byte)snum;
						fd.tracks[tnum].sectors[snum].address.crc = 0;
						info.AppendText(String.Format("      Standard Sector {0} {1} bytes ({2}-{3})\n", snum, 512, startPos, bpos));
					} // read all sector
				} // standard non protected sectors

				Debug.Assert(maxBufPos == track_record_start + track.recordSize, "Invalid Track Size",
					String.Format("Track {0} Current pointer position = {1} next track = {2}" , track.trackNumber, bpos, track_record_start + track.recordSize));
				bpos = track_record_start + track.recordSize;
			} // for all tracks

			return PastiStatus.Ok;

		} // pasti/pasti file


		private static uint readInt32(byte[] buf, ref uint pos) {
			uint val = (uint)buf[pos] + (uint)(buf[pos + 1] << 8) + (uint)(buf[pos + 2] << 16) + (uint)(buf[pos + 3] << 24);
			pos += 4;
			return val;
		}


		private static ushort readInt16(byte[] buf, ref uint pos) {
			ushort val = (ushort)buf[pos];
			val += (ushort)(buf[pos + 1] << 8);
			pos += 2;
			return val;
		}


		private static byte readByte(byte[] buf, ref uint pos) {
			return buf[pos++];
		}


		private static char readChar(byte[] buf, ref uint pos) {
			return (char)buf[pos++];
		}

	}

}