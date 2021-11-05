using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using auxmic.fft;
using auxmic.sync;
using NAudio.Wave;

namespace auxmic
{
    /*
     * Perform fingerprinting with the original auxmic algorithm.
     * It has problems with small files and is quite slow, but works
     * better than "SoundFingerprinter" in some cases
     * (eg: when you are working with a music video)
     */
    public class AuxMicFingerprinter : IFingerprinter
    {
        private SyncParams _syncParams = new SyncParams
        {
            L = 256,
            FreqRangeStep = 60,
            WindowFunction = WindowFunctions.Hamming
        };

        public object CreateFingerPrints(Clip clip)
        {
            return GetHashes(clip);
        }

        /// <summary>
        /// Расчёт хэшей от максимальных магнитуд по окнам для всего звукового файла.
        ///
        /// Calculation hashes from maximum window magnitudes for the entire sound file.
        /// </summary>
        /// <returns></returns>
        private Int32[] GetHashes(Clip clip)
        {
            ISoundFile soundFile = clip.SoundFile;
            string waveFile = soundFile.TempFilename;

            if (FileCache.Contains(GetCachedFilename(clip)))
            {
                clip.ReportProgress(clip.MaxProgressValue);

                return FileCache.Get<Int32[]>(GetCachedFilename(clip));
            }

            int L = _syncParams.L;
            long N = soundFile.DataLength;

            int ranges = (int) Math.Ceiling(((decimal) (L / 2) / _syncParams.FreqRangeStep));

            Int32[] hashes = new Int32[N / L];

            clip.MaxProgressValue = hashes.Length;

            int row = 0;

            // перебираем все данные по т.н. окнам
            // Loop through all the data in the "window"
            using (var reader = new WaveFileReader(waveFile))
            {
                for (int i = 0; i <= N - L; i += L)
                {
                    clip.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // читаем необходимое количество данных из левого канала в "окно"
                    // read a piece of the sound file for processing as a window
                    Complex[] segment = ReadComplex(reader,soundFile.WaveFormat, L);

                    // применяем оконную функцию через делегат
                    // apply a window function via a delegate
                    _syncParams.WindowFunction(segment);

                    // In-place FFT-преобразование этого временного массива
                    // In-place FFT-converting this temporary array
                    Fourier.FFT(segment, Direction.Backward);

                    // считаем powers в этом же цикле
                    // это массив частот с максимальными магнитудами
                    // диапазон частот задаётся шагом rangeStep от 0
                    Int16[] segmentPowers = new Int16[ranges];

                    double[] maxMagnitude = new double[ranges];

                    for (Int16 freq = 0; freq < L / 2; freq++)
                    {
                        double magnitude = Complex.Abs(Complex.Log10(segment[freq]));

                        // определяем в какой диапазон частот с шагом rangeStep попадает текущая частота
                        int index = freq / _syncParams.FreqRangeStep;

                        // если найдена максимальная магнитуда, запоминаем её частоту в powers
                        if (magnitude > maxMagnitude[index])
                        {
                            maxMagnitude[index] = magnitude;
                            segmentPowers[index] = freq;
                        }
                    }

                    // расчитываем хэши по каждому окну на основе power - массива частот с максимальными магнитудами
                    hashes[row] = CombineHashCodes(segmentPowers);

                    row++;

                    clip.ReportProgress(row);
                }
            }

            // add to cache
            FileCache.Set(GetCachedFilename(clip), hashes);

            return hashes;
        }

        private int Min(params int[] args)
        {
            return args.Min();
        }
        
        /// <summary>
        /// Метод полностью повторяет <see cref="internal Int32[] Read(int samplesToRead)"/>, 
        /// за исключением возвращаемого значения, не Int32[], а сразу Complex[].
        /// Различаются только одной строкой: Complex[] result = new Complex[samplesToRead];
        /// Не получилось сделать через generics
        /// </summary>
        /// <param name="samplesToRead"></param>
        /// <returns></returns>
        internal Complex[] ReadComplex(WaveFileReader fileReader, WaveFormat waveFormat, int samplesToRead)
        {
            Complex[] result = new Complex[samplesToRead];

            int blockAlign = waveFormat.BlockAlign;
            int channels = waveFormat.Channels;

            byte[] buffer = new byte[blockAlign * samplesToRead];

            int bytesRead = fileReader.Read(buffer, 0, blockAlign * samplesToRead);

            for (int sample = 0; sample < bytesRead / blockAlign; sample++)
            {
                switch (waveFormat.BitsPerSample)
                {
                    case 8:
                        result[sample] = (Int16) buffer[sample * blockAlign];
                        break;

                    case 16:
                        result[sample] = BitConverter.ToInt16(buffer, sample * blockAlign);
                        break;

                    case 32:
                        result[sample] = BitConverter.ToInt32(buffer, sample * blockAlign);
                        break;

                    default:
                        throw new NotSupportedException(String.Format(
                            "BitDepth '{0}' not supported. Try 8, 16 or 32-bit audio instead.",
                            waveFormat.BitsPerSample));
                }
            }

            return result;
        }


