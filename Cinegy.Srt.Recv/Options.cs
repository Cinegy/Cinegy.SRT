//  Copyright 2019-2023 Cinegy GmbH
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CommandLine;

namespace Cinegy.Srt.Recv;

internal class Options
{
    [Option('m', "multicastaddress", Required = true, HelpText = "Output multicast address group to stream to.")]
    public string MulticastAddress { get; set; }

    [Option('p', "mulicastport", Required = true, HelpText = "Output multicast port to stream to.")]
    public int MulticastPort { get; set; }

    [Option('o', "outputadapter", Required = false, HelpText = "IP address of the adapter to emit multicast on (if not set, tries first binding adapter).")]
    public string OutputAdapterAddress { get; set; }

    [Option('a', "srtaddress", Required = true, HelpText = "IP address of the source listener to pull data from")]
    public string SrtAddress { get; set; }

    [Option('s', "srtport", Required = false, Default = 9000, HelpText = "UDP port on the source listener to connect to")]
    public int SrtPort { get; set; }

    [Option('q', "quiet", Required = false, Default = false, HelpText = "Don't print anything to the console")]
    public bool SuppressOutput { get; set; }
}
