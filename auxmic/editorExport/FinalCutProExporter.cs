using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using auxmic.mediaUtil;

namespace auxmic.editorExport
{
    // Create a "Final Cut Pro 7" project file that Davinci Resolve, Magix Vegas, and Adobe Premiere 2018 can load.
    public class FinalCutProExporter : IEditorExporter
    {
        private double offsetAdjustmentMilliseconds;
        private double totalDurationSeconds;
        private IMediaTool _mediaTool;
        private float timebase;
        
        public FinalCutProExporter(IMediaTool mediaTool)
        {
            _mediaTool = mediaTool;
        }

        public void Export(Clip master, IList<Clip> clips, bool wantSecondaryAudio, TextWriter output)
        {
            timebase = 0;
            MediaProperties masterMetadata = _mediaTool.LoadMetadata(master.Filename);
            if (masterMetadata.IsVideo)
            {
                timebase = masterMetadata.FrameRate;
            }
            
            Dictionary<string, MediaProperties> mediaPropertiesMap = new Dictionary<string, MediaProperties>();
            foreach (var clip in clips)
            {
                var mediaProperties = _mediaTool.LoadMetadata(clip.Filename);
                mediaPropertiesMap.Add(clip.Filename, mediaProperties);
                if (timebase == 0 && mediaProperties.IsVideo)
                {
                    timebase = mediaProperties.FrameRate;
                }
            }
            
            var completeList = new List<Clip>();
            completeList.Add(master);
            completeList.AddRange(clips);

            CalculateAdjustments(completeList);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = ("    "),
                CloseOutput = true,
                OmitXmlDeclaration = true
            };

            using XmlWriter writer = XmlWriter.Create(output, settings);
            writer.WriteStartElement("xmeml");
            writer.WriteAttributeString("version", "5");

            WriteStartSequence(writer, master);

            writer.WriteStartElement("video");
            
            bool videoFormatWritten = false;
            if (masterMetadata.IsVideo)
            {
                videoFormatWritten = true;
                WriteVideoFormat(writer, masterMetadata);
                WriteVideoTrack(writer, master, masterMetadata);
            }
            
            foreach (var clip in clips)
            {
                var clipMetaData = mediaPropertiesMap.GetValueOrDefault(clip.Filename);
                if (!videoFormatWritten && clipMetaData is {IsVideo: true})
                {
                    videoFormatWritten = true;
                    WriteVideoFormat(writer, clipMetaData);
                }
                WriteVideoTrack(writer, clip, clipMetaData);
            }

            writer.WriteEndElement(); // video

            // Here we are only writing the audio for the master track
            writer.WriteStartElement("audio");
            WriteAudioTrack(writer, master, masterMetadata.IsVideo);

            if (wantSecondaryAudio)
            {
                foreach (var clip in clips)
                {
                    var clipMetaData = mediaPropertiesMap.GetValueOrDefault(clip.Filename);
                    WriteAudioTrack(writer, clip, clipMetaData.IsVideo);
                }
            }

            writer.WriteEndElement(); // audio

            WriteEndSequence(writer);

            writer.WriteEndElement(); // xmeml
            writer.Flush();
        }

        // Figure out the "left most" track so we get the track offsets correct in the NLE.
        // Figure out the "right most" track so we can calculate the duration of all tracks.
        private void CalculateAdjustments(List<Clip> completeList)
        {
            double leftMost = 0;
            double rightMost = 0;
            for (int i = 0; i < completeList.Count; i++)
            {
                var clip = completeList[i];
                if (clip.Offset.TotalMilliseconds < leftMost)
                {
                    leftMost = clip.Offset.TotalMilliseconds;
                }

                var clipDurationInMilliseconds = clip.DataLength / clip.WaveFormat.SampleRate * 1000;
                if (clip.Offset.TotalMilliseconds + clipDurationInMilliseconds > rightMost)
                {
                    rightMost = clip.Offset.TotalMilliseconds + clipDurationInMilliseconds;
                }
            }

            // rightMost = rightMost + Math.Abs(leftMost);
            // double leftMost = completeList.Select(clip => clip.Offset.TotalMilliseconds).Prepend(0).Min();

            totalDurationSeconds = (rightMost + Math.Abs(leftMost)) / 1000;
            offsetAdjustmentMilliseconds = leftMost;
        }

        private void WriteVideoFormat(XmlWriter videoWriter, MediaProperties mediaProperties)
        {
            videoWriter.WriteStartElement("format");
            WriteVideoSampleCharacteristics(videoWriter, mediaProperties);
            videoWriter.WriteEndElement(); // format
        }

