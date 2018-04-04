// Cinegy.Srt.Wrapper.h

#pragma once
using namespace System;
using namespace msclr::interop;

namespace Cinegy
{
	namespace Srt
	{
		public delegate void OnDataEventHandler(const char* data, size_t dataSize);

		public ref class SrtReceiver
		{
		public:
			//Helper(void);
			void SrtReceiver::Run();
			void SrtReceiver::Stop();
			int GetPort() { return _port; };
			void SetPort(int value) { _port = value; }
			void SetHostname(String^ value) { _strHostname = _strHostname->Copy(value); }
			String^ GetHostname() { return _strHostname; }
			event OnDataEventHandler ^ OnDataReceived;
			
		private:
			int _port;
			String^ _strHostname;
			void FireOnDataEvent(const char* data, size_t size) { OnDataReceived(data, size); }

		};
	}
}
