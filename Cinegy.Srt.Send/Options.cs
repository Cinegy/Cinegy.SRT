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

namespace Cinegy.Srt.Send;

internal class Options
{
    [Option('i', "inputadapter", Required = false, HelpText = "IP address of the adapter to source multicast on (if not set, tries first binding adapter).")]
    public string InputAdapterAddress { get; set; }

    [Option('m', "multicastaddress", Required = true, HelpText = "Input multicast address group to stream from.")]
    public string MulticastAddress { get; set; }

    [Option('p', "mulicastport", Required = true, HelpText = "Input multicast port to stream from.")]
    public int MulticastPort { get; set; }

    [Option("nonblockingmode", Required = false, Default = true, HelpText = "Process using non blocking socket mode")]
    public bool NonBlockingMode { get; set; }

    [Option('s', "srtport", Required = false, Default = 9000, HelpText = "UDP port for SRT connection")]
    public int SrtPort { get; set; }
}
