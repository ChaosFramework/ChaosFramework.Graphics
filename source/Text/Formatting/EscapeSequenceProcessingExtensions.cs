using ChaosFramework.Collections;
using ChaosUtil.Primitives;
using System;

namespace ChaosFramework.Graphics.Text.Formatting
{
    using Colors;

    static class EscapeSequenceProcessingExtensions
    {
        internal static void ProcessEscapeSequence(
            this TextGeometry @this,
            UnicodeChars[] sequence,
            int lineIndex,
            int totalCharIndex,
            LinkedList<Rgba> colorStack,
            LinkedList<float> sizeStack
            )
        {
            if (sequence.Length < 3)
                throw new ArgumentException($"Escape character at end of line {lineIndex} exceeds length of line.", nameof(sequence));

            switch (sequence[1])
            {
                case (UnicodeChars)ColoredTextScope.ESCAPE_CODE_COL:
                    @this.ProcessColorSequence(sequence, lineIndex, totalCharIndex, colorStack);
                    break;

                case (UnicodeChars)SizedTextScope.ESCAPE_CODE_SZ:
                    @this.ProcessSizeSequence(sequence, lineIndex, totalCharIndex, sizeStack);
                    break;

                default:
                    throw new ArgumentException("Invalid escape sequence!", nameof(sequence));
            }
        }

        static void ProcessColorSequence(
            this TextGeometry @this,
            UnicodeChars[] sequence,
            int lineIndex,
            int totalCharIndex,
            LinkedList<Rgba> colorStack
            )
        {
            if (sequence[2] == (UnicodeChars)Scope.ESCAPE_CODE)
            {
                if (colorStack.empty)
                    @this.colorCodes[totalCharIndex] = Rgba.OPAQUE_WHITE;
                else
                {
                    colorStack.RemoveAt(0);
                    @this.colorCodes[totalCharIndex] = colorStack.empty ? Rgba.OPAQUE_WHITE : colorStack.first;
                }
            }
            else
            {
                Rgba color;
                if (!ColoredTextScope.TryParseColor(sequence, 2, out color))
                    throw new ArgumentException($"Invalid color code: {sequence.Substring(2, sequence.Length - 3)}", nameof(sequence));

                @this.colorCodes[totalCharIndex] = color;
                colorStack.Insert(0, color);
            }
        }

        static void ProcessSizeSequence(
            this TextGeometry @this,
            UnicodeChars[] sequence,
            int lineIndex,
            int totalCharIndex,
            LinkedList<float> sizeStack
            )
        {
            if (sequence.Length == 3)
            {
                if (sizeStack.empty)
                    @this.sizeCodes[totalCharIndex] = 1;
                else
                {
                    sizeStack.RemoveAt(0);
                    @this.sizeCodes[totalCharIndex] = sizeStack.empty ? 1 : sizeStack.first;
                }
            }
            else
            {
                float size;
                string sizeCode = sequence.Substring(2, sequence.Length - 3);
                if (!float.TryParse(sequence.Substring(2, sequence.Length - 3), out size))
                    throw new ArgumentException($"Invalid size code: {sizeCode}", nameof(sizeCode));

                @this.sizeCodes[totalCharIndex] = size;
                sizeStack.Insert(0, size);
            }
        }
    }
}
