﻿/*!
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pasti {
	/// <summary>
	/// The reader class
	/// </summary>
	public class PastiReader {
		TextBox _infoBox;

		/// <summary>
		/// The Pasti reader constructor
		/// </summary>
		/// <param name="tb">The textbox used to display information</param>
		public PastiReader(TextBox tb) {
			_infoBox = tb;
		}

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


		/// <summary>
		/// Read a Pasti file and fills the Floppy structure
		/// </summary>
		/// <param name="fileName">Name of the Pasti file to read</param>
		/// <param name="fd">the Floppy parameter</param>
		/// <returns>The status</returns>
		public PastiStatus readPasti(string fileName, Floppy fd) {
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
			PastiStatus status = PastiStatus.Ok;
            try {
				status = decodePasti(sbuf, fd);
			} 
			catch (IndexOutOfRangeException e) {
				MessageBox.Show("End of File - Missing data", "Error in Pasti File");
				return PastiStatus.NotPastiFile;

			}
			return status;
		}


		/// <summary>
		/// Decode the information in a Pasti file and fill the Floppy structure accordingly
		/// </summary>
		/// <param name="buffer">Contains the data read from Pasti file</param>
		/// <param name="fd">The Floppy structure to fill</param>
		/// <returns>The read status</returns>
		private PastiStatus decodePasti(byte[] buffer, Floppy fd) {
			uint bpos = 0;
			uint startPos = 0;
			uint maxBufPos = 0;

			startPos = bpos;
			FileDesc file = new FileDesc();
			// Read File Descriptor
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			file.pastiFileId += readChar(buffer, ref bpos);
			file.version = readUInt16(buffer, ref bpos);
			file.tool = readUInt16(buffer, ref bpos);
			file.reserved_1 = readUInt16(buffer, ref bpos);
			file.trackCount = readByte(buffer, ref bpos);
			file.revision = readByte(buffer, ref bpos);
			file.reserved_2 = readUInt32(buffer, ref bpos);

			if (file.pastiFileId != "RSY\0")
				return PastiStatus.NotPastiFile;
			if (file.version != 3)
				return PastiStatus.UnsupportedVersion;

			_infoBox.Clear();
			_infoBox.FontFamily = new FontFamily("Consolas");
			_infoBox.FontSize = 14;
			_infoBox.AppendText(file.ToString());
			_infoBox.AppendText(String.Format(" - ({0}-{1})\n", startPos, bpos - 1));
			_infoBox.ScrollToHome();

			//fd.tracks = new Track[file.trackCount];	// create an array of track
			fd.tracks = new Track[85, 2];
			//fd.trackCount = file.trackCount;		// store the number of tracks
			//fd.version = (byte)(file.version * 10 + file.revision);
			//fd.tool = file.tool;

			TrackDesc td = new TrackDesc();
			for (int tnum = 0; tnum < file.trackCount; tnum++) {
				uint track_record_start = bpos;
				startPos = bpos;
				Debug.Assert((startPos % 2) == 0, "Track Descriptor not word aligned", String.Format("Address = {0}", startPos));
				// Read Track Descriptors
				td.recordSize = readUInt32(buffer, ref bpos);
				td.fuzzyCount = readUInt32(buffer, ref bpos);
				td.sectorCount = readUInt16(buffer, ref bpos);
				td.trackFlags = readUInt16(buffer, ref bpos);
				td.trackLength = readUInt16(buffer, ref bpos);
				td.trackNumber = readByte(buffer, ref bpos);
				td.trackType = readByte(buffer, ref bpos);
				maxBufPos = bpos;
				int track = td.trackNumber & 0x7F;
				int side = (td.trackNumber & 0x80) >> 7;

				_infoBox.AppendText(td.ToString());
				_infoBox.AppendText(String.Format(" ({0}-{1})\n", startPos, bpos - 1));

				fd.tracks[track, side] = new Track();

				// create array of sector for this track
				fd.tracks[track, side].sectors = new Sector[td.sectorCount];
				fd.tracks[track, side].sectorCount = td.sectorCount;
				fd.tracks[track, side].byteCount = td.trackLength;
				fd.tracks[track, side].side = (uint)(td.trackNumber >> 7);
				fd.tracks[track, side].number = (uint)(td.trackNumber & 0x7F);

				// read sector descriptors if specified
				if ((td.trackFlags & TrackDesc.TRK_SECT_DESC) != 0) {
					SectorDesc[] sectors = new SectorDesc[td.sectorCount];
					for (int snum = 0; snum < td.sectorCount; snum++) {
						sectors[snum] = new SectorDesc();
						startPos = bpos;

						// read sector descriptor
						sectors[snum].dataOffset = readUInt32(buffer, ref bpos);
						sectors[snum].bitPosition = readUInt16(buffer, ref bpos);
						sectors[snum].readTime = readUInt16(buffer, ref bpos);
						sectors[snum].id.track = readByte(buffer, ref bpos);
						sectors[snum].id.side = readByte(buffer, ref bpos);
						sectors[snum].id.number = readByte(buffer, ref bpos);
						sectors[snum].id.size = readByte(buffer, ref bpos);
						sectors[snum].id.crc = readUInt16(buffer, ref bpos);
						sectors[snum].fdcFlags = readByte(buffer, ref bpos);
						sectors[snum].reserved = readByte(buffer, ref bpos);

						_infoBox.AppendText(sectors[snum].ToString());
						_infoBox.AppendText(String.Format(" ({0}-{1})\n", startPos, bpos - 1));

						fd.tracks[track, side].sectors[snum] = new Sector();
						fd.tracks[track, side].sectors[snum].fdcFlags = sectors[snum].fdcFlags;
						fd.tracks[track, side].sectors[snum].bitPosition = sectors[snum].bitPosition;
						fd.tracks[track, side].sectors[snum].readTime = sectors[snum].readTime;
						fd.tracks[track, side].sectors[snum].id = sectors[snum].id;
						fd.tracks[track, side].sectors[snum].fdcFlags = sectors[snum].fdcFlags;
					} // for all sectors

					byte[] fuzzyMask = null;
					// if necessary read fuzzy bytes
					if (td.fuzzyCount != 0) {
						startPos = bpos;
						fuzzyMask = new byte[td.fuzzyCount];
						for (int i = 0; i < td.fuzzyCount; i++)
							fuzzyMask[i] = readByte(buffer, ref bpos);
						_infoBox.AppendText(String.Format("   Reading Fuzzy {0} bytes ({1}-{2})\n", td.fuzzyCount, startPos, bpos - 1));
					}

					// the track data are located after the sector descriptor records & fuzzy record
					uint track_data_start = bpos;		// store the position of data in track record
					_infoBox.AppendText(String.Format("   Start of Track data {0}\n", track_data_start));

					// read track data if specified
					if ((td.trackFlags & TrackDesc.TRK_IMAGE) != 0) {
						// track with sync offset
						startPos = bpos;
						// read sync if provided
						if ((td.trackFlags & TrackDesc.TRK_SYNC) != 0)
							fd.tracks[track, side].firstSyncOffset = readUInt16(buffer, ref bpos);
						else
							fd.tracks[track, side].firstSyncOffset = 0;
						// read track data size
						fd.tracks[track, side].byteCount = readUInt16(buffer, ref bpos);
						// read track data
						fd.tracks[track, side].trackData = new byte[fd.tracks[track, side].byteCount];
						for (int i = 0; i < fd.tracks[track, side].byteCount; i++)
							fd.tracks[track, side].trackData[i] = readByte(buffer, ref bpos);
						// seems like we are reading even number
						maxBufPos = Math.Max(maxBufPos, bpos + bpos % 2);
						fd.tracks[track, side].standardTrack = false;
						_infoBox.AppendText(String.Format("      Reading Track Image {0}{1} bytes (including {2} header bytes) SyncPos={3} ({4}-{5})\n",
							fd.tracks[track, side].byteCount + (((td.trackFlags & TrackDesc.TRK_SYNC) != 0) ? 4 : 2), (bpos % 2 != 0) ? "+1" : "",
							((td.trackFlags & TrackDesc.TRK_SYNC) != 0) ? 4 : 2, fd.tracks[track, side].firstSyncOffset, startPos, bpos + (bpos % 2) - 1));
					} // read track_data

					// read all sectors data
					bool bitWidth = false;
					for (int snum = 0; snum < td.sectorCount; snum++) {
						if (((sectors[snum].fdcFlags & SectorDesc.BIT_WIDTH) != 0) && (file.revision == 2))
							bitWidth = true;

						// create sector buffer and read data if necessary
						if ((sectors[snum].fdcFlags & SectorDesc.SECT_RNF) == 0) {
							fd.tracks[track, side].sectors[snum].sectorData = new byte[128 << sectors[snum].id.size];
							bpos = track_data_start + sectors[snum].dataOffset;
							startPos = bpos;
							for (int i = 0; i < 128 << sectors[snum].id.size; i++)
								fd.tracks[track, side].sectors[snum].sectorData[i] = readByte(buffer, ref bpos);
							maxBufPos = Math.Max(maxBufPos, bpos);
							_infoBox.AppendText(String.Format("      Reading Sector {0} {1} bytes ({2}-{3}) {4}\n",
								fd.tracks[track, side].sectors[snum].id.number, 128 << sectors[snum].id.size, startPos, bpos - 1,
								(((td.trackFlags & TrackDesc.TRK_IMAGE) != 0) && (sectors[snum].dataOffset < fd.tracks[track, side].byteCount)) ? " in Track Image" : ""));
						} // read sector data
					}	// for all sectors

					// if we have timing record we read
					ushort[] timing = null;
					if (bitWidth) {
						bpos = maxBufPos;
						startPos = bpos;
						ushort timingFlags = readUInt16(buffer, ref bpos);
						ushort timingSize = readUInt16(buffer, ref bpos);
						int entries = (timingSize - 4) / 2;
						timing = new ushort[entries];
						for (int i = 0; i < entries; i++) {
							// Big Indian value
							timing[i] = (ushort)(readByte(buffer, ref bpos) << 8);
							timing[i] = readByte(buffer, ref bpos);
						}
						maxBufPos = Math.Max(maxBufPos, bpos);
						_infoBox.AppendText(String.Format("      Reading Timing {0} bytes Flag={1} ({2}-{3})\n", timingSize, timingFlags, startPos, bpos - 1));
					}	// sector has timing data

					// now we transfer the fuzzy bytes and timing bytes if necessary
					int startFuzzy = 0;
					int startTiming = 0;
					for (int snum = 0; snum < td.sectorCount; snum++) {
						// fuzzy bytes
						if ((fd.tracks[track, side].sectors[snum].fdcFlags & SectorDesc.FUZZY_BITS) != 0) {
							int fsize = 128 << sectors[snum].id.size;
							fd.tracks[track, side].sectors[snum].fuzzyData = new byte[fsize];
							for (int i = 0; i < fsize; i++)
								fd.tracks[track, side].sectors[snum].fuzzyData[i] = fuzzyMask[i + startFuzzy];
							startFuzzy += fsize;
							_infoBox.AppendText(String.Format("      Transferring {0} Fuzzy bytes to sect {1}\n", fsize, fd.tracks[track, side].sectors[snum].id.number));
						}
						// timing bytes
						if ((sectors[snum].fdcFlags & SectorDesc.BIT_WIDTH) != 0) {
							int tsize = (128 << sectors[snum].id.size) / 16;
							fd.tracks[track, side].sectors[snum].timmingData = new ushort[tsize];
							if (file.revision == 2) {
								for (int i = 0; i < tsize; i++)
									fd.tracks[track, side].sectors[snum].timmingData[i] = timing[i + startTiming];
								startTiming += tsize;
								_infoBox.AppendText(String.Format("      Transferring {0} Timing values to sect {1}\n", tsize, fd.tracks[track, side].sectors[snum].id.number));
							}	// revision == 2 => read timing from file							
							else {
								for (int i = 0; i < tsize; i++) {
									if (i < (tsize / 4)) fd.tracks[track, side].sectors[snum].timmingData[i] = 127;
									else if (i < (tsize / 2)) fd.tracks[track, side].sectors[snum].timmingData[i] = 133;
									else if (i < ((3 * tsize) / 2)) fd.tracks[track, side].sectors[snum].timmingData[i] = 121;
									else fd.tracks[track, side].sectors[snum].timmingData[i] = 127;
								}
							}	// if revision != 2 => we simulate with a table
						}
					}
					if (timing != null)
						Debug.Assert(timing.Length == startTiming, "invalid timing size");
					if (fuzzyMask != null)
						Debug.Assert(fuzzyMask.Length == startFuzzy, "invalid fuzzy size");
				} // TRK_SECT provided

				else {
					// read all standard sectors
					for (int snum = 0; snum < td.sectorCount; snum++) {
						startPos = bpos;
						fd.tracks[track, side].sectors[snum] = new Sector();
						fd.tracks[track, side].sectors[snum].sectorData = new byte[512];
						for (int i = 0; i < 512; i++)
							fd.tracks[track, side].sectors[snum].sectorData[i] = readByte(buffer, ref bpos);

						fd.tracks[track, side].sectors[snum].fdcFlags = 0;
						fd.tracks[track, side].sectors[snum].bitPosition = 0;
						fd.tracks[track, side].sectors[snum].readTime = 0;
						fd.tracks[track, side].sectors[snum].fuzzyData = null;
						fd.tracks[track, side].sectors[snum].timmingData = null;
						fd.tracks[track, side].sectors[snum].id.track = (byte)(td.trackNumber & 0x7F);
						fd.tracks[track, side].sectors[snum].id.side = (byte)(((td.trackNumber & 0x80) == 0x80) ? 1 : 0);
						fd.tracks[track, side].sectors[snum].id.number = (byte)snum;
						fd.tracks[track, side].sectors[snum].id.crc = 0;
						_infoBox.AppendText(String.Format("      Standard Sector {0} {1} bytes ({2}-{3})\n", snum, 512, startPos, bpos));
						maxBufPos = Math.Max(maxBufPos, bpos);
					} // read all sector
				} // standard non protected sectors

				// we set record size to even value
				maxBufPos += maxBufPos % 2;

				Debug.Assert(maxBufPos == track_record_start + td.recordSize, "Invalid Track Size",
					String.Format("Track {0} Current pointer position = {1} next track = {2}", td.trackNumber, bpos, track_record_start + td.recordSize));
				bpos = track_record_start + td.recordSize;
			} // for all tracks

			return PastiStatus.Ok;

		} // pasti/pasti file


		private static uint readUInt32(byte[] buf, ref uint pos) {
			uint val = (uint)buf[pos] + (uint)(buf[pos + 1] << 8) + (uint)(buf[pos + 2] << 16) + (uint)(buf[pos + 3] << 24);
			pos += 4;
			return val;
		}


		private static ushort readUInt16(byte[] buf, ref uint pos) {
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