using ChaosFramework.Core;
using ChaosUtil.Primitives;
using ArgumentException = System.ArgumentException;

namespace ChaosFramework.Graphics.Text.Formatting
{
    public abstract class Scope : Disposable
    {
        /// <summary>
        ///     Character used to indicate the start of a control sequence.
        ///     (Here the ESC character is used.)
        /// </summary>
        public const char ESCAPE_CODE = (char)27;

        /// <summary> Returns the length of an escape string excluding the start character. </summary>
        /// <param name="text"> The string to be analyzed. </param>
        /// <param name="start"> The index of the start character. (Must be <see cref="ESCAPE_CODE"/>.) </param>
        /// <returns> The length of an escape string excluding the start character. </returns>
        public static int GetEscapeSequenceLength(string text, int start)
        {
            if (text[start] != ESCAPE_CODE)
                throw new ArgumentException("The character at the given index is not an escape code.");

            int end = -1;
            for (int i = start + 1; i < text.Length; i++)
                if (text[i] == ESCAPE_CODE)
                {
                    end = i;
                    break;
                }

            if (end == -1)
                throw new ArgumentException("Invalid escape code.");

            return end - start;
        }

        /// <summary> Returns the length of an escape string excluding the start character. </summary>
        /// <param name="text"> The string to be analyzed. </param>
        /// <param name="start"> The index of the start character. (Must be <see cref="ESCAPE_CODE"/>.) </param>
        /// <returns> The length of an escape string excluding the start character. </returns>
        public static int GetEscapeSequenceLength(UnicodeChars[] text, int start)
        {
            const UnicodeChars escape = (UnicodeChars)ESCAPE_CODE;
            if (text[start] != escape)
                throw new ArgumentException("The character at the given index is not an escape code.");

            int end = -1;
            for (int i = start + 1; i < text.Length; i++)
                if (text[i] == escape)
                {
                    end = i;
                    break;
                }

            if (end == -1)
                throw new ArgumentException("Invalid escape code.");

            return end - start;
        }

        /// <summary> Returns the escape sequence starting at a given character. </summary>
        /// <param name="text"> The text to be analyzed. </param>
        /// <param name="start"> The index of the start character. (Must be <see cref="ESCAPE_CODE"/>.) </param>
        /// <returns> The escape sequence beginning and ending with an escape character. </returns>
        public static string GetEscapeSequence(string text, int start)
            => text.Substring(start, 1 + GetEscapeSequenceLength(text, start));

        /// <summary> Returns the escape sequence starting at a given character. </summary>
        /// <param name="text"> The text to be analyzed. </param>
        /// <param name="start"> The index of the start character. (Must be <see cref="ESCAPE_CODE"/>.) </param>
        /// <returns> The escape sequence beginning and ending with an escape character. </returns>
        public static UnicodeChars[] GetEscapeSequence(UnicodeChars[] text, int start)
            => text.SubArray(start, 1 + GetEscapeSequenceLength(text, start));

        public static string Unescape(string text)
        {
            System.Text.StringBuilder bldr = new System.Text.StringBuilder();
            bool escaped = false;
            foreach (char c in text)
            {
                if (c == ESCAPE_CODE)
                    escaped = !escaped;
                if (!escaped)
                    bldr.Append(c);
            }
            return bldr.ToString();
        }

        public readonly System.Text.StringBuilder bldr;
        public Scope(System.Text.StringBuilder bldr) { this.bldr = bldr; }

        protected override sealed void DoDispose()
        {
            base.DoDispose();
            Reset();
        }

        protected abstract void Reset();
    }
}
