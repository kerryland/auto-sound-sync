using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NAudio.Wave;

namespace auxmic.editorExport
{
    // Create a "Final Cut Pro 7" project file that Davinci Resolve and Magix Vegas can load.
    //  
    // This file cannot be imported into Adobe Premiere 2018. I can't tell you why not.
    public class FinalCutProExporter : IEditorExporter
    {
        private static int timebase = 25;

        private double offsetAdjustmentMilliseconds;
        private double totalDurationSeconds;
        private IMediaTool _mediaTool;

        public FinalCutProExporter(IMediaTool mediaTool)
        {
            _mediaTool = mediaTool;
        }

        public void Export(Clip master, IList<Clip> clips, TextWriter output)
        {
            bool masterIsVideo = _mediaTool.IsVideo(master.Filename);
            
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
            WriteVideoFormat(writer);

            if (masterIsVideo)
            {
                WriteVideoTrack(writer, master);
            }

            foreach (var clip in clips)
            {
                WriteVideoTrack(writer, clip);
            }
            writer.WriteEndElement(); // video

            writer.WriteStartElement("audio");
            WriteAudioTrack(writer, master, masterIsVideo);
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

        private void WriteVideoFormat(XmlWriter videoWriter)
        {
            videoWriter.WriteStartElement("format");
            WriteVideoSampleCharacteristics(videoWriter);
            videoWriter.WriteEndElement(); // format
        }

        private void WriteAudioTrack(XmlWriter writer, Clip clip, bool masterIsVideo)
        {
            writer.WriteStartElement("track");
            WriteMostOfClipItem(writer, clip, "_audio");

            // Write the track duration
            var length = FinalCutDuration(clip);
            var offset = FinalCutOffset(clip.Offset.TotalMilliseconds);
            WriteInOutStartOut(writer, 0, length, offset, 
                length + offset);
            
            writer.WriteStartElement("file");
            writer.WriteAttributeString("id", clip.DisplayName + "_file");
            if (!masterIsVideo)
            {
                writer.WriteElementString("pathurl", "file://" + clip.Filename.Replace("\\", "/"));
            }
            
            writer.WriteEndElement(); // file
            
            WriteSourceTrack(writer, "audio", "1");

            writer.WriteEndElement(); // clipitem
            writer.WriteEndElement(); // track
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

            writer.WriteElementString("duration", (totalDurationSeconds * timebase).ToString());
            
            WriteRate(writer);
            writer.WriteElementString("in", "-1");
            writer.WriteElementString("out", "-1");

            WriteTimecode(writer, "90000");  // TODO: What is this? clip.???
            
            writer.WriteStartElement("media");
        }
        
        private void WriteEndSequence(XmlWriter writer)
        {
            writer.WriteEndElement(); // media
            writer.WriteEndElement(); // sequence
        }


        private void WriteVideoTrack(XmlWriter writer, Clip clip)
        {
            writer.WriteStartElement("track");
            writer.WriteStartElement("clipitem");
            writer.WriteAttributeString("id", clip.DisplayName + "_video");
            writer.WriteElementString("name", clip.DisplayName);

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
            WriteTimecode(writer, "0");

            //------------- media --------------------
            writer.WriteStartElement("media");
            writer.WriteStartElement("video");
            WriteDuration(writer, clip);
            WriteVideoSampleCharacteristics(writer);
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

        private void WriteVideoSampleCharacteristics(XmlWriter writer)
        {
            writer.WriteStartElement("samplecharacteristics");
            writer.WriteElementString("width", "1280");  // Doesn't matter
            writer.WriteElementString("height", "720");
            writer.WriteElementString("anamorphic", "FALSE");
            writer.WriteElementString("pixelaspectratio", "Square");
            writer.WriteElementString("fielddominance", "none");
            WriteRate(writer);
            writer.WriteElementString("colordepth", "24");

            writer.WriteEndElement(); // samplecharacteristics
        }

        private void WriteTimecode(XmlWriter writer, string frame)
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

        private void WriteMostOfClipItem(XmlWriter writer, Clip clip, string idSuffix)
        {
            writer.WriteStartElement("clipitem");
            writer.WriteAttributeString("id", clip.DisplayName + idSuffix);
            writer.WriteElementString("name", clip.DisplayName);
            WriteDuration(writer, clip);
        
            WriteRate(writer);
        }

        private void WriteDuration(XmlWriter writer, Clip clip)
        {
            var duration = FinalCutDuration(clip);
            writer.WriteElementString("duration", duration.ToString());
        }

        private static int FinalCutDuration(Clip clip)
        {
            int duration = clip.DataLength / clip.WaveFormat.SampleRate * timebase;
            return duration;
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