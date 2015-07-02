using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ipf {

	/// <summary>
	/// Store the content of a gap element including the gap value
	/// </summary>
	public class GapElement {
		public uint gapBytes;
		public byte value;
		public GapType type;
	}

	/// <summary>
	/// Store the content of a data element. The sample is a list
	/// </summary>
	public class DataElem {
		public uint dataBytes;
		public List<byte> value;
		public DataType type;
	}

	/// <summary>
	/// Store the sector/bloc information
	/// </summary>
	public class Sector {
		public uint dataBits;
		public uint gapBits;
		public List<GapElement> gapElems = new List<GapElement>();
		public List<DataElem> dataElems = new List<DataElem>();
		public BlockFlags flags = BlockFlags.None;
	}

	/// <summary>
	/// Store information about one track
	/// </summary>
	public class Track {
		public uint trackBytes;
		public uint dataBits;
		public uint gapBits;
		public Density density;
		public uint startBitPos;
		public uint blockCount;
		public TrackFlags trackFlags;
		public List<Sector> sectors = new List<Sector>();
	}

	/// <summary>
	/// Store information about all tracks of a FD
	/// </summary>
	/// <remarks>We also store the info record to be able to rewrite the same info record</remarks>
	public class Floppy {
		public Track[,] tracks;
		public InfoRecord info;
	}
}
