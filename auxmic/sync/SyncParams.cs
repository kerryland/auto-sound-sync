using System.Numerics;

namespace auxmic
{
    public delegate void WindowFunction(Complex[] data);

    public sealed class SyncParams
    {
        /// <summary>
        /// длина т.н. скользящего окна - отрезка на которые разбиваются данные и по которым считаются FFT
        /// Значение - степень 2.
        /// Для более точного распознавания использовать меньшее значение, при этом требуется больше расчётов.
        /// 128 - для коротких файлов
        /// 256 - для средних
        /// 4096 - для продолжительных записей
        ///
        /// How many bytes are hashed in the sliding window - a segment into which the data is divided and by which the FFT is calculated.
        ///
        /// Each bucket in the hash array represents this many bytes.
        /// 
        /// The value is a power of 2.
        /// Use a lower value for more accuracy. Lower values require more calculations.
        /// 128 - for short files
        /// 256 - for medium files
        /// 4096 - for long recordings
        /// </summary>
        public ushort L { get; set; }

        /// <summary>
        /// оконная функция, scipy.hamming
        /// </summary>
        public WindowFunction WindowFunction { get; set; }

        /// <summary>
        /// Шаг частот для отбора частот из спектра с максимальными магнитудами.
        /// Диапазон частот формируется от 0 до L/2 с шагом FreqRangeStep.
        /// При L = 256 и FreqRangeStep = 60 получим 3 интервала: от 0 до 59, от 60 до 119, от 120 до 128 (половина L)
        /// т.е. от 0 с шагом 60 до половины L=256
        /// 
        /// Рекомендуемые значения: 40, 60 (стоит иметь в ввиду, что при разных значениях L будет эффективно своё значение FreqRangeStep)
        /// Рекомендуется иметь 3-4 диапазона частот. Для этого для L=256 => FreqRangeStep целесообразней взять как 40, это даст
        /// следующие диапазоны: 0 - 39, 40 - 79, 80 - 119, 120 - 128 (128 - это половина L, т.к. оставшаяся часть - зеркальное отображение)
        ///
        /// Frequency step for selecting frequencies from the spectrum with maximum magnitudes.
        /// The frequency range is formed from 0 to L / 2 with a FreqRangeStep step.
        /// With L = 256 and FreqRangeStep = 60, we get 3 intervals: from 0 to 59, from 60 to 119, from 120 to 128 (half L)
        /// i.e. from 0 in increments of 60 to half L = 256
        ///
        /// Recommended values: 40, 60 (it should be borne in mind that for different values of L, its own FreqRangeStep value will be effective)
        /// It is recommended to have 3-4 frequency ranges. For this, for L = 256 => FreqRangeStep, it is more expedient to take as 40, this will give
        /// the following ranges: 0 - 39, 40 - 79, 80 - 119, 120 - 128 (128 is half of L, since the rest is a mirror image)
        /// </summary>
        public byte FreqRangeStep { get; set; }
    }
}
