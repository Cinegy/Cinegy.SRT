using System.Diagnostics;
using CommandLine;

namespace Cinegy.Srt.Recv
{
    internal class Options
    {
        [Option('m', "multicastaddress", Required = true,
        HelpText = "Input multicast address group to stream to.")]
        public string MulticastAddress { get; set; }

        [Option('p', "mulicastport", Required = true,
        HelpText = "Input multicast port to stream to.")]
        public int MulticastPort { get; set; }

        [Option('o', "outputadapter", Required = false,
        HelpText = "IP address of the adapter to emit multicast on (if not set, tries first binding adapter).")]
        public string OutputAdapterAddress { get; set; }

        [Option('i', "inputadapter", Required = true,
        HelpText = "IP address of the adapter to listen for SRT connections")]
        public string InputAdapterAddress { get; set; }

        [Option('s', "srtport", Required = false, Default = 9000,
        HelpText = "UDP port to listen for inbound SRT data on.")]
        public int SrtPort { get; set; }

        [Option('q', "quiet", Required = false, Default = false,
        HelpText = "Don't print anything to the console")]
        public bool SuppressOutput { get; set; }
    }



}
