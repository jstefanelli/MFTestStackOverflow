using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaFoundation;
using MediaFoundation.Alt;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using static MediaFoundation.HResult;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MFTestLib
{
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("E7816350-9CC9-492C-8685-BD8E7CEBCEC9")]
	public partial class ExternalMediaSink : COMBase, IMFMediaSinkAlt
	{
		protected bool IsShutdown { get; set; } = false;
		protected long CurrentFrame { get; set; } = 0;
		protected ExternalStreamSink StreamSink { get; set; }

		public ExternalMediaSink()
		{
			StreamSink = new ExternalStreamSink(this);
			CurrentFrame = 0;
			IsShutdown = false;
		}

		public HResult GetCharacteristics(out MFMediaSinkCharacteristics pdwCharacteristics)
		{
			Debug.WriteLine("MediaSink:GetCharacteristics");

			pdwCharacteristics = MFMediaSinkCharacteristics.None;

			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				pdwCharacteristics = MFMediaSinkCharacteristics.FixedStreams;
			}

			return S_OK;
		}

		public HResult AddStreamSink(int dwStreamSinkIdentifier, IMFMediaType pMediaType, out IMFStreamSinkAlt ppStreamSink)
		{
			Debug.WriteLine("MediaSink:AddStreamSink");
			ppStreamSink = null;
			return MF_E_STREAMSINKS_FIXED;
		}

		public HResult RemoveStreamSink(int dwStreamSinkIdentifier)
		{
			Debug.WriteLine("MediaSink:RemoveStreamSink");
			return MF_E_STREAMSINKS_FIXED;
		}

		public HResult GetStreamSinkCount(out int pcStreamSinkCount)
		{
			Debug.WriteLine("MediaSink:GetStreamSinkCount");

			pcStreamSinkCount = 0;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				pcStreamSinkCount = 1;
			}
			return S_OK;
		}

		public HResult GetStreamSinkByIndex(int dwIndex, out IMFStreamSinkAlt ppStreamSink)
		{
			Debug.WriteLine("MediaSink:GetStreamSinkByIndex");

			ppStreamSink = null;
			if (dwIndex != 0)
				return MF_E_INVALIDINDEX;

			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				ppStreamSink = StreamSink;
			}

			return S_OK;
		}

		public HResult GetStreamSinkById(int dwStreamSinkIdentifier, out IMFStreamSinkAlt ppStreamSink)
		{
			Debug.WriteLine("MediaSink:GetStreamSinkById");

			ppStreamSink = null;
			if (dwStreamSinkIdentifier != 0)
				return MF_E_INVALIDSTREAMNUMBER;

			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				ppStreamSink = StreamSink;
			}

			return S_OK;
		}

		public HResult SetPresentationClock(IMFPresentationClock pPresentationClock)
		{
			Debug.WriteLine("MediaSink:SetPresentationClock");

			HResult hr = S_OK;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				if(PresentationClock != null)
				{
					hr = PresentationClock.RemoveClockStateSink(this);
					if (Failed(hr))
						return hr;
				}

				if(pPresentationClock != null)
				{
					hr = pPresentationClock.AddClockStateSink(this);
					if (Failed(hr))
						return hr;
				}

				if(pPresentationClock != null)
					PresentationClock = pPresentationClock;

			}
			return hr;
		}

		public HResult GetPresentationClock(out IMFPresentationClock ppPresentationClock)
		{
			Debug.WriteLine("MediaSink:GetPResentationClock");

			ppPresentationClock = null;
			lock (this) {
				if (PresentationClock == null)
					return MF_E_NO_CLOCK;
				else
					ppPresentationClock = PresentationClock;
			}

			return S_OK;
		}

		public HResult Shutdown()
		{
			Debug.WriteLine("MediaSink:Shutdown");

			HResult hr = S_OK;
			lock (this)
			{
				if (IsShutdown)
					return MF_E_SHUTDOWN;

				if(StreamSink != null)
				{
					hr = StreamSink.Shutdown();
					if (Failed(hr))
						return hr;
				}

				SafeRelease(StreamSink);
				SafeRelease(PresentationClock);

				IsShutdown = true;

			}
			return hr;
		}

		internal HResult ProcessSample(IMFSample s)
		{
			Debug.WriteLine("Received sample!");
			SafeRelease(s);
			return HResult.S_OK;
		}
		
	}
}