        /// <summary>
        /// Хэширование массива (кастомное)
        /// Generate an integer hash for the provided data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int CombineHashCodes(params Int16[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length == 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (data.Length == 1)
            {
                return data[0];
            }

            int result = data[0];

            for (var i = 1; i < data.Length; i++)
            {
                result = (result << 5) | (result >> 29) ^ data[i];
            }

            return result;
        }

        public ClipMatch matchClips(Clip master, Clip lqClip)
        {
            int[] hq_hashes = (int[]) master.Hashes;
            int[] lq_hashes = (int[]) lqClip.Hashes;

            int maxMatches = 0;

            int start = -lq_hashes.Length + 1;
            int end = hq_hashes.Length;

            int startIndex = start;
            int matchLQ = 0;
            int matchOffset = 0;

            lqClip.SetProgressMax(-start + end);
            int progressCount = 0;

            // for (int hq_idx = start; hq_idx < end; hq_idx++)
            Parallel.For(start, end, hq_idx =>
                {
                    lqClip.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    int intersectionStart = Math.Max(hq_idx, 0);
                    int indexHQ = 0;
                    int indexLQ = -1;
                    int offset  = 0;

                    int intersectionLength = Min(
                        lq_hashes.Length,
                        lq_hashes.Length + hq_idx,
                        hq_hashes.Length - hq_idx,
                        hq_hashes.Length);

                    int matchesCount = 0;

                    for (int intersectionIndexHQ = intersectionStart;
                        intersectionIndexHQ < intersectionLength + intersectionStart;
                        intersectionIndexHQ++)
                    {
                        int intersectionIndexLQ = intersectionIndexHQ - hq_idx;

                        if (hq_hashes[intersectionIndexHQ] == lq_hashes[intersectionIndexLQ])
                        {
                            if (indexLQ == -1)
                            {
                                indexHQ = intersectionIndexHQ;
                                indexLQ = intersectionIndexLQ;
                                offset = hq_idx;
                            }

                            matchesCount++;
                        }
                    }

                    Monitor.Enter(this);
                    if (matchesCount > maxMatches)
                    {
                        maxMatches = matchesCount;
                        startIndex = indexHQ;
                        matchLQ = indexLQ;
                        matchOffset = offset;
                        ;
                    }

                    Monitor.Exit(this);

                    lqClip.ReportProgress(progressCount++);
                }
            );

            if (maxMatches == 0)
            {
                return null;
            }

            double hqPosition = ((double) startIndex * _syncParams.L / lqClip.WaveFormat.SampleRate);
            double lqPosition = ((double) matchLQ * _syncParams.L / lqClip.WaveFormat.SampleRate);
            double offsetPosition = ((double) matchOffset * _syncParams.L / lqClip.WaveFormat.SampleRate);

            Debug.WriteLine("Seek positions are HQ " + hqPosition + " and LQ " + lqPosition);

            return new ClipMatch(lqPosition, hqPosition, offsetPosition);
        }

        /// <summary>
        /// Возвращает имя уже посчитанных хэшей для файла.
        /// Оказалось, что важно различать временные файлы с посчитанными хэшами по SampleRate,
        /// без различения по SampleRate возможна ситуация, когда загрузится кэш хэшей, посчитанный по 
        /// другому SampleRate. Такое возможно, если обработать два файла с различными SampleRate, 
        /// а затем поменять их местами (мастером сделать другой).
        /// Если у клипа не задан _resampleFormat, то это мастер-запись и берём SampleRate из SoundFile.
        /// </summary>
        /// <returns></returns>
        private string GetCachedFilename(Clip clip)
        {
            return FileCache.GetTempFilename(
                clip.Filename,
                clip.WaveFormat.SampleRate);
        }


        public void Cleanup(Clip clip)
        {
            // Remove hashed data
            FileCache.Remove(GetCachedFilename(clip));
        }
    }
}