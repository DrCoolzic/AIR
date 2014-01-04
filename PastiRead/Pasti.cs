/*!
@file Pasti.cs
<summary>Pasti STX file - Basic structures definition</summary>

<div class="jlg">Copyright (C) 2014 Jean Louis-Guerin\n\n
This file is part of the Atari Image Reader (AIR) project.\n
The Atari Image Reader project may be used and distributed without restriction provided
that this copyright statement is not removed from the file and that any
derivative work contains the original copyright notice and the associated
disclaimer.\n
The Atari Image Reader project is free software; you can redistribute it
and/or modify  it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 3
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
using System.Windows.Media;


namespace Pasti {
	/// <summary>Pasti File Header Descriptor</summary>
	/// <remarks>The file Header is used to identify a Pasti File
	/// and to provide some information about the file content
	/// and the tools used to generate the file.</remarks>
	public class FileDesc {
		/// <summary>
		/// Identify a Pasti file and should be equal to the null terminated string “RSY”
		/// </summary>
		public string pastiFileId = "";
		/// <summary>File version number: Should be equal to 3.</summary>
		public ushort version;
		/// <summary>Tool used to create image</summary>
		/// <remarks>
		/// - 0x01 for file generated with the Atari imaging tool, and 
		/// - 0xCC for file generated with the Discovery Cartridge (DC) imaging tool</remarks>
		/// - 0x10 for file generated with the Aufit program
		public ushort tool;
		/// <summary>Reserved</summary>
		public ushort reserved_1;
		/// <summary>Number of track records</summary>
		public byte trackCount;
		/// <summary>revision number of the file. Works in conjunction with version</summary>
		/// <remarks>
		/// - 0x00 Old Pasti Format
		/// - 0x02 New Pasti Format</remarks>
		public byte revision;
		/// <summary>Reserved</summary>						 
		public uint reserved_2;

		/// <summary>
		/// Override ToString() to display File header information
		/// </summary>
		/// <returns>The string</returns>
		public override string ToString() {
			//return base.ToString();
			return String.Format("Pasti file version {0}.{1} Image Tool={2:X2} - Number of tracks={3}",
				version, revision, tool, trackCount);
		}
	}	// FileDesc


	/// <summary>Pasti File Track Descriptor</summary>
	/// <remarks>There is one track record for each track described in the Pasti file</remarks>
	public class TrackDesc {
		/// <summary>Total size of this track record.</summary>
		/// <remarks>Therefore the position of the next Track descriptor (if any) in the file is 
		/// equal to the position of the current Track descriptor plus recordSize.</remarks>
		public uint recordSize;
		/// <summary>number of bytes in the fuzzy mask</summary> 
		/// <remarks>Sectors with fuzzy bytes have the fuzzy bit indicator set (described by 
		/// the bit 7 of the fdcFlags in the Sector descriptor)</remarks>
		public uint fuzzyCount;
		/// <summary>number of sectors in the track</summary>
		/// <remarks>If bits 0 of trackFlags (described below) is set this also indicates the 
		/// number of Sector descriptors in the Track record. A value of 0 indicate an 
		/// empty or unformatted track.</remarks>
		public ushort sectorCount;
		/// <summary>This field contain bit-mask flags that provide information about the content of the track record as follow:</summary>
		/// <remarks> 
		/// - Bit 8-15 not used set to 0
		/// - Bit 7: This bit is set to indicate that the Track image record contain a two byte 
		/// Track Image header, otherwise the Track image header is only one byte. This bit can only set if bit 6 is set.
		/// - Bit 6: This bit is set to indicate that the track record contains an optional Track image
		/// - Bit 5: always set to 1
		/// - Bit 1-4: not used and set to 0
		/// - Bit 0: When this bit is set the track descriptor is followed by sectorCount Sector descriptors.
		/// When not set the track record only contains Sector image following immediately this track descriptor. 
		/// This can only be used for standard 512 bytes sectors with standard sector numbering from 1 to n. 
		/// In this case the size of the track record is equal to 16 + (sectorCount * 512).
		/// </remarks>
		public ushort trackFlags;
		/// <summary>length of the track in number of bytes. Usually around 6250 bytes.</summary>
		public ushort trackLength;
		/// <summary>Bit 7 contains the side (0 or 1) and bits 6-0 contain the track number (usually 0 to 79).</summary>
		public byte trackNumber;
		/// <summary> track image type (not used)</summary>
		public byte trackType;

		// Track descriptor flags
		///<summary> 2 bytes track_image header contains sync offset info</summary>
		public const byte TRK_SYNC = 0x80; 
		/// <summary>track record contains track_image</summary> 
		public const byte TRK_IMAGE = 0x40;
		/// <summary> track contains protections ? not used</summary>
		public const byte TRK_PROT = 0x20; 
		/// <summary>The track record contains sectorCount SectDesc</summary> 
		public const byte TRK_SECT = 0x01;

		/// <summary>
		/// Override ToString() to display Track header information
		/// </summary>
		/// <returns>The string</returns>
		public override string ToString() {
			//return base.ToString();
			return String.Format("Track {0:D2}.{1:D1} {2} bytes {3} sect FuzBytes={4} {5} {6} RecSize={7}",
				trackNumber & 0x7F, (trackNumber & 0x80) >> 7, trackLength, sectorCount, (fuzzyCount == 0) ? "No" : String.Format("{0}", fuzzyCount),
				((trackFlags & TRK_IMAGE)) != 0 ? "TrackImage" : "", 
				((trackFlags & TRK_SECT)) != 0 ? "SectorDesc" : "", recordSize);
		}

	}	// TrackDesc


	/// <summary>Address Segment</summary>
	/// <remarks>This structure is used to store all the information from the address field</remarks>
	public class Address {
		/// <summary>Track number from address field</summary> 
		public byte track;
		/// <summary>Head number from address field</summary>
		public byte head;
		/// <summary>Sector number from address field</summary> 
		public byte number;
		/// <summary>Size value from address field</summary> 
		public byte size;
		/// <summary>CRC Value from address field</summary> 
		public ushort crc;

		/// <summary>
		/// Override ToString() to display Address field information
		/// </summary>
		/// <returns>The string</returns>
		public override string ToString() {
			//return base.ToString();
			return String.Format("T={0,-2} H={1} SN={2,-3} S={3} CRC={4:X4}",
				track, head, number, size,crc);
		}
	}	// Address


	/// <summary>Pasti File Sector Descriptor</summary>
	/// <remarks>
	/// Following the Track descriptor we find an optional set of records used to provide 
	/// additional information about all the sectors of a track. These descriptors are 
	/// only present if bit 0 of the track descriptor TrackFlags is set. The number 
	/// of sector descriptors is equal to the sectorCount field in the track descriptor. 
	/// Each Sector Descriptor is 16 bytes long. There is one sector image info structure 
	/// for each sector found in the track.</remarks>
	public class SectorDesc {
		/// <summary>Offset of sector data inside the track record</summary> 
		/// <remarks>The Track Data record is located just after the optional Sector 
		/// descriptor and Fuzzy Mask record. Therefore we have 
		/// sector_data_position = track_data_position + dataOffset. This can point 
		/// either inside a Track image or to a sector image.</remarks>
		public uint dataOffset;
		/// <summary>This field store the position in number of bits of this sector address block from the index</summary>
		/// <remarks>If you multiply this value by 4 you will get roughly the time it takes in micro-sec to reach the sector address block.</remarks>
		public ushort bitPosition;
		/// <summary>This field contains either zero or the read time for the sector data block.</summary>
		/// <remarks> 
		/// - If the value measured by the imaging tool is within 2% of the standard sector read time 
		/// (16384 us = 512 * 32 micro-sec) the value written is 0 to indicate a sector with "standard" timing.
		/// - Otherwise the measured value in micro-sec is directly stored.
		/// </remarks>
		public ushort readTime;
		/// <summary>Address field of sector (refer to the Address structure)</summary> 
		public Address address = new Address();
 		/// <summary>This field contains a mixture of the FDC status, as it would have been read by the WD1772, 
		/// and other flags used to interpret the track record content.</summary>
		/// <remarks> 
		/// - Bit 7: When set the sector contains fuzzy bits described by a Fuzzy Mask record.
		/// - Bit 6: not used
		/// - Bit 5: FDC Record Type (1 = deleted data, 0 = normal data)
		/// - Bit 4: FDC RNF (record not found). When set the sector contains only an address block but no 
		/// associated data block. In that case there is no data information associated to this descriptor.
		/// - Bit 3: FDC CRC error. If RNF=0 it indicates a CRC error in the data field if RNF=1 it indicates 
		/// a CRC error in address field.
		/// - Bit 1-2: not used
		/// - Bit 0: Intra-sector bit width variation. This flag is used to indicate a protection based on 
		/// bit width variation inside a sector. Currently the only known protection of this type is Macrodos/Speedlock
		///		- If the revision number of the File descriptor is 0 the macrodos/speedlock protection is assumed. 
		///		In that case an internal table needs to be used to simulate the Macrodos/Speedlock bit width variation.
		///		- If the revision number in the File descriptor is 2 the track record contains an extra Timing record 
		///		following the Track Data record. This allow a more precise description of the bit width variations and 
		///		is therefore more open if in the future another kind of intra-sector bit width variation protection 
		///		is discovered.</remarks>
		public byte fdcFlags;
		/// <summary>Reserved (should be 0x00)</summary>
		public byte reserved;

		// Useful FDC Read Sector Status bits
		/// <summary>Record Type (1 = deleted data)</summary>
		public const byte REC_TYPE = 0x20;
		/// <summary>RNF or Seek error</summary>
		public const byte SECT_RNF = 0x10;
		/// <summary>CRC Error (in Data if RNF=0 else in ID)</summary>
		public const byte CRC_ERR = 0x08;

		// Pseudo FDC status bits used for other purpose
		/// <summary>Sector has Fuzzy Bits</summary>
		public const byte FUZZY_BITS = 0x80;
		/// <summary>Sector has bit width variation</summary>
		public const byte BIT_WIDTH = 0x01;

		/// <summary>
		/// Override ToString() to display Pasti Sector descriptor information
		/// </summary>
		/// <returns>The string</returns>		
		public override string ToString() {
			//return base.ToString();
			return String.Format("   Sector {0} bitPos={1,-6} Time={2,-5} Flags={3:X2} {4}{5} Off={6,-6}",
				address.ToString(), bitPosition, readTime, fdcFlags, ((fdcFlags & FUZZY_BITS) != 0) ? "F" : " ",
				((fdcFlags & BIT_WIDTH) != 0) ? "T" : " ", dataOffset);
		}
	}	// SectDesc

}	// Pasti name space
