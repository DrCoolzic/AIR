using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ipf {
	/// <summary>Type of media imaged</summary>
	public enum MediaType : uint { Unknown, Floppy_Disk };

	/// <summary>Image encoder Type</summary>
	public enum EncoderType : uint { Unknown, CAPS, SPS };

	/// <summary>Platform on which the image can be run</summary>
	/// <remarks>Platform can take the following values:
	/// - 01 = Amiga
	/// - 02 = Atari ST
	/// - 03 = PC
	/// - 04 = Amstrad CPC
	/// - 05 = Spectrum
	/// - 06 = Sam Coupe
	/// - 07 = Archimedes
	/// - 08 = C64
	/// - 09 = Atari 8bit
	/// </remarks>
	public enum Platform : uint {
		Unknown, Amiga, Atari_ST, PC, Amstrad_CPC, Spectrum, Sam_Coupe, Archimedes, C64, Atari_8bit
	}

	/// <summary>The cell density for this track</summary>
	public enum Density : uint {
		Unknown, Noise, Auto, Copylock_Amiga, Copylock_Amiga_New, Copylock_ST, Speedlock_Amiga,
		Speedlock_Amiga_Old, Adam_Brierley_Amiga, Adam_Brierley_Key_Amiga
	}

	/// <summary>Signal Processing Type</summary>
	public enum SignalType : uint { Unknown, cell_2us }

	/// <summary>Flags for the track</summary>
	[Flags]
	public enum TrackFlags : uint {
		None = 0x0,
		Fuzzy = 0x1
	}

	/// <summary>Block Encoder Type</summary>
	public enum BlockEncoderType : uint { Unknown, MFM, RAW }


	/// <summary>Flags for a block descriptor</summary>
	[Flags]
	public enum BlockFlags : uint {
		None = 0x00,
		FwGap = 0x01,
		BwGap = 0x02,
		DataInBit = 0x04
	}
	
	// https://msdn.microsoft.com/fr-fr/library/cc138362.aspx
	//if ((bd.blockFlags & BlockFlags.BwGap) == BlockFlags.BwGap) {
	//if (bd.blockFlags.HasFlag(BlockFlags.BwGap)) {
	//if(bd.blockFlags.Equals(BlockFlags.None))


	/// <summary>Type of Data in data element</summary>
	public enum DataType : uint { Unknown, Sync, Data, Gap, Raw, Fuzzy }

	/// <summary>Type of a gap element</summary>
	public enum GapType : uint { Unknown, Forward, Backward }

	/// <summary>Type of information in a gap element</summary>
	public enum GapElemType { Unknown, Gap_Length, Sample_Length }

	/// <summary>IPF record Header Descriptor</summary>
	/// <remarks>Each record in IPF file starts with a record header</remarks>
	public class RecordHeader {
		/// <summary>Identify the record</summary>
		public char[] type = new char[4];

		/// <summary>Length of the record</summary>
		public uint length;

		/// <summary>CRC32 for the complete record</summary>
		public uint crc;

		/// <summary>Override ToString() to display record header info</summary>
		/// <returns>The record header string</returns>
		public override string ToString() {
			return String.Format("Record={0} Size={1} CRC={2:X8}",
				new string(type), length, crc);
		}
	}	// RecordHeader


	/// <summary>
	/// The info record definition
	/// </summary>
	public class InfoRecord {
		/// <summary>Type of media imaged</summary>
		/// <remarks> The only type currently recognized is floppy disks (value=1)</remarks>
		public MediaType mediaType;

		/// <summary>image encoder type</summary>
		/// <remarks>Depending on the encoder ID some fields in the IMGE records are interpreted differently. 
		/// Currently this field can take the following values: 01 = CAPS encoder or 02 = SPS encoder
		/// </remarks>
		public EncoderType encoderType;

		/// <summary> Image encoder revision </summary>
		/// <remarks>Currently only revision 1 exists for CAPS or SPS encoders</remarks>
		public uint encoderRev;

		/// <summary>
		/// Each IPF file has a unique ID that can be used as a unique key for database
		/// </summary>
		/// <remarks>More than one file can have the same Key for example for a game that has more than one disk.</remarks>
		public uint fileKey;

		/// <summary>Revision of the file.</summary>
		/// <remarks>Normally the revision is one</remarks>
		public uint fileRev;

		/// <summary>Reference to original source</summary>
		public uint origin;

		/// <summary>The first track number of the floppy image. </summary>
		/// <remarks>Usually 0</remarks>
		public uint minTrack;

		/// <summary>The last track number of the floppy image.</summary>
		/// <remarks>Usually 83</remarks>
		public uint maxTrack;

		/// <summary>The lowest head (side) number of the floppy image.</summary>
		/// <remarks>Usually 0</remarks>
		public uint minSide;

		/// <summary>The highest head (side) number of the floppy image. Usually 1</summary>
		public uint maxSide;

		/// <summary>The image creation date: Specify the year, the month, the day</summary>
		/// <remarks>the date is packed into a 4 bytes integers. </remarks>
		public uint creationDate;

		/// <summary>The image creation time: Specify the hour, the minute, the second, and the tick</summary>
		///  <remarks>the time is packed into a 4 bytes integers.</remarks>  
		public uint creationTime;

		/// <summary>Array of four possible platforms</summary>
		/// <remarks>This array contains 4 values that identifies on which platforms the image can be run
		/// (one image can be run on several platform).</remarks>
		public Platform[] platforms = new Platform[4];

		/// <summary>Number of the disk in a multi-disc release otherwise 0</summary>
		public uint diskNumber;

		/// <summary>A unique user ID of the disk image creator</summary>
		public uint creatorId;

		/// <summary>reserved for future</summary>
		public uint[] reserved = new uint[3];


		/// <summary>Override ToString() to display info record content</summary>
		/// <returns>The record header string</returns>
		public override string ToString() {
			string date = String.Format("{0:D2}/{1:D2}/{2:D2}", creationDate / 10000, (creationDate/100) % 100, creationDate % 100);
			string time = String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", creationTime/10000000, (creationTime/100000) % 100, 
				(creationTime/1000)%100, creationTime % 1000);
			string pstring = null;
			foreach (Platform p in platforms)
				if (p != 0) pstring += (p.ToString() + " ");

			return String.Format(
					"   Type={0} Encoder={1}(V{2}) File={3}(V{4}) Disk={5} Origin={6:X8} Creator={7:X8}\n" +
					"   Track={8:D2}-{9:D2} Side={10}-{11} Date={12} Time={13} Platforms={14}\n",
					mediaType.ToString(), encoderType.ToString(), encoderRev, fileKey, fileRev, diskNumber, origin,
					creatorId, minTrack, maxTrack, minSide, maxSide, date, time, pstring);
		}
	}


	public class ImageRecord {
		public uint track;
		public uint side;
		public Density density;
		public SignalType signalType;
		public uint trackBytes;
		public uint startBytePos;
		public uint startBitPos;
		public uint dataBits;
		public uint gapBits;
		public uint trackBits;
		public uint blockCount;
		public uint encoder;
		public TrackFlags trackFlags;
		public uint dataKey;
		/// <summary>reserved for future</summary>
		public uint[] reserved = new uint[3];


		public override string ToString() {
			return String.Format(
				"   T{0:D2}.{1} Size={2} bytes ({3} bits = Data={4} + Gap={5}) Start Byte={6} Bit={7}\n" +
				"   DataKey={8:D3} Block={9} Density={10} Signal={11} Encoder={12} Flags={13}\n",
				track, side, trackBytes, trackBits, dataBits, gapBits, startBytePos, startBitPos,
				dataKey, blockCount, density.ToString(), signalType.ToString(), encoder, trackFlags.ToString());
		}
	}


	public class DataRecord {
		public uint length;
		public uint bitSize;
		public uint crc;
		public uint key;
		public override string ToString() {
			return String.Format("== DataKey={0:D3} Size={1} bytes ({2} bits) CRC={3:X8}",
				key, length, bitSize, crc);
		}
	}


	[StructLayout(LayoutKind.Explicit)]
	public struct BlockDescriptor {
		[FieldOffset(0)]
		public uint dataBits;
		[FieldOffset(4)]
		public uint gapBits;

		[FieldOffset(8)]	// CAPS
		public uint dataBytes;
		[FieldOffset(12)]
		public uint gapBytes;

		[FieldOffset(8)]	// SPS
		public uint gapOffset;
		[FieldOffset(12)]
		public SignalType cellType;

		[FieldOffset(16)]
		public BlockEncoderType encoderType;
		[FieldOffset(20)]
		public BlockFlags blockFlags;
		[FieldOffset(24)]
		public uint gapDefaultValue;
		[FieldOffset(28)]
		public uint dataOffset;
	}


	public class CteiRecord {
		public uint releaseCrc;
		public uint analyzerRev;
		public uint[] reserved = new uint[14];
		public override string ToString() {
			return String.Format("   Associated IPF CRC={0:X8}  Analyzer revision={1}\n",
				releaseCrc, analyzerRev);
		}
	}


	public class CtexRecord {
		public uint track;
		public uint side;
		public Density density;
		public uint formatId;
		public uint fix;
		public uint trackSize;
		public uint[] reserved = new uint[2];
		public override string ToString() {
			return String.Format("   T{0:D2}.{1} Density={2} Format={3} Fix={4} TrackSize={5}\n",
				track, side, density.ToString(), formatId, fix, trackSize);
		}
	}

 
}
