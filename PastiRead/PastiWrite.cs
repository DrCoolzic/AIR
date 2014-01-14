using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pasti {
	class PastiWriter {
		TextBox _infoBox;

		public PastiWriter(TextBox tb) {
			_infoBox = tb;
		}

		/// <summary>
		/// Status returned when reading Pasti file
		/// </summary>
		public enum WriteStatus {
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
		public WriteStatus writePasti(string fileName, Floppy fd) {
			FileStream fs;

			// encode file into buffer
			List<byte> sbuf = new List<byte>();
			WriteStatus status = encodePasti(sbuf, fd);


			try {
				fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			}
			catch (Exception exc) {
				Console.WriteLine("Error: {0}", exc.Message);
				return WriteStatus.FileNotFound;
			}
			fs.Write(sbuf.ToArray(), 0 , sbuf.Count);
			fs.Close();

			return status;		
		}


		/// <summary>
		/// Encode the information in a Floppy structure into a buffer using Pasti file structure
		/// </summary>
		/// <param name="buffer">Contains the data read from Pasti file</param>
		/// <param name="floppy">The Floppy structure to fill</param>
		/// <returns>The read status</returns>
		private WriteStatus encodePasti(List<byte> buffer, Floppy floppy) {
			int start = 0;

			// compute the number of tracks in file
			int trackCount = 0;
			for (int track = 0; track < 84; track++)
				for (int side = 0; side < 2; side++)
					if (floppy.tracks[track, side] != null) trackCount++;

			FileDesc fd = new FileDesc();
			fd.version = 3;
			fd.revision = 2;			
			//fd.trackCount = floppy.trackCount;
			fd.tool = 0x0FF;

			writeByte(buffer, (byte)'R');			// File header
			writeByte(buffer, (byte)'S');
			writeByte(buffer, (byte)'Y');
			writeByte(buffer, 0);

			writeInt16(buffer, fd.version);			// version
			writeInt16(buffer, fd.tool);			// tool
			writeInt16(buffer, 0);					// reserved 1
			writeByte(buffer, fd.trackCount);		// trackCount
			writeByte(buffer, fd.revision);			// revision
			writeInt32(buffer, 0);					// reserved 2

			_infoBox.Clear();
			_infoBox.ScrollToHome();
			_infoBox.FontFamily = new FontFamily("Consolas");
			_infoBox.FontSize = 14;
			_infoBox.AppendText(fd.ToString());
			_infoBox.AppendText(String.Format(" - ({0}-{1})\n", start, buffer.Count-1));
						
			// Write all track records
			//for (int tnum = 0; tnum < floppy.trackCount; tnum++) {
			for (int track = 0; track < 84; track++)
				for (int side = 0; side < 2; side++) {
					if (floppy.tracks[track, side] == null) continue;
					int startTrackRecord = buffer.Count;
					TrackDesc td = new TrackDesc();

					td.recordSize = 16;		// initial value = size of TrackDesc
					//track.trackFlags = 0x0010;	// Only bit 5 set to 1 initially
					td.sectorCount = (ushort)floppy.tracks[track, side].sectors.Count();
					td.trackLength = (ushort)floppy.tracks[track, side].byteCount;
					td.trackNumber = (byte)floppy.tracks[track, side].number;
					if (floppy.tracks[track, side].side == 1) td.trackNumber |= 0x80;
					td.trackType = 0;

					// do we have sector descriptors
					if (trackNeedSectorDesc(floppy.tracks[track, side])) {
						td.trackFlags |= TrackDesc.TRK_SECT_DESC;
						td.recordSize += (uint)(16 * td.sectorCount);
					}

					// do we have fuzzy record
					for (int sect = 0; sect < floppy.tracks[track, side].sectors.Count(); sect++)
						if (floppy.tracks[track, side].sectors[sect].fuzzyData != null)
							td.fuzzyCount += (uint)floppy.tracks[track, side].sectors[sect].fuzzyData.Count();
					td.recordSize += td.fuzzyCount;

					bool trkSync = false;
					// do we have track data
					if (trackNeedTrackData(floppy.tracks[track, side])) {
						if (trkSync) {
							td.trackFlags |= TrackDesc.TRK_IMAGE | TrackDesc.TRK_SYNC;
							td.recordSize += 4;	// header
						}
						else {
							td.trackFlags |= TrackDesc.TRK_IMAGE;
							td.recordSize += 2;	// header
						}
						td.recordSize += floppy.tracks[track, side].byteCount;
						// we write even number of bytes
						td.recordSize += floppy.tracks[track, side].byteCount % 2;
					}

					// do we have timing record
					int timingEntries = 0;
					for (int sect = 0; sect < floppy.tracks[track, side].sectors.Count(); sect++)
						if (floppy.tracks[track, side].sectors[sect].timmingData != null)
							timingEntries += floppy.tracks[track, side].sectors[sect].timmingData.Count();

					if (timingEntries > 0)
						td.recordSize += (uint)((2 * timingEntries) + 4);

					// we had up the sector data - here we do not optimize (data shared with track data)
					for (int sect = 0; sect < floppy.tracks[track, side].sectors.Count(); sect++)
						if (floppy.tracks[track, side].sectors[sect].sectorData != null)
							td.recordSize += (uint)floppy.tracks[track, side].sectors[sect].sectorData.Count();

					//// seems like track record size is always kept even
					//td.recordSize += td.recordSize % 2;

					// Track Descriptor is ready => write it
					start = buffer.Count;
					writeInt32(buffer, td.recordSize);
					writeInt32(buffer, td.fuzzyCount);
					writeInt16(buffer, td.sectorCount);
					writeInt16(buffer, td.trackFlags);
					writeInt16(buffer, td.trackLength);
					writeByte(buffer, td.trackNumber);
					writeByte(buffer, td.trackType);

					_infoBox.AppendText(td.ToString());
					_infoBox.AppendText(String.Format(" ({0}-{1})\n", start, buffer.Count - 1));

					// write sector descriptor if needed
					if (trackNeedSectorDesc(floppy.tracks[track, side])) {
						uint offset = 0;
						if (trackNeedTrackData(floppy.tracks[track, side]))
							offset = (uint)(floppy.tracks[track, side].byteCount + (floppy.tracks[track, side].byteCount % 2) + (trkSync ? 4 : 2));
						for (int sect = 0; sect < floppy.tracks[track, side].sectorCount; sect++) {
							SectorDesc sd = new SectorDesc();
							sd.dataOffset = offset;
							sd.bitPosition = floppy.tracks[track, side].sectors[sect].bitPosition;
							sd.readTime = floppy.tracks[track, side].sectors[sect].readTime;
							sd.id = floppy.tracks[track, side].sectors[sect].id;	// TODO ?
							sd.fdcFlags = floppy.tracks[track, side].sectors[sect].fdcFlags;
							sd.reserved = 0;
							if ((floppy.tracks[track, side].sectors[sect].fdcFlags & SectorDesc.SECT_RNF) == 0)
								offset += (uint)(128 << floppy.tracks[track, side].sectors[sect].id.size);

							start = buffer.Count;
							writeInt32(buffer, sd.dataOffset);
							writeInt16(buffer, sd.bitPosition);
							writeInt16(buffer, sd.readTime);

							writeByte(buffer, sd.id.track);
							writeByte(buffer, sd.id.side);
							writeByte(buffer, sd.id.number);
							writeByte(buffer, sd.id.size);
							writeInt16(buffer, sd.id.crc);

							writeByte(buffer, sd.fdcFlags);
							writeByte(buffer, sd.reserved);

							_infoBox.AppendText(sd.ToString());
							_infoBox.AppendText(String.Format(" ({0}-{1})\n", start, buffer.Count - 1));
						}
					}

					// write fuzzy mask if needed
					for (int sect = 0; sect < floppy.tracks[track, side].sectorCount; sect++) {
						if (floppy.tracks[track, side].sectors[sect].fuzzyData != null) {
							start = buffer.Count;
							for (int i = 0; i < floppy.tracks[track, side].sectors[sect].fuzzyData.Count(); i++)
								writeByte(buffer, floppy.tracks[track, side].sectors[sect].fuzzyData[i]);
							_infoBox.AppendText(String.Format("   Writing Fuzzy {0} bytes for sector {1} ({2}-{3})\n",
								floppy.tracks[track, side].sectors[sect].fuzzyData.Count(), floppy.tracks[track, side].sectors[sect].id.number, start, buffer.Count - 1));
						}	// fuzzy found
					}	// all sect


					// the track data are located after the sector descriptor records & fuzzy record
					int track_data_start = buffer.Count; // store the position of data in track record
					_infoBox.AppendText(String.Format("   Start of Track data {0}\n", track_data_start));

					// write track data if needed
					if (trackNeedTrackData(floppy.tracks[track, side])) {
						start = buffer.Count;
						if (trkSync)
							writeInt16(buffer, floppy.tracks[track, side].firstSyncOffset);
						writeInt16(buffer, (ushort)floppy.tracks[track, side].byteCount);
						for (int i = 0; i < floppy.tracks[track, side].byteCount; i++)
							writeByte(buffer, floppy.tracks[track, side].trackData[i]);
						// we write even number
						if ((floppy.tracks[track, side].byteCount % 2) != 0)
							writeByte(buffer, 0xFF);

						_infoBox.AppendText(String.Format("      Writing Track {0}{1} bytes SyncPos={2} ({3}-{4})\n",
							floppy.tracks[track, side].byteCount + ((trkSync) ? 4 : 2), ((floppy.tracks[track, side].byteCount % 2) != 0) ? "(+1)" : "",
							floppy.tracks[track, side].firstSyncOffset, start, buffer.Count - 1));
					}

					// write all sectors data
					for (int sect = 0; sect < floppy.tracks[track, side].sectorCount; sect++) {
						start = buffer.Count;
						if ((floppy.tracks[track, side].sectors[sect].fdcFlags & SectorDesc.SECT_RNF) == 0) {
							for (int i = 0; i < floppy.tracks[track, side].sectors[sect].sectorData.Count(); i++)
								writeByte(buffer, floppy.tracks[track, side].sectors[sect].sectorData[i]);
							_infoBox.AppendText(String.Format("      Writing Sector {0} {1} bytes ({2}-{3})\n",
								floppy.tracks[track, side].sectors[sect].id.number, floppy.tracks[track, side].sectors[sect].sectorData.Count(), start, buffer.Count - 1));
						}
					}

					// write timing if necessary
					if (timingEntries > 0) {
						// write header
						start = buffer.Count;
						writeInt16(buffer, 0x05);
						writeInt16(buffer, (ushort)(2 * timingEntries + 4));
						_infoBox.AppendText(String.Format("   Writing Timing header Flag=5 size={0} ({1}-{2})\n", 2 * timingEntries + 4, start, buffer.Count - 1));
						// write timing
						for (int sect = 0; sect < floppy.tracks[track, side].sectorCount; sect++) {
							if (floppy.tracks[track, side].sectors[sect].timmingData != null) {
								start = buffer.Count;
								for (int i = 0; i < floppy.tracks[track, side].sectors[sect].timmingData.Count(); i++) {
									// Big Indian value
									buffer.Add((byte)(floppy.tracks[track, side].sectors[sect].timmingData[i] << 8));
									buffer.Add((byte)floppy.tracks[track, side].sectors[sect].timmingData[i]);
								}
								_infoBox.AppendText(String.Format("   Writing Timing {0} values for sector {1} ({2}-{3})\n",
									floppy.tracks[track, side].sectors[sect].timmingData.Count(), floppy.tracks[track, side].sectors[sect].id.number, start, buffer.Count - 1));
							}	// timing found
						}	// all sect
					}

					if (buffer.Count % 2 != 0) {
						writeByte(buffer, 0xFF);	// dummy extra byte
					}	// even

					Debug.Assert(buffer.Count == startTrackRecord + td.recordSize, "Invalid Track Size",
						String.Format("Track {0} Current pointer position = {1} next track = {2}", td.trackNumber, buffer.Count, startTrackRecord + td.recordSize));
				} // for all tracks

			return WriteStatus.Ok;

		}	// encode pasti


		private static void writeInt32(List<byte> buffer, uint val) {
			buffer.Add((byte)(val & 0xFF));
			buffer.Add((byte)((val >> 8) & 0xFF));
			buffer.Add((byte)((val >> 16) & 0xFF));
			buffer.Add((byte)((val >> 24) & 0xFF));
		}


		private static void writeInt16(List<byte> buffer, ushort val) {
			buffer.Add((byte)(val & 0xFF));
			buffer.Add((byte)(val >> 8));
		}

		private static void writeByte(List<byte> buffer, byte val) {
			buffer.Add(val);
		}


		private bool trackNeedSectorDesc(Track track) {
			// here we test for
			// - All the sectors of the track are numbered sequentially from 1 to n
			// - All the sectors use valid sector numbers from 0x00-0xF4
			// - All the sectors of the track have unique sector number
			// - All sectors in the track have standard timing values
			// - invalid track number
			// - Special Flags: No data, CRC, DDAM (cover sector no data)

			int secnum = 1;
			for (int sect = 0; sect < track.sectorCount; sect++) {
				// sequential sector number 1..n
				if (track.sectors[sect].id.number != secnum++) 
					track.standardSectors = false;

				// duplicate sector
				for (int j = sect + 1; j < track.sectorCount; j++)
					if (track.sectors[sect].id.number == track.sectors[j].id.number)
						track.standardSectors = false;

				// sector with invalid sector number
				if ((track.sectors[sect].id.number >= 0xF5) && (track.sectors[sect].id.number <= 0xF7))
					track.standardSectors = false;

				// sector with invalid track number
				if (track.sectors[sect].id.track != track.number)
					track.standardSectors = false;

				// sector with invalid side/head
				if (track.sectors[sect].id.side != track.side)
					track.standardSectors = false;

				// sector with invalid size
				if (track.sectors[sect].id.size > 3)
					track.standardSectors = false;

				// short long sector time
				if (track.sectors[sect].readTime != 0)
					track.standardSectors = false;

				// FDC Flags - Here we read the saved flags

				// RNF = sector with no data
				//if ((track.sectors[sect].fdcFlags & SectorDesc.SECT_RNF) != 0)
				if (track.sectors[sect].sectorData == null)
					track.standardSectors = false;

				// CRC error = ID / Data
				if ((track.sectors[sect].fdcFlags & SectorDesc.CRC_ERR) != 0)
					track.standardSectors = false;

				// Record type = DDAM
				if ((track.sectors[sect].fdcFlags & SectorDesc.REC_TYPE) != 0)
					track.standardSectors = false;
				
				// FDC - Fuzzy
				if (track.sectors[sect].fuzzyData != null)
					track.standardSectors = false;

				// FDC - Timing
				if (track.sectors[sect].timmingData != null)
					track.standardSectors = false;
			}
			if (trackNeedTrackData(track))
				return true;
			return !track.standardSectors;
		}


		private bool trackNeedTrackData(Track track) {
			// We need Optional Track Data when:
			// - Single Data Segment = ID found but no data
			// - Invalid Data in Gap ?
			// - Shifted Track: Data over Index + Data beyond Index + ID over index
			// - Invalid sync mark sequence
			// - Sector within Sector
			// - No Flux Area
			// - Short long track ??
			// - SWS
			// - no data

			// for now we are cheating by using the TRK_IMAGE flag
			return !track.standardTrack;
		}

	}

}