        private void WriteAudioTrack(XmlWriter writer, Clip clip, bool clipIsVideo)
        {
            // Each channel should be its own track
            for (int channel=1; channel <= clip.WaveFormat.Channels; channel++)
            {
                WriteAudioChannel(channel);
            }

            void WriteAudioChannel(int channel)
            {
                writer.WriteStartElement("track");
                writer.WriteStartElement("clipitem");

                WriteMostOfClipItem(writer, clip, "audio", channel);

                // Write the track duration
                var length = FinalCutDuration(clip);
                var offset = FinalCutOffset(clip.Offset.TotalMilliseconds);
                WriteInOutStartOut(writer, 0, length, offset,
                    length + offset);

                writer.WriteStartElement("file");
                writer.WriteAttributeString("id", clip.DisplayName + "_file");
                if (!clipIsVideo)
                {
                    writer.WriteElementString("pathurl", "file://" + clip.Filename.Replace("\\", "/"));
                }

                writer.WriteEndElement(); // file

                WriteSourceTrack(writer, "audio", channel.ToString());

                WriteLinks(writer, clip, clipIsVideo);
                
                writer.WriteEndElement(); // clipitem
                writer.WriteEndElement(); // track
            }
        }

        private void WriteLinks(XmlWriter writer, Clip master, bool masterIsVideo)
        {
            void WriteLinkLinks(int trackindex, string mediatype, string clipref)
            {
                writer.WriteStartElement("link");
                writer.WriteElementString("linkclipref", clipref);
                writer.WriteElementString("mediatype", mediatype);
                writer.WriteElementString("trackindex", trackindex.ToString());
                writer.WriteElementString("clipindex", "1");
                writer.WriteElementString("groupindex", "1");
                writer.WriteEndElement(); // link
            }

            int startProjectTrack = 0;
            if (masterIsVideo)
            {
                WriteLinkLinks(++startProjectTrack, "video", GenerateClipItemRef(master, "video", 0));
            }

            for (int otherchannel = 1; otherchannel <= master.WaveFormat.Channels; otherchannel++)
            {
                WriteLinkLinks(otherchannel + startProjectTrack, "audio",
                    GenerateClipItemRef(master, "audio", otherchannel));
            }
        }

        private int FinalCutOffset(double offsetMilliseconds)
        {
            var offsetMillis = (offsetMilliseconds - offsetAdjustmentMilliseconds);
            return (int) ((offsetMillis / 1000) * timebase);
        }

        private void WriteStartSequence(XmlWriter writer, Clip clip)
        {
            writer.WriteStartElement("sequence");
            writer.WriteElementString("name", "Sequence 1");

            writer.WriteElementString("duration",
                (totalDurationSeconds * timebase).ToString(CultureInfo.InvariantCulture));

            WriteRate(writer);
            writer.WriteElementString("in", "-1");
            writer.WriteElementString("out", "-1");

            WriteTimecode(writer);

            writer.WriteStartElement("media");
        }

        private void WriteEndSequence(XmlWriter writer)
        {
            writer.WriteEndElement(); // media
            writer.WriteEndElement(); // sequence
        }

        private void WriteVideoTrack(XmlWriter writer, Clip clip, MediaProperties mediaProperties)
        {
            writer.WriteStartElement("track");
            writer.WriteStartElement("clipitem");
            WriteMostOfClipItem(writer, clip, "video", 0);

            var duration = FinalCutDuration(clip);
            var offset = FinalCutOffset(clip.Offset.TotalMilliseconds);
            writer.WriteElementString("duration", duration.ToString());

            WriteRate(writer);

            WriteInOutStartOut(writer, 0, duration, offset, offset + duration);

            // file id           
            //------------- file -----------------
            writer.WriteStartElement("file");
            writer.WriteAttributeString("id", clip.DisplayName + "_file");
            WriteDuration(writer, clip);
            WriteRate(writer);
            writer.WriteElementString("name", clip.DisplayName);
            writer.WriteElementString("pathurl", "file://" + clip.Filename.Replace("\\", "/"));

            //------------- timecode --------------
            WriteTimecode(writer);

            //------------- media --------------------
            writer.WriteStartElement("media");
            writer.WriteStartElement("video");
            WriteDuration(writer, clip);
            WriteVideoSampleCharacteristics(writer, mediaProperties);
            writer.WriteEndElement(); // video

            writer.WriteStartElement("audio");
            writer.WriteElementString("channelcount", clip.WaveFormat.Channels.ToString());
            writer.WriteEndElement(); // audio

            writer.WriteEndElement(); // media
            writer.WriteEndElement(); // file

            writer.WriteElementString("compositemode", "normal");

            writer.WriteEndElement(); // clipitem
            writer.WriteElementString("enabled", "TRUE");
            writer.WriteElementString("locked", "FALSE");
            writer.WriteEndElement(); // track
        }

