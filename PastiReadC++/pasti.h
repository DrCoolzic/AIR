/*!
@file pasti.h
<summary>Header file for Pasti STX file definition</summary>
*/

#include <stdint.h>
#pragma warning(disable : 4996)

/// <summary>Pasti File Descriptor</summary>
/// <remarks>The file Header is used to identify a Pasti File
/// and to provide some information about the file content
/// and the tools used to generate the file.</remarks>
struct FileDesc {
	/// constructor
	FileDesc() : revision(0), tool(0), nb_tracks(0) { *file_id = 0; }

	char		file_id[4];			///< File Identifier "RSY\0"
	uint16_t	version;			///< File version number
	uint16_t	tool;				///< Tool used to create image
	uint16_t	res1;				///< reserved 1
	uint8_t		nb_tracks;			///< Number of track records
	uint8_t		revision;			///< File revision number
	uint32_t	res2;				///< reserved 2
};


/// <summary>Pasti Track Descriptor</summary>
/// <remarks>There is one track record for each track described in the Pasti file</remarks>
struct TrackDesc {
	/// constructor
	TrackDesc() : record_size(0), fuzzy_bytes(0), nb_sectors(0), 
		flags(0), track_size(0), track_number(0), record_type(0) {}
	
	/// <summary>Total size of this track record. Adding this value to the 
	/// current track position points to the next track record in the file</summary>
	uint32_t	record_size;
	uint32_t	fuzzy_bytes;		///< number of bytes of fuzzy mask
	uint16_t	nb_sectors;			///< number of sector in track
	uint16_t	flags;				///< bitmask info for track record
	uint16_t	track_size;			///< track size in bytes
	uint8_t		track_number;			///< track number (coded for both side)
	uint8_t		record_type;			///< track image type
};

// Track descriptor flags
const uint8_t TRK_SYNC	= 0x80;		///< track_image header contains sync offset info
const uint8_t TRK_IMAGE	= 0x40;		///< track record contains track_image
const uint8_t TRK_PROT	= 0x20;		///< track contains protections ? not used
const uint8_t TRK_SECT	= 0x01;		///< track record contains sector headers

/// <summary>Address Segment</summary>
/// This structure is used to store all the information from the address field
struct Address {
	/// constructor
	Address() : track(0), head(0), number(0), size(0), crc(0) {}

	uint8_t		track;				///< Track number from address block
	uint8_t		head;				///< Head number from address block
	uint8_t		number;				///< Sector number from address block
	uint8_t		size;				///< Size number from address block
	uint16_t	crc;				///< CRC Value from address block
};


/// <summary>Pasti Sector Descriptor</summary>
/// <remarks>There is one sector image info structure for each sector found in the track</remarks>
struct SectorDesc {
	uint32_t	data_offset;		///< offset of sector data in the track record
	uint16_t	position;			///< position of the sector from start of track in bits
	uint16_t	read_time;			///< sector read time in ms
	Address		address;			///< Address field of sector
	uint8_t		fdc_status;			///< FDC status
	uint8_t		flags;				///< (always 00)
};


// Useful FDC Read Sector Status bits
const uint8_t REC_TYPE = 0x20;		///< Record Type (1 = deleted data)
const uint8_t SECT_RNF = 0x10;		///< RNF or Seek error
const uint8_t CRC_ERR  = 0x08;		///< CRC Error (in Data if RNF=0 else in ID)
// Pseudo FDC status bits used for other purpose
const uint8_t FUZZY_BITS = 0x80;	///< Sector has Fuzzy Bits
const uint8_t BIT_WIDTH  = 0x01;	///< Sector has bit width variation

// INTRA_VAR may use internal table to simulate timing (macrodos) unless timing specified


/// <summary>Contains information about one sector</summary>
/// <remarks>This class is used to store all information required to read or write a specific sector</remarks>
class Sector {
public:
	/// constructor
	Sector() : data(0), fuzzy(0), timing(0) {}
	/// destructor
	~Sector() { 
		if (data) delete [] data; 
		if (fuzzy) delete [] fuzzy ; 
		if (timing) delete [] timing; 
	}

	char*	data;				///< buffer for the sector data
	char*	fuzzy;				///< buffer for fuzzy mask bytes if necessary
	char*	timing;				///< buffer for timing bytes if necessary
	uint16_t	position;		///< position in the track of the sector address field in bits
	uint16_t	read_time;		///< read time of the track in ms
	Address	address;			///< Address field content
	uint8_t	fdc_status;			///< Status returned by the FDC
	uint8_t	flags;				///< Sector flags (not used ?)
};

/// <summary>Contains information about one Track</summary>
class Track {
public:
	Track() : buffer(0), sectors(0), nb_sectors(0) {}
	~Track() { 
		if (buffer) delete [] buffer; 
		if (sectors) delete [] sectors; 
	}

	Sector*	sectors;			///< array of Sector
	int		nb_sectors;			///< number of sectors
	char*	buffer;				///< buffer for the track data if necessary
	int		size;				///< track size
	int		number;				///< track number
	int		side;				///< track side
};

/// <summary>Contains information about a complete Floppy disk</summary>
/// <remarks>Complete floppy information (i.e. all tracks)</remarks>
class Floppy {
public:
	Floppy() : tracks(0), nb_tracks(0), version(0), tool(0) {}
	~Floppy() { 
		if (tracks) delete [] tracks; 
	}

	Track*	tracks;				///< array of Track
	int		nb_tracks;			///< number of tracks
	int		version;
	int		tool;
};

bool readPasti(FILE* in, FILE* out, Floppy* fd);
