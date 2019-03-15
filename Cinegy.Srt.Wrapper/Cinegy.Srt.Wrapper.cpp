/* Copyright 2019 Cinegy GmbH

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

//This class creates a limited wrapper around the SRT library, and is 
//currently really just for testing more easily on Windows with a minimum
//footprint of extra features.

//see https://github.com/Haivision/srt for details of the library.

#include "stdafx.h"
#include "Cinegy.Srt.Wrapper.h"

namespace Cinegy
{
	namespace Srt
	{

		SRTSOCKET m_bindsock = SRT_INVALID_SOCK;
		SRTSOCKET m_sock = SRT_INVALID_SOCK;
		const size_t DEFAULT_CHUNK = 1328;

		int bw_report = 0;
		unsigned stats_report_freq = 0;
		size_t chunk = DEFAULT_CHUNK;
		bool m_running = false;

		typedef std::vector<char> bytevector;
		
		sockaddr_in CreateAddrInet(const std::string& name, unsigned short port)
		{
			sockaddr_in sa;
			memset(&sa, 0, sizeof sa);
			sa.sin_family = AF_INET;
			sa.sin_port = htons(port);

			if (name != "")
			{
				if (inet_pton(AF_INET, name.c_str(), &sa.sin_addr) == 1)
					return sa;

				// XXX RACY!!! Use getaddrinfo() instead. Check portability.
				// Windows/Linux declare it.
				// See:
				//  http://www.winsocketdotnetworkprogramming.com/winsock2programming/winsock2advancedInternet3b.html
				hostent* he = gethostbyname(name.c_str());
				if (!he || he->h_addrtype != AF_INET)
					throw std::invalid_argument("SrtSource: host not found: " + name);

				sa.sin_addr = *(in_addr*)he->h_addr_list[0];
			}

			return sa;
		}
		
		bytevector Read(size_t chunk)
		{
			static size_t counter = 1;

			bytevector data(chunk * 2);
			bool ready = true;
			int stat;

			do
			{
				stat = srt_recvmsg(m_sock, data.data(), (int)chunk);

				if (stat == SRT_ERROR)
				{
					auto errDesc = UDT::getlasterror_desc();
					Console::WriteLine("recvmsg Error: {0}", gcnew String(errDesc));
					m_running = false;
					srt_close(m_bindsock);

					return bytevector();
				}

				if (stat == 0)
				{
					// Not necessarily eof. Closed connection is reported as error.
					ready = false;
				}
				
			} while (!ready);

			chunk = size_t(stat);
			if (chunk < data.size())
				data.resize(chunk);

			return data;
		}
				
		void SrtReceiver::Run()
		{
			auto managedHostname = GetHostname();

			std::string host = marshal_as<std::string>(managedHostname);
			
			auto port = _port;
			
			m_bindsock = srt_socket(AF_INET, SOCK_DGRAM, 0);
			Console::WriteLine("Socket: {0}", m_bindsock);

			sockaddr_in sa = CreateAddrInet(host, port);
			sockaddr* psa = (sockaddr*)&sa;

			Console::WriteLine("Binding a server on {0}:{1} ...", gcnew String(host.c_str()), port);

			auto stat = srt_bind(m_bindsock, psa, sizeof sa);
			if (stat == SRT_ERROR)
			{
				srt_close(m_bindsock);
				auto errDesc = UDT::getlasterror_desc();
				Console::WriteLine("srt_bind Error: {0}", gcnew String(errDesc));
			}

			stat = srt_listen(m_bindsock, 1);
			if (stat == SRT_ERROR)
			{
				auto errDesc = UDT::getlasterror_desc();
				Console::WriteLine("srt_listen Error: {0}", gcnew String(errDesc));
			}

			sockaddr_in scl;
			int sclen = sizeof scl;

			m_sock = srt_accept(m_bindsock, (sockaddr*)&scl, &sclen);
			if (m_sock == SRT_INVALID_SOCK)
			{
				auto errDesc = UDT::getlasterror_desc();
				Console::WriteLine("srt_accept Error: {0}", gcnew String(errDesc));
			}
			else
			{
				char saddr[INET_ADDRSTRLEN];
				inet_ntop(AF_INET, &(scl.sin_addr), saddr, INET_ADDRSTRLEN);
				Console::WriteLine("Connection accepted from: {0}", gcnew String(saddr));
			}
			m_running = true;

			while(m_running)
			{
				const bytevector& data = Read(chunk);
								
				FireOnDataEvent(data.data(), data.size() * sizeof(char));
			}
			
			srt_close(m_bindsock);

			return;
		}

		void SrtReceiver::Stop()
		{
			srt_close(m_bindsock);
			srt_cleanup();
			m_running = false;
		}




	}
}
