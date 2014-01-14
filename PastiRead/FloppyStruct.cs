using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pasti {
	/// <summary>Contains information about one sector</summary>
	/// <remarks>This class is used to store all information required to read or write a specific sector</remarks>
	public class Sector {
		/// <summary>buffer for the sector data</summary> 
		public byte[] sectorData;
		/// <summary>buffer for fuzzy mask bytes if necessary</summary> 
		public byte[] fuzzyData;
		/// <summary>buffer for timing bytes if necessary</summary>
		public ushort[] timmingData;
		/// <summary>position in the track of the sector address field in bits</summary>
		public ushort bitPosition;
		/// <summary>read time of the track in ms or 0 if standard sector</summary>
		public ushort readTime;
		/// <summary>Address field content</summary>
		public IDField id = new IDField();
		/// <summary>Status returned by the FDC</summary>
		public byte fdcFlags;
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

		/// <summary>All the sectors of the track follow the Atari standard</summary>
		/// <remarks>This is used to define if Sector Descriptor are required</remarks>
		public bool standardSectors = true;
		/// <summary>The track follow the Atari standard</summary>
		/// <remarks>This is used to define if Track Data is required</remarks>
		public bool standardTrack = true;
	}


	/// <summary>Contains information about a complete Floppy disk</summary>
	/// <remarks>Complete floppy information (i.e. all tracks)</remarks>
	public class Floppy {
		/// <summary>Array of Tracks</summary>
		public Track[,] tracks;
		/// <summary>Total number of tracks</summary>
		/// <remarks>Not really needed should be equal to tracks.Length</remarks>
		//public byte trackCount;
		/// <summary>Contains Pasti version</summary>
		/// <remarks>The format uses the upper nibble as major revision and the lower nibble as minor revision. For example 0x32 is version 3.2</remarks>
		//public byte version;
		/// <summary>Pasti imaging tool</summary>
		/// <remarks>The tool used to create the Past image
		/// - 0x01 Ijor Atari imaging tool
		/// - 0xCC Ijor Discovery Cartridge based imaging tool
		/// - 0x10 DrCoolZic Aufit imaging Tool
		/// - 0xFF Reserved for Test (e.g. tool provided here)</remarks>
		//public ushort tool;
	}

}
