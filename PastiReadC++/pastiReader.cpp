/*!
@file pastiReader.cpp
<summary>This file provide a function to parse a Pasti file in structures</summary>

*/

#include <string.h>
#include <stdlib.h>
#include <stdio.h>

#include "pasti.h"

#define max(a, b)((a)>(b)?(a):(b))

bool readPasti(FILE* pasti, FILE* out, Floppy* fd) {
	long start;
	FileDesc file;
	TrackDesc track;
	SectorDesc* sectors;
	bool found_timing = false;
	long pos_last_byte = 0;

	fread(&file, sizeof(FileDesc), 1, pasti );
	if (out) { 
		fprintf(out, "File Header %s version=%u tool=%x reserved1=%x tracks=%u revision=%x reserved2=%x\n", 
			file.file_id, file.version, file.tool, file.res1, file.nb_tracks, file.revision, file.res2);
	}
	
	if(!strcmp(file.file_id, "RSY")) {
		fd->nb_tracks = file.nb_tracks;			// store the number of tracks
		fd->version = file.version * 10 + file.revision;
		fd->tool = file.tool;

		fd->tracks = new Track[fd->nb_tracks];	// create an array of track

		// read all track records
		for(int tnum = 0; tnum < fd->nb_tracks; tnum++) {
			long track_record_start = ftell(pasti);					// store start pos of track header
			fread( &track, sizeof(TrackDesc), 1, pasti ); 			// read TrackDesc
			
			// create array of sector blocks for this track
			fd->tracks[tnum].sectors = new Sector[track.nb_sectors];
			fd->tracks[tnum].nb_sectors = track.nb_sectors;
			fd->tracks[tnum].size = track.track_size;
			fd->tracks[tnum].side = track.track_number >> 7;
			fd->tracks[tnum].number = track.track_number & 0x7F;

			sectors = new SectorDesc[track.nb_sectors];

			if (out) { 
				fprintf(out, "Track %03d (%d-%02d) %d bytes %d sectors Flags=%02X size=%d", 
					tnum, fd->tracks[tnum].side, fd->tracks[tnum].number, fd->tracks[tnum].size, 
					fd->tracks[tnum].nb_sectors, track.flags, track.record_size); 
				if (track.fuzzy_bytes) fprintf(out, " fuzzy=%u", track.fuzzy_bytes); 
				if (track.record_type) fprintf(out, " type=%x", track.record_type); 
				fprintf(out, " (%d-%d)\n", track_record_start, track_record_start + track.record_size);
				fprintf(out, "   Track Desc (%d-%d)\n", track_record_start, ftell(pasti));
			}

			// read sector header if specified
			if (track.flags & TRK_SECT) {
				
				for(int snum = 0; snum < track.nb_sectors; snum++) {
					start = ftell(pasti);
					fread(&sectors[snum], sizeof(SectorDesc), 1, pasti );
					if (out) {
						fprintf(out, "   Sector Desc %2d T=%2d H=%d S=%-3d Z=%d CRC=%04X Pos=%6d time=%u",
							snum, sectors[snum].address.track, sectors[snum].address.head, 
							sectors[snum].address.number, sectors[snum].address.size, sectors[snum].address.crc, 
							sectors[snum].position, sectors[snum].read_time);
						if(sectors[snum].fdc_status)
							fprintf(out, " FDC=%02X", sectors[snum].fdc_status);
						if(sectors[snum].flags)
							fprintf(out, " Flags=%02X", sectors[snum].flags);
						fprintf(out, " offset=%u (%d-%d)\n", sectors[snum].data_offset, start, ftell(pasti));
					}
					fd->tracks[tnum].sectors[snum].fdc_status = sectors[snum].fdc_status;
					fd->tracks[tnum].sectors[snum].position = sectors[snum].position;
					fd->tracks[tnum].sectors[snum].read_time = sectors[snum].read_time;
					fd->tracks[tnum].sectors[snum].address = sectors[snum].address;
					fd->tracks[tnum].sectors[snum].flags = sectors[snum].flags;
				}	// for all sectors

				// if necessary read fuzzy bytes
				char* fuzzy_mask = 0;
				if (track.fuzzy_bytes) {
					start = ftell(pasti);	// store start position for debug
					fuzzy_mask = new char[track.fuzzy_bytes];
					fread(fuzzy_mask, 1, track.fuzzy_bytes, pasti );	// read fuzzy bytes mask
					if (out)
						fprintf(out, "   Reading fuzzy mask %d bytes (%d-%d)\n", track.fuzzy_bytes, start, ftell(pasti));
				}	// fuzzy bytes to read

				short track_size = 0;
				short sync_offset = -1;
				// the track data are located after the sector desc records & fuzzy record
				long track_data_start = ftell(pasti);	// store the position of data in track record

				// read track data if specified
				if (track.flags & TRK_IMAGE) {
					start = ftell(pasti);
					// track with sync offset
					if (track.flags & TRK_SYNC)
						fread(&sync_offset, 2, 1, pasti);
					fread(&track_size, 2, 1, pasti);
					fd->tracks[tnum].buffer = new char[track_size];
					fread(fd->tracks[tnum].buffer, 1, track_size, pasti);
					if (out)
						fprintf(out, "   Reading track data %d bytes offset=%d (%d-%d)\n", track_size, sync_offset, start, ftell(pasti));
				}	// read track_data

				// read all sectors data
				char* fuzzy_pos = fuzzy_mask;	// used to transfer fuzzy mask by sector
				pos_last_byte = 0;
				for(int snum = 0; snum < track.nb_sectors; snum++) {
					// we take care of transfering the fuzzy bytes if necessary
					if (fd->tracks[tnum].sectors[snum].fdc_status & FUZZY_BITS) {
						fd->tracks[tnum].sectors[snum].fuzzy = new char[128 << sectors[snum].address.size];
						for (int i = 0; i < (128 << sectors[snum].address.size); i++)
							fd->tracks[tnum].sectors[snum].fuzzy[i] = *(fuzzy_pos++);
					}
					// create sector buffer and read info if necessary
					if (!(sectors[snum].fdc_status & SECT_RNF)) {
						fd->tracks[tnum].sectors[snum].data = new char[128 << sectors[snum].address.size];
						fseek(pasti, track_data_start, SEEK_SET);
						fseek(pasti, sectors[snum].data_offset, SEEK_CUR);
						start = ftell(pasti);
						fread(fd->tracks[tnum].sectors[snum].data, 1, 128 << sectors[snum].address.size, pasti);
						pos_last_byte = max(pos_last_byte, ftell(pasti));
						if (out) fprintf(out, "   Reading sector %2d %d bytes %s (%d-%d)\n", snum, 
							128 << sectors[snum].address.size, fd->tracks[tnum].sectors[snum].fuzzy ? "+ Fuzzy" : "",
							start, ftell(pasti));
					}	// read sector info

				}	// for all sectors

				if (out) {
					// here we check that all fuzzy bytes mask has been used
					if ((fuzzy_pos - fuzzy_mask) != track.fuzzy_bytes)
						fprintf(out, "*** Error not all fuzzy bytes %d have been read %d \n",
						track.fuzzy_bytes, fuzzy_pos - fuzzy_mask);
				}

				// check if we have timing info
				for(int snum = 0; snum < track.nb_sectors; snum++) {
					// create timing buffer and read info if necessary
					if ((sectors[snum].fdc_status & BIT_WIDTH) && (file.revision == 2)) {
						start = ftell(pasti);
						struct {
							uint16_t flag;
							uint16_t size;
						} timing_header;
						
						fseek(pasti, pos_last_byte, SEEK_SET);	// position to max last byte
						start = ftell(pasti);
						fread(&timing_header, 4, 1, pasti);	// read timing header

						fd->tracks[tnum].sectors[snum].timing = new char[timing_header.size];
						fread(fd->tracks[tnum].sectors[snum].timing, 1, timing_header.size - 4, pasti);
						if (out) 
							fprintf(out, "   Reading Timing s1=%u size=%u (%d-%d)\n",
							timing_header.flag, timing_header.size, start, ftell(pasti));
					}	// sector has timing info
				}


				if (fuzzy_mask) delete [] fuzzy_mask;
			}	// TRK_SECT provided
			else {
				// read all standard sectors
				for(int snum = 0; snum < track.nb_sectors; snum++) {
					fd->tracks[tnum].sectors[snum].data = new char[128 << sectors[snum].address.size];
					fread(fd->tracks[tnum].sectors[snum].data, 1, 128 << sectors[snum].address.size, pasti );
					fd->tracks[tnum].sectors[snum].fdc_status = 0;
					fd->tracks[tnum].sectors[snum].position = 0;
					fd->tracks[tnum].sectors[snum].read_time = 0;
					fd->tracks[tnum].sectors[snum].flags = 0;
					fd->tracks[tnum].sectors[snum].address = sectors[snum].address;
				}	// read all sector
			}	// standard non protected sectors

			fseek(pasti, track_record_start, SEEK_SET);		// reset to start of track record
			fseek(pasti, track.record_size,SEEK_CUR);			// move to next track record

			if (sectors) delete [] sectors;
			
		}	// for all tracks

	}	// pasti/pasti file

	else {
		fprintf(out, "non STX/Pasti image (bad header)");
		return false;
	}
	
	return true;
}

