using MediaFoundation;
using MediaFoundation.Alt;
using MediaFoundation.Interop;
using MediaFoundation.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MediaFoundation.HResult;

namespace MFTestLib
{
	public enum StreamState
	{
		TypeNotSet = 0,
		Ready,
		Started,
		Stopped,
		Paused,
		Finalized,
		Count = Finalized + 1
	}

	public enum StreamOperation
	{
		SetMediaType = 0,
		Start,
		Restart,
		Pause,
		Stop,
		ProcessSample,
		PlaceMarker,
		Finalize,
		Count = Finalize + 1
	}

	public partial class ExternalStreamSink : COMBase
	{
		public StreamState State { get; protected set; } = StreamState.TypeNotSet;

		public ExternalStreamSink(ExternalMediaSink baseSink)
		{
			Debug.WriteLine("ExternalStreamSink");
			BaseSink = baseSink;
		}

		public HResult Start(long time)
		{
			Debug.WriteLine("StreamSink:Start");

			lock (this)
			{
				HResult hr = CheckShutdown();
				if (hr.Failed())
					return hr;

				if (State == StreamState.TypeNotSet || State == StreamState.Finalized)
					return MF_E_INVALIDREQUEST;

				hr = QueueEvent(MediaEventType.MEStreamSinkStarted, Guid.Empty, S_OK, null);
				if (hr.Failed())
					return hr;

				if(State != StreamState.Started)
				{
					hr = QueueEvent(MediaEventType.MEStreamSinkRequestSample, Guid.Empty, S_OK, null);
					State = StreamState.Started;
				}
			}
			return S_OK;
		}

		public HResult Stop()
		{
			Debug.WriteLine("StreamSink:Stop");

			lock (this)
			{
				HResult hr = CheckShutdown();
				if (hr.Failed())
					return hr;

				if (State == StreamState.TypeNotSet || State == StreamState.Finalized)
					return MF_E_INVALIDREQUEST;

				hr = QueueEvent(MediaEventType.MEStreamSinkStopped, Guid.Empty, S_OK, null);
				if (hr.Failed())
					return hr;

				State = StreamState.Stopped;
			}
			
			return S_OK;
		}

		public HResult Pause()
		{
			Debug.WriteLine("StreamSink:Pause");

			lock (this)
			{
				HResult hr = CheckShutdown();
				if (hr.Failed())
					return hr;

				if (State == StreamState.TypeNotSet || State == StreamState.Finalized || State == StreamState.Stopped)
					return MF_E_INVALIDREQUEST;

				hr = QueueEvent(MediaEventType.MEStreamSinkPaused, Guid.Empty, S_OK, null);
				if (hr.Failed())
					return hr;

				State = StreamState.Paused;
			}
			return S_OK;
		}

		public HResult Restart()
		{
			Debug.WriteLine("StreamSink:Restart");

			lock (this)
			{
				HResult hr = CheckShutdown();
				if (hr.Failed())
					return hr;

				if (State != StreamState.Paused)
					return MF_E_INVALIDREQUEST;

				hr = QueueEvent(MediaEventType.MEStreamSinkStarted, Guid.Empty, S_OK, null);
				if (hr.Failed())
					return hr;

				hr = QueueEvent(MediaEventType.MEStreamSinkRequestSample, Guid.Empty, S_OK, null);
				if (hr.Failed())
					return hr;

				State = StreamState.Started;
			}
			return S_OK;
		}

		public HResult Shutdown()
		{
			Debug.WriteLine("StreamSink:Shutdown");

			lock (this)
			{
				HResult hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				if(EventQueue != null)
				{
					hr = EventQueue.Shutdown();
					if (hr.Failed())
					{
						Debug.WriteLine("Could not shutdown EventQueue: " + hr.ToString() + " " + hr.GetDescription());
						Debug.WriteLine("EventQueue Shutdown: " + hr.ToString() + " " + hr.GetDescription());
					}
				}

				SafeRelease(MediaType);
				MediaType = null;

				SafeRelease(BaseSink);
				BaseSink = null;

				SafeRelease(EventQueue);
				EventQueue = null;

				State = StreamState.Finalized;
			}
			return S_OK;
		}

		public HResult CheckShutdown()
		{
			return State == StreamState.Finalized ? MF_E_SHUTDOWN : S_OK;
		}

		private void LogIfFailed(HResult hr)
		{
			if (Failed(hr))
			{
				Debug.WriteLine("HResult Error: " + hr.ToString() + " " + hr.GetDescription());
			}
		}
	}
}
