/*!
@file ProcessStream.cs
<summary> Process information from the KryoFlux Stream File</summary>

<div class="jlg">Copyright (C) 2013-2015 Jean Louis-Guerin\n\n
This file is part of the Atari Universal FD Image Tool project.\n\n
The Atari Universal FD Image Tool project may be used and distributed without restriction provided
that this copyright statement is not removed from the file and that any
derivative work contains the original copyright notice and the associated
disclaimer.\n\n
The Atari Universal FD Image Tool project is free software; you can redistribute it
and/or modify  it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 3
of the License, or (at your option) any later version.\n\n
The Atari Universal FD Image Tool project is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\n
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using KFStream;
using KFReaderPackage;

namespace KFReaderPackage {

	/// <summary>
	/// The FluxData structure contains the flux transition information
	/// </summary>
	/// <remarks>
	/// This class is used when flux samples file is provided e.g. SCP or KF-RAW file.
	/// </remarks>
	public class FluxData {
		/// <summary>
		/// Array of flux transitions recorded for this track (including all revolutions). 
		/// </summary>
		/// <remarks> The values are stored in nanoseconds</remarks>
		public int[] fluxValue;

		/// <summary>Total number of flux transitions for ALL revolutions of this track</summary>
		public int totalFluxCount;

		/// <summary>Value of the shortest flux transition recorded for this track</summary>
		/// <remarks> The values is stored in nanoseconds</remarks>
		public int minFlux;

		/// <summary>Value of the largest flux transition recorded for this track in sample clock</summary>
		/// <remarks> The values is stored in nanoseconds</remarks>
		public int maxFlux;
	}


	/// <summary>
	/// The FluxDataRev structure contains the information for one revolution of one track 
	/// This information is to be used with the FluxData class
	/// </summary>
	/// <remarks>
	/// The FluxDataRev are kept in an array with an entry for each revolution and contains 
	/// pointers to information in the FluxData class.
	/// </remarks>
	public struct FluxDataRev {
		/// <summary>
		/// The time it takes to complete a revolution in nanoseconds (time between two indexes). 
		/// </summary>
		public int revolutionTime;

		/// <summary>
		///  Index in FluxValue array of the first flux for this revolution.
		/// </summary>
		public int firstFluxIndex;

		/// <summary>
		/// The number of flux transitions for this revolution.
		/// </summary>
		public int fluxCount;
	}


	/// <summary>
	/// Class to Process all KryoFlux Stream files
	/// </summary>
	public class ProcessStream {
		private FluxData _fluxData;
		private FluxDataRev[] _fluxDataRev;
		private KFReader _reader;

		public FluxData Data { get { return _fluxData; }}
		public FluxDataRev[] Rev { get { return _fluxDataRev; }}
		public KFReader Reader { get { return _reader; } }

		/// <summary>
		/// Reads, Parses and fills the FluxData and FluxDataRev structures
		/// </summary>
		/// <param name="fileName">Complete name of the stream file to read</param>
		/// <param name="infoBox">Text box used to display debug information</param>
		/// <returns>True if file processed without problem; false if error while processing</returns>
		public bool readStreamTrack(string fileName, TextBox infoBox) {
			_reader = new KFReader();
			StreamStatus status = _reader.readStream(fileName);

			if (status == StreamStatus.sdsOk) {
				int fluxMin = Int32.MaxValue;
				int fluxMax = 0;
				double tick = 1000000000.0 / _reader.SampleClock;
				double sample = tick;
				
				_fluxData = new FluxData();
				_fluxData.fluxValue = new int[_reader.FluxCount];
				_fluxData.totalFluxCount = _reader.FluxCount;
				_fluxDataRev = new FluxDataRev[_reader.IndexCount - 1];

				// convert samples before first index
				for (int i = 0; i <= _reader.Indexes[0].fluxPosition; i++ )
					_fluxData.fluxValue[i] = (int)((double)_reader.FluxValues[i] * sample);

				for (int rev = 0; rev < _reader.IndexCount - 1; rev++) {
					// compute corrected sample clock
					//sample = 200000000.0 / (double)_reader.Indexes[rev + 1].indexTime;
					_fluxDataRev[rev].revolutionTime = (int)((double)_reader.Indexes[rev + 1].indexTime * sample);
					_fluxDataRev[rev].fluxCount = _reader.Indexes[rev + 1].fluxPosition - _reader.Indexes[rev].fluxPosition;
					int offset = _reader.Indexes[rev].fluxPosition;
					_fluxDataRev[rev].firstFluxIndex = offset;

					for (int f = 0; f < _fluxDataRev[rev].fluxCount; f++) {
						// convert samples for current revolution
						_fluxData.fluxValue[offset + f] = (int)((double)_reader.FluxValues[offset + f] * sample);
						int value = _fluxData.fluxValue[offset + f];
						if (value > fluxMax) fluxMax = value;
						if (value < fluxMin) fluxMin = value;
						if ((double)_reader.Indexes[rev].preIndexTime * sample > 10000.0) {
							// If we have a large NFA at border of revolution we split at position of the index.
							if (f == 0)
								_fluxData.fluxValue[offset + f] -= (int)((double)_reader.Indexes[rev].preIndexTime * sample);
							else if (f == (_fluxDataRev[rev].fluxCount - 1))
								_fluxData.fluxValue[offset + f] += (int)((double)(_reader.Indexes[rev].preIndexTime * sample));
						}	// NFA
					}	// all flux in current revolution
				}	// all revolutions

				_fluxData.maxFlux = fluxMax;
				_fluxData.minFlux = fluxMin;
				return true;
			}	// read correctly

			return false;
		}

		/// <summary>
		/// This is an asynchronous wrapper of the readTrack() function
		/// </summary>
		/// <param name="name">Name of the stream file to read</param>
		/// <param name="track">Store information for this track</param>
		/// <param name="side">Store the information for this side</param>
		/// <returns>True if file processed without problem; false if error while processing</returns>
		/// See also <see cref="readTrack()"/> function.
		public Task<bool> readStreamTrackAsync(string name, TextBox infoBox) {
			return Task<bool>.Run(() => readStreamTrack(name, infoBox));
		}

	}	// streamProcess Class
}	// name space