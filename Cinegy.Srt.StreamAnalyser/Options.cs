﻿using CommandLine;

namespace Cinegy.Srt.StreamAnalyser
{
    internal class Options
    {
        [Option('q', "quiet", Required = false, Default = false,
        HelpText = "Don't print anything to the console")]
        public bool SuppressOutput { get; set; }

        [Option('l', "logfile", Required = false,
        HelpText = "Optional file to record events to.")]
        public string LogFile { get; set; }

        [Option("skipdecodetransportstream", Required = false, Default = false,
        HelpText = "Optional instruction to skip decoding further TS and DVB data and metadata")]
        public bool SkipDecodeTransportStream { get; set; }

        [Option('c', "teletextdecode", Required = false, Default = false,
        HelpText = "Optional instruction to decode DVB teletext subtitles / captions from default program")]
        public bool DecodeTeletext { get; set; }

        [Option("programnumber", Required = false,
        HelpText = "Pick a specific program / service to inspect (otherwise picks default).")]
        public ushort ProgramNumber { get; set; }

        [Option('d', "descriptortags", Required = false, Default = "",
        HelpText = "Comma separated tag values added to all log entries for instance and machine identification")]
        public string DescriptorTags { get; set; }

        [Option('v', "verboselogging", Required = false,
        HelpText = "Creates event logs for all discontinuities and skips.")]
        public bool VerboseLogging { get; set; }

        [Option('t', "telemetry", Required = false, Default = false,
        HelpText = "Enable telemetry to Cinegy Telemetry Server")]
        public bool TelemetryEnabled { get; set; }

        [Option('o', "organization", Required = false,
        HelpText = "Tag all telemetry with this organization (needed to indentify and access telemetry from Cinegy Analytics portal")]
        public string OrganizationId { get; set; }
    }

    // Define a class to receive parsed values using stream verb
    [Verb("stream", HelpText = "Stream from the network.")]
    internal class StreamOptions : Options
    {
        [Option('a', "srtaddress", Required = true,
            HelpText = "IP address of the source listener to pull data from")]
        public string SrtAddress { get; set; }

        [Option('s', "srtport", Required = false, Default = 9000,
            HelpText = "UDP port on the source listener to connect to")]
        public int SrtPort { get; set; }

        [Option('i', "adapter", Required = false,
        HelpText = "IP address of the adapter to listen for SRT connections (if not set, tries first binding adapter).")]
        public string AdapterAddress { get; set; }

        [Option( "nortpheaders", Required = false, Default = true,
        HelpText = "Optional instruction to skip the expected 12 byte RTP headers (meaning plain MPEGTS inside UDP is expected")]
        public bool NoRtpHeaders { get; set; }

        [Option("interarrivaltime", Required = false, Default = 40,
        HelpText = "Maximum permitted time between UDP packets before alarming.")]
        public int InterArrivalTimeMax { get; set; }

        [Option('h', "savehistoricaldata", Required = false, Default = false,
        HelpText = "Optional instruction to save and then flush to disk recent TS data on stream problems.")]
        public bool SaveHistoricalData { get; set; }

        [Option('e', "timeserieslogging", Required = false,
        HelpText = "Record time slice metric data to log file.")]
        public bool TimeSeriesLogging { get; set; }

    }

}
