/*
Copyright (c) 2015, Joao Nuno Carvalho

All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the author nor the names of any contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Text;

namespace auxmic.wave
{
    // Heavily edited version of BSD Style licensed code originally found here:
    // https://github.com/joaocarvalhoopen/WAV_Tools_C_Sharp/blob/master/WAV_file.cs
    public class WAV_file
    {
        public enum BITS_PER_SAMPLE
        {
            NOT_DEFINED = 0,
            BPS_8_BITS = 8,
            BPS_16_BITS = 16
        }

        public enum NUM_CHANNELS
        {
            NOT_DEFINED = 0,
            ONE = 1,
            TWO = 2
        }

        // WAV object fields.
        private string _file_name;

        private ushort audio_format;
        private ushort block_align;
        private ushort bps; //Bits per sample 
        // private short[] bufferInternal_int16;

        private byte[] bufferInternal_uint8;
        private uint byte_rate;

        // WAV file header fields.
        private byte[] chunk_id = new byte[4]; // This are char[] in the C sense, one byte each.
        private uint chunk_size;
        private byte[] datachunk_id = new byte[4]; // This are char[] in the C sense, one byte each.
        private uint datachunk_size;
        private byte[] fmtchunk_id = new byte[4]; //    "
        private uint fmtchunk_size;
        private byte[] format = new byte[4]; //    "
        private ushort num_channels;

        public string File_name
        {
            get => _file_name;
            set
            {
                if (!value.ToUpper().EndsWith("WAV"))
                    throw new ArgumentException("WAV file name must end with 'wav', or 'WAV' extension!");
                _file_name = value;
            }
        }

        // Properties.
        public NUM_CHANNELS NumOfChannels
        {
            get => (NUM_CHANNELS) num_channels;
            set => num_channels = (ushort) value;
        }

        public uint SampleRate { get; set; }

        public BITS_PER_SAMPLE BitsPerSample
        {
            get => (BITS_PER_SAMPLE) bps;
            set => bps = (ushort) value;
        }

        public void loadFile(Stream inHere)
        {
            var reader = new BinaryReader(inHere);
            {
                // Read WAV file header fields.
                chunk_id = reader.ReadBytes(4); // Byte[]
                chunk_size = reader.ReadUInt32();
                format = reader.ReadBytes(4); // Byte[]
                fmtchunk_id = reader.ReadBytes(4); // Byte[]
                fmtchunk_size = reader.ReadUInt32();
                audio_format = reader.ReadUInt16();
                num_channels = reader.ReadUInt16();
                SampleRate = reader.ReadUInt32();
                byte_rate = reader.ReadUInt32();
                block_align = reader.ReadUInt16();
                bps = reader.ReadUInt16(); //Bits per sample 
                datachunk_id = reader.ReadBytes(4); // Byte[]
                datachunk_size = reader.ReadUInt32();

                // File type validations.
                if (Encoding.ASCII.GetString(chunk_id) != "RIFF"
                    || Encoding.ASCII.GetString(format) != "WAVE")
                    throw new ApplicationException("ERROR: Source is not a WAV file");
                if (audio_format != 1) throw new ApplicationException("ERROR: API only supports PCM format in WAV.");

                switch ((BITS_PER_SAMPLE) bps)
                {
                    case BITS_PER_SAMPLE.BPS_8_BITS:
                        bufferInternal_uint8 = reader.ReadBytes((int) datachunk_size);
                        break;

                    case BITS_PER_SAMPLE.BPS_16_BITS:
                        reader.ReadBytes((short) datachunk_size); // SO I JUST ADDED THIS INSTEAD.
                        break;

                    default:
                        throw new ApplicationException("ERROR: Incorrect bits per sample in source");
                }
            }
        }
    } 
}