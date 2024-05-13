using System;
using System.Collections.Generic;
using auxmic.editorExport;
using auxmic.logging;

namespace auxmic.mediaUtil
{
    public class MediaTool : IMediaTool
    {
        private readonly CommandLineCapturer _capturer;
        private readonly AuxMicLog _log;
        public MediaTool(AuxMicLog log, string exeName)
        {
            _log = log;
            _capturer = new CommandLineCapturer(log, exeName, "ffprobe", "");
        }
        
        public MediaProperties LoadMetadata(string filename)
        {
            List<string> mediaData = _capturer.Execute(
                // -select_streams v:0  to limit to first video stream
                $"-v error -select_streams v:0  -show_entries stream=codec_type,width,height,r_frame_rate,time_base,sample_aspect_ratio -i \"{filename}\"");

            foreach (var line in mediaData)
            {
                _log.Log(line);
            }
            
            Dictionary<string, string> map = ConvertToMap(mediaData);

            string value;
            bool isVideo = map.TryGetValue("codec_type", out value) && value == "video";
            map.TryGetValue("sample_aspect_ratio", out var par);

            int width = getIntWithDefault(map, "width", 0);
            int height = getIntWithDefault(map, "height", 0);
            
            float frameRate = 0f;
            if (map.TryGetValue("r_frame_rate", out value))
            {
                var fraction = SplitFraction(value);
                if (fraction.Denominator != 0) {
                    frameRate = (float) fraction.Nominator / fraction.Denominator;
                }
            }
            
            float timeBase = 0f;
            if (map.TryGetValue("time_base", out value))
            {
                var fraction = SplitFraction(value);
                if (fraction.Nominator != 1)
                {
                    timeBase = frameRate;
                }
                else
                {
                    timeBase = fraction.Denominator;    
                }
            }

            return new MediaProperties(isVideo, width, height, par, frameRate, timeBase);
        }

        private int getIntWithDefault(Dictionary<string, string> map, string propertyName, int defaultValue = 0)
        {
            int result = defaultValue;
            if (map.TryGetValue(propertyName, out var value)) { 
                result = int.Parse(value);
            }

            return result;
        }

        private static Dictionary<string, string> ConvertToMap(List<string> inputStrings)
        {
            var map = new Dictionary<string, string>();

            foreach (string str in inputStrings)
            {
                string[] parts = str.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0];
                    string value = parts[1];
                    map[key] = value;
                }
            }

            return map;
        }
        
        private static Fraction SplitFraction(string expression)
        {
            string[] parts = expression.Split('/');
            
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid expression format. Expected 'numerator/denominator'.");
            }

            if (!int.TryParse(parts[0], out int numerator) || !int.TryParse(parts[1], out int denominator))
            {
                throw new ArgumentException("Invalid expression format. Expected numeric values.");
            }

            return new Fraction(numerator, denominator);
        }
    }

    readonly struct Fraction
    {
        public int Nominator { get; }
        public int Denominator { get; }

        public Fraction(int nominator, int denominator)
        {
            this.Nominator = nominator;
            this.Denominator = denominator;
        }
    }
}