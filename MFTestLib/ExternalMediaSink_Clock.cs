using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaFoundation;
using static MediaFoundation.HResult;

namespace MFTestLib
{
	public partial class ExternalMediaSink : IMFClockStateSink
	{
		protected IMFPresentationClock PresentationClock { get; set; }

		public HResult OnClockStart(long hnsSystemTime, long llClockStartOffset)
		{
			Debug.WriteLine("MediaSink:OnClockStart");

			HResult hr;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				hr = StreamSink.Start(llClockStartOffset);
				CurrentFrame = 0;
			}

			return hr;
		}

		public HResult OnClockStop(long hnsSystemTime)
		{
			Debug.WriteLine("MediaSink:OnClockStop");

			HResult hr;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				hr = StreamSink.Stop();
			}
			return hr;
		}

		public HResult OnClockPause(long hnsSystemTime)
		{
			Debug.WriteLine("MediaSink:OnClockPause");

			HResult hr;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				hr = StreamSink.Pause();
			}
			return hr;
		}

		public HResult OnClockRestart(long hnsSystemTime)
		{
			Debug.WriteLine("MediaSink:OnClockRestart");

			HResult hr;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				hr = StreamSink.Restart();
			}
			return hr;
		}

		public HResult OnClockSetRate(long hnsSystemTime, float flRate)
		{
			Debug.WriteLine("MediaSink:OnClockSetRate");

			if (IsShutdown)
				return MF_E_SHUTDOWN;

			return S_OK;
		}
	}
}
