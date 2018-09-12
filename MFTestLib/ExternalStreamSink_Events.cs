using MediaFoundation;
using MediaFoundation.Alt;
using MediaFoundation.Misc;
using static MediaFoundation.HResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MFTestLib
{
	internal class Marker
	{
		public MFStreamSinkMarkerType Type;
		public PropVariant MarkerValue;
		public PropVariant ContextValue;

		public Marker(MFStreamSinkMarkerType type, PropVariant markerValue, PropVariant contextValue)
		{
			Type = type;
			MarkerValue = markerValue;
			ContextValue = contextValue;
		}
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("9A643F95-F59C-4287-9235-A7EB0999C9EF")]
	public partial class ExternalStreamSink : IMFStreamSinkAlt, IMFMediaEventGeneratorAlt
	{
		private IMFMediaEventQueueAlt EventQueue;
		private ExternalMediaSink BaseSink;

		#region IMFMediaEventGeneratorAlt

		private void GenEventQueue()
		{
			if (EventQueue == null)
				MFExternAlt.MFCreateEventQueue(out EventQueue);
		}

		public HResult GetEvent(MFEventFlag dwFlags, out IMFMediaEvent ppEvent)
		{
			Debug.WriteLine("StreamSink:GetEvent");

			HResult hr;
			IMFMediaEventQueueAlt queue = null;
			ppEvent = null;

			lock (this)
			{
				hr = CheckShutdown();
				LogIfFailed(hr);
				
				if(Succeeded(hr))
				{
					GenEventQueue();
					queue = EventQueue;
					Marshal.AddRef(Marshal.GetIUnknownForObject(queue));
				}
			}
			if (Succeeded(hr))
			{
				hr = queue.GetEvent(dwFlags, out ppEvent);
				LogIfFailed(hr);
			}

			SafeRelease(queue);

			return hr;
		}

		public HResult BeginGetEvent(IntPtr pCallback, object o)
		{
			Debug.WriteLine("StreamSink:BeginGetEvent");

			HResult hr;

			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				GenEventQueue();
				hr = EventQueue.BeginGetEvent(pCallback, o);
				LogIfFailed(hr);
			}

			return hr;
		}

		public HResult EndGetEvent(IntPtr pResult, out IMFMediaEvent ppEvent)
		{
			Debug.WriteLine("StreamSink:EndGetEvent");

			HResult hr;
			ppEvent = null;

			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				GenEventQueue();
				hr = EventQueue.EndGetEvent(pResult, out ppEvent);
				LogIfFailed(hr);
			}

			return hr;
		}

		public HResult QueueEvent(MediaEventType met, Guid guidExtendedType, HResult hrStatus, ConstPropVariant pvValue)
		{
			Debug.WriteLine("StreamSink:QueueEvent");

			HResult hr;

			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				GenEventQueue();
				hr = EventQueue.QueueEventParamVar(met, guidExtendedType, hrStatus, pvValue);
				LogIfFailed(hr);
			}
			return hr;
		}

		#endregion

		#region IMFStreamSinkAlt

		public HResult GetMediaSink(out IMFMediaSinkAlt ppMediaSink)
		{
			Debug.WriteLine("StreamSink:GetMediaSink");

			ppMediaSink = null;
			HResult hr = BaseSink == null ? E_INVALIDARG : S_OK;
			if (Failed(hr))
				return hr;

			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				ppMediaSink = BaseSink;
			}

			return hr;
		}

		public HResult GetIdentifier(out int pdwIdentifier)
		{
			Debug.WriteLine("StreamSink:GetIdentifier");

			pdwIdentifier = -1;
			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				pdwIdentifier = 0;
			}

			return hr;
		}

		public HResult GetMediaTypeHandler(out IMFMediaTypeHandler ppHandler)
		{
			Debug.WriteLine("StreamSink:GetMediaTypeHandler");

			ppHandler = null;
			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				try
				{
					ppHandler = (IMFMediaTypeHandler)this;
					hr = S_OK;
				}catch(InvalidCastException ex)
				{
					hr = E_NOINTERFACE;
				}
				LogIfFailed(hr);
			}

			return hr;
		}

		public HResult ProcessSample(IMFSample pSample)
		{
			Debug.WriteLine("StreamSink:ProcessSample");

			if (pSample == null)
				return E_INVALIDARG;

			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				hr = ProcessSampleInternal(pSample);
				if (Failed(hr))
					return hr;

				hr = QueueEvent(MediaEventType.MEStreamSinkRequestSample, Guid.Empty, S_OK, null);
				if (Failed(hr))
					return hr;
			}
			return hr;
		}

		public HResult PlaceMarker(MFStreamSinkMarkerType eMarkerType, ConstPropVariant pvarMarkerValue, ConstPropVariant pvarContextValue)
		{
			Debug.WriteLine("StreamSink:PlaceMarker");

			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				PropVariant markerValue = new PropVariant();
				PropVariant contextValue = new PropVariant();
				pvarMarkerValue?.Copy(markerValue);
				pvarContextValue?.Copy(contextValue);
				Marker m = new Marker(eMarkerType, markerValue, contextValue);

				hr = ProcessSampleInternal(m);
				if (Failed(hr))
					return hr;
			}
			return hr;
		}

		public HResult Flush()
		{
			Debug.WriteLine("StreamSink:Flush");

			return S_OK;
		}
	
		private HResult ProcessSampleInternal(object o)
		{
			if(o is Marker)
			{
				Marker m = o as Marker;
				QueueEvent(MediaEventType.MEStreamSinkMarker, Guid.Empty, S_OK, m.ContextValue);
				return QueueEvent(MediaEventType.MEStreamSinkRequestSample, Guid.Empty, S_OK, null);
			}
			else if(o is IMFSample)
			{
				IMFSample s = o as IMFSample;
				HResult hr = BaseSink.ProcessSample(s);
				SafeRelease(s);
				return hr;
			}
			else
			{
				return E_INVALIDARG;
			}
		}

		#endregion
	}
}
