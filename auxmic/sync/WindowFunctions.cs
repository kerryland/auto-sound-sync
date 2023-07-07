using System;
using System.Numerics;

namespace auxmic
{
    public sealed class WindowFunctions
    {
        private static readonly float HAMMING_ALPHA = 0.54f;
        private static readonly float HAMMING_BETA = 0.46f;

        /// <summary>
        /// http://en.wikipedia.org/wiki/Window_function#Window_examples
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void Hamming(Complex[] data)
        {
            int N = data.Length;

            for (int n = 0; n < N; n++)
                data[n] = new Complex(data[n].Real * (HAMMING_ALPHA - HAMMING_BETA * Math.Cos((2 * Math.PI * n) / (N - 1))), 0);
        }
    }
}
