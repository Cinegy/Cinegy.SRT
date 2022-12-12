using CommandLine;

namespace Cinegy.Srt.Send
{
    internal class Options
    {
        [Option('m', "multicastaddress", Required = true,
        HelpText = "Input multicast address group to stream from.")]
        public string MulticastAddress { get; set; }

        [Option('p', "mulicastport", Required = true,
        HelpText = "Input multicast port to stream from.")]
        public int MulticastPort { get; set; }

        [Option('i', "inputadapter", Required = false,
        HelpText = "IP address of the adapter to source multicast on (if not set, tries first binding adapter).")]
        public string InputAdapterAddress { get; set; }

        //[Option('a', "srtaddress", Required = true,
        //HelpText = "IP address of the source listener to pull data from")]
        //public string SrtAddress { get; set; }

        [Option('s', "srtport", Required = false, Default = 9000,
        HelpText = "UDP port for SRT connection")]
        public int SrtPort { get; set; }

        [Option('q', "quiet", Required = false, Default = false,
        HelpText = "Don't print anything to the console")]
        public bool SuppressOutput { get; set; }
    }



}