        private void WriteVideoSampleCharacteristics(XmlWriter writer, MediaProperties mediaProperties)
        {
            DetermineAnamorphicAndPar(mediaProperties, out var anamorphic, out var finalCutPar);

            writer.WriteStartElement("samplecharacteristics");
            writer.WriteElementString("width", mediaProperties.Width.ToString());
            writer.WriteElementString("height", mediaProperties.Height.ToString());
            writer.WriteElementString("anamorphic", anamorphic);
            writer.WriteElementString("pixelaspectratio", finalCutPar);
            writer.WriteElementString("fielddominance", "none");
            WriteRate(writer);
            writer.WriteElementString("colordepth", "24");

            writer.WriteEndElement(); // samplecharacteristics
        }

        private static void DetermineAnamorphicAndPar(MediaProperties mediaProperties, out string anamorphic,
            out string finalCutPar)
        { /*
          40:33:            NTSC DV anamorphic widescreen
          118:81:           PAL DV anamorphic widescreen

          square, 1:1
          NTSC-601, 10-:11 
          PAL-601, 59:54
          DVCPROHD-720P or HD-(960x720), 4:3
          DVCPROHD-1080i60 or HD-(1280x1080),  1.5 == 3:2
          DVCPROHD-1080i50 or HD-(1440x1080).  1.33 == 4:3
         */
            anamorphic = "FALSE";
            finalCutPar = "square";
            if (mediaProperties.Par == "10:11")
            {
                finalCutPar = "NTSC-601";
            }
            else if (mediaProperties.Par == "40:33")
            {
                finalCutPar = "NTSC-601";
                anamorphic = "TRUE";
            }
            else if (mediaProperties.Par == "59:54")
            {
                finalCutPar = "PAL-601";
            }
            else if (mediaProperties.Par == "118:81")
            {
                finalCutPar = "PAL-601";
                anamorphic = "TRUE";
            }
            else if (mediaProperties.Par == "4:3")
            {
                if (mediaProperties.Width == 960 && mediaProperties.Height == 720)
                {
                    finalCutPar = "HD-(960x720)";
                }
                else if (mediaProperties.Width == 1440 && mediaProperties.Height == 1080)
                {
                    finalCutPar = "HD-(1440x1080)";
                }
            }
            else if (mediaProperties.Par == "3:2" && mediaProperties.Width == 1280 && mediaProperties.Height == 1080)
            {
                finalCutPar = "HD-(1280x1080)";
            }
        }

        private void WriteTimecode(XmlWriter writer)
        {
            writer.WriteStartElement("timecode");
            writer.WriteElementString("displayformat", "NDF");
            WriteRate(writer);
            writer.WriteEndElement(); // timecode
        }

        private static void WriteSourceTrack(XmlWriter writer, string mediaType, string trackIndex)
        {
            writer.WriteStartElement("sourcetrack");
            writer.WriteElementString("mediatype", mediaType);
            writer.WriteElementString("trackindex", trackIndex);
            writer.WriteEndElement(); // sourcetrack
        }

        private string GenerateClipItemRef(Clip clip, string mediaType, int channel)
        {
            return clip.DisplayName + "_" + mediaType + " " + channel;
        }
        
        private void WriteMostOfClipItem(XmlWriter writer, Clip clip, string mediatype, int channel)
        {
            writer.WriteAttributeString("id", GenerateClipItemRef(clip, mediatype, channel));
            writer.WriteElementString("masterclipid", "master-clip-5"); // TODO: doesn't matter what?

            writer.WriteElementString("name", clip.DisplayName);
            WriteDuration(writer, clip);

            WriteRate(writer);
        }

        private void WriteDuration(XmlWriter writer, Clip clip)
        {
            var duration = FinalCutDuration(clip);
            writer.WriteElementString("duration", duration.ToString());
        }

        private long FinalCutDuration(Clip clip)
        {
            float duration = (clip.DataLength / (float) clip.WaveFormat.SampleRate) * timebase;
            return (long) duration;
        }

        private void WriteInOutStartOut(XmlWriter writer, long _in, long _out, long start, long end)
        {
            writer.WriteElementString("start", start.ToString());
            writer.WriteElementString("end", end.ToString());
            writer.WriteElementString("enabled", "TRUE");
            writer.WriteElementString("in", _in.ToString());
            writer.WriteElementString("out", _out.ToString());
        }

        private void WriteRate(XmlWriter writer)
        {
            writer.WriteStartElement("rate");
            writer.WriteElementString("ntsc", "FALSE");
            writer.WriteElementString("timebase", timebase.ToString());
            writer.WriteEndElement(); // rate
        }
    }
}