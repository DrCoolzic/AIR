using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPF {
	/// <summary>IPF record Header Descriptor</summary>
	/// <remarks></remarks>
	public class RecordHeader {
		/// <summary>Identify the record</summary>
		public byte[] recordType = new byte[4];

		/// <summary>Length of the record</summary>
		public uint recordLength;

		/// <summary>CRC32 for the complete record</summary>
		public uint crc32;

		/// <summary>
		/// Override ToString() to display record header information
		/// </summary>
		/// <returns>The record header string</returns>
		public override string ToString() {
			//return base.ToString();
			return String.Format("Record Type {0} - Size {1} - CRC={2:X8}",
				recordType.ToString(), recordLength, crc32);
		}
	}	// RecordHeader

	/// <summary>
	/// The type of image
	/// </summary>
	public enum ImageType : uint { Unknown, Floppy_Disk };

	/// <summary>
	/// The image encoder Type
	/// </summary>
	public enum EncoderType : uint { Unknown, CAPS, SPS };

	/// <summary>
	/// Platform on which the image can be run
	/// </summary>
	public enum Platform : uint {
		Amiga = 1, Atari_ST = 2, PC = 3, Amstrad_CPC = 4, Spectrum = 5, Sam_Coupe = 6, Archimedes = 7, C64 = 8, Atari_8bit
	}


	public class infoRecord {
		/// <summary>The type of image.</summary>
		/// <remarks> The only type currently recognized is 1 for floppy.</remarks>
		ImageType image;

		/// <summary>The image encoder ID</summary>
		/// <remarks>Depending on the encoder ID some fields in the IMGE records are interpreted differently. 
		/// Currently this field can take the following values: 01 = CAPS encoder or 02 = SPS encoder
		/// </remarks>
		EncoderType encoderType;

		/// <summary> Image encoder revision </summary>
		/// <remarks>Currently only revision 1 exists for CAPS or SPS encoders</remarks>
		uint encoderRev;

		/// <summary>
		/// Each IPF file has a unique ID that can be used as a unique key for database
		/// </summary>
		/// <remarks>More than one file can have the same ID for example for a game that has more than one disk.</remarks>
		uint fileKey;

		/// <summary>
		/// Revision of the file. Normally the revision is one
		/// </summary>
		uint fileRev;

		/// <summary>
		/// The first track number of the floppy image. Usually 0
		/// </summary>
		uint minTrack;

		/// <summary>
		/// The last track number of the floppy image. Usually 83
		/// </summary>
		uint maxTrack;

		/// <summary>
		/// The lowest head (side) number of the floppy image. Usually 0
		/// </summary>
		uint minHead;

		/// <summary>
		/// The highest head (side) number of the floppy image. Usually 1
		/// </summary>
		uint maxHead;

		/// <summary>
		/// The image creation date packed into a 4 bytes integers. Specify the year, the month, the day
		/// </summary>
		uint creationDate;

		/// <summary>
		/// The image creation time packed into a 4 bytes integers. Specify the hour, the minute, the second, and the tick
		/// </summary>
		uint creationTime;

		/// <summary>Array of four 4 possible platforms</summary>
		/// <remarks>This array contains 4 values that identifies on which platforms the image can be run</remarks>
		Platform[] platforms = new Platform[4];

		/// <summary>
		/// Number of the disk in a multi-disc release otherwise 0
		/// </summary>
		uint diskNumber;

		/// <summary>
		/// A unique user ID of the disk image creator
		/// </summary>
		uint creatorId;

		uint[] reserved = new uint[3];

		public override string ToString() {
			string date = String.Format("{0:D2}/{1:D2}/{2:D2}", creationDate / 10000, (creationDate/100) % 100, creationDate % 100);
			string time = String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", creationTime/10000000, (creationTime/100000) % 100, 
				(creationTime/1000)%100, creationTime % 1000);
			string pstring = null;
			//foreach (uint n in platforms) {
			//	switch (n) {
			//		case 1: pstring += "Amiga "; break;
			//		case 2: pstring += "Atari_ST "; break;
			//		case 3: pstring += "PC "; break;
			//		case 4: pstring += "Amstrad_CPC "; break;
			//		case 5: pstring += "Spectrum "; break;
			//		case 6: pstring += "Sam_Coupe "; break;
			//		case 7: pstring += "Archimedes "; break;
			//		case 8: pstring += "C64 "; break;
			//		case 9: pstring += "Atari_8bit"; break;
			//	}

			//}
			foreach (Platform p in platforms)
				if (p != 0) pstring += (p.ToString() + " ");

			return String.Format("   Type {0} Encoder {1}(V{2}) File {2}(V{3}) Disk {4} Creator {5:X8}\n" +
					"      Track {6:D2}-{7:D2} Head {8}-{9} Date {10} Time {11} Platforms {12}",
					image.ToString(), encoderType.ToString(), encoderRev, fileKey, fileRev, diskNumber, creatorId, 
					minTrack, maxTrack, minHead, maxHead, date, time, pstring);
		}
	}
}
