using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ipf {

	public class GapElement {
		public uint gapBytes;
		public byte value;
		public GapType type;
	}

	public class DataElem {
		public uint dataBytes;
		public List<byte> value;
		public DataType type;
	}

	public class Sector {
		public uint dataBytes;
		public uint gapBytes;
		public List<GapElement> gapElems = new List<GapElement>();
		public List<DataElem> dataElems = new List<DataElem>();
	}

	public class Track {
		public uint trackBytes;
		public uint dataBytes;
		public uint gapBytes;
		public Density density;
		public uint startPos;
		public uint blockCount;
		public TrackFlags trackFlags;
		public List<Sector> sectors = new List<Sector>();
	}


	public class Floppy {
		public Track[,] tracks;
	}
}
