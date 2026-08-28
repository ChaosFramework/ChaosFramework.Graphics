using ChaosFramework.Shapes;
using ChaosFramework.Collections;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using static ChaosFramework.Math.Clamping;
using ArgumentException = System.ArgumentException;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.Text
{
    using Colors;
    using Formatting;

    public partial class TextGeometry
    {
        // TODO: Avoid exposing these as mutable members
        public readonly int numPrintedChars = 0;

        /// <summary>Minimal bounds that fully contain all printed characters.</summary>
        public readonly Bounds2f geometryBounds;

        /// <summary>
        ///     Minimal bounds that fully contain all printed characters,
        ///     including leading and trailing blank space, both horizontally and vertically.
        /// </summary>
        public readonly Bounds2f textBounds;

        public readonly UnicodeChars[][] lines;
        public readonly float[] lineWidths;
        public readonly Vector2f[] charPositions;
        public readonly int[] lineStartIndices;
        public readonly TextGeometryDescription args;
        public readonly MeshData meshData;

        internal readonly SysCol.Dictionary<int, Rgba> colorCodes = new SysCol.Dictionary<int, Rgba>();
        internal readonly SysCol.Dictionary<int, float> sizeCodes = new SysCol.Dictionary<int, float>();

        public TextVertex[] vertices;

        public Align verticalAlign => align & (Align.Bottom | Align.Top);
        public Align horizontalAlign => align & (Align.Left | Align.Right);
        public string text => args.text;
        public bool enableEscapeCodes => args.layout.enableEscapeSequences;
        public bool monoSpace => args.layout.monospace;
        public bool ignoreControlCharacters => args.layout.ignoreControlCharacters;
        public Vector2f letterDistance => args.layout.letterDistance;
        public Align align => args.layout.align;

        public TextGeometry(TextGeometryDescription args)
        {
            LinkedList<Rgba> colorStack = new LinkedList<Rgba>();
            LinkedList<float> sizeStack = new LinkedList<float>();

            this.args = args;

            if (ignoreControlCharacters)
                lines = new UnicodeChars[][] { text.GetUnicodeChars() };
            else
            {
                string[] splitIntoLines = text.Split('\n');
                lines = new UnicodeChars[splitIntoLines.Length][];
                for (int i = 0; i < splitIntoLines.Length; i++)
                    lines[i] = splitIntoLines[i].GetUnicodeChars();
            }

            int totalCharCounter = 0;
            if (enableEscapeCodes)
            {
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    for (int characterIndex = 0; characterIndex < lines[lineIndex].Length; characterIndex++)
                    {
                        if (lines[lineIndex][characterIndex] == (UnicodeChars)Formatting.Scope.ESCAPE_CODE)
                        {
                            if (characterIndex + 2 >= lines[lineIndex].Length)
                                throw new ArgumentException($"Escape character at end of line {lineIndex} exceeds length of line.", nameof(args));

                            UnicodeChars[] sequence = Scope.GetEscapeSequence(lines[lineIndex], characterIndex);
                            this.ProcessEscapeSequence(sequence, lineIndex, totalCharCounter, colorStack, sizeStack);
                            lines[lineIndex] = lines[lineIndex].Remove(characterIndex, sequence.Length);
                            characterIndex--;
                        }
                        else
                            totalCharCounter++;
                    }
                    totalCharCounter++;
                }
            }
            else
                foreach (UnicodeChars[] line in lines)
                    totalCharCounter += line.Length + 1;

        buildGeometry:
            geometryBounds = new Bounds2f();
            textBounds = new Bounds2f(0, 0);
            charPositions = new Vector2f[totalCharCounter];

            lineWidths = new float[lines.Length];
            int currentLine = 0;
            int tmpCharCounter = 0;
            foreach (UnicodeChars[] line in lines)
            {
                int alignCenterTabCounter = 0;
                float charWidth = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    UnicodeChars c = line[i];

                    if (!ignoreControlCharacters && c == (UnicodeChars)'\t')
                        lineWidths[currentLine] += args.layout.tabStops[Min(alignCenterTabCounter++, args.layout.tabStops.length - 1)];
                    else if (!ignoreControlCharacters && c == (UnicodeChars)'\b')
                        lineWidths[currentLine] -= (charWidth * letterDistance.x);
                    else if (!ignoreControlCharacters && c == (UnicodeChars)'\r')
                    {
                        System.Array.Resize(ref lineWidths, lineWidths.Length + 1);
                        currentLine++;
                    }
                    else
                    {
                        float charScale = GetEscapeData(tmpCharCounter, sizeCodes, 1);
                        charWidth = monoSpace ? 1 : (args.font.GetGlyph(c).advanceCursor.x * charScale);
                        lineWidths[currentLine] += charWidth * letterDistance.x;
                        numPrintedChars++;
                    }
                    tmpCharCounter++;
                }
                tmpCharCounter++;
                currentLine++;
            }
            lineStartIndices = new int[currentLine];
            currentLine = 0;
            int letterPosY = 0;

            int numVerts = numPrintedChars * 4;
            Vector3f[] pos = new Vector3f[numVerts];
            Vector4f[] tex = new Vector4f[numVerts];
            Rgba[] col = new Rgba[numVerts];
            uint[] ind = new uint[numPrintedChars * 6];
            uint currentIndex = 0;
            for (int i = 0; i < ind.Length; i += 6)
            {
                ind[i] = currentIndex;
                ind[i + 1] = ind[i + 4] = currentIndex + 1;
                ind[i + 2] = ind[i + 3] = currentIndex + 2;
                ind[i + 5] = currentIndex + 3;
                currentIndex += 4;
            }

            vertices = new TextVertex[numVerts];

            int charIndex = 0;
            int totalLineOffset = 0;
            int virtualLineIndex = 0;
            Vector2f endOfLastChar = new Vector2f();
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                UnicodeChars[] line = lines[lineIndex];
                lineStartIndices[virtualLineIndex] = totalLineOffset;

                Vector2f charPos = Vector2f.EMPTY;
                if (verticalAlign == Align.Top)
                    charPos.y = (-letterPosY - 0.5f) * letterDistance.y;
                else if (verticalAlign == Align.Bottom)
                    charPos.y = (lines.Length - letterPosY - 0.5f) * letterDistance.y + (1 - letterDistance.y);
                else
                    charPos.y = (-letterPosY + lines.Length / 2f) * letterDistance.y - letterDistance.y * 0.5f;

                textBounds.Expand(new Vector2f(textBounds.center.x, charPos.y - 0.5f));
                textBounds.Expand(new Vector2f(textBounds.center.x, charPos.y + 0.5f));

                int alignCenterTabCounter = 0;
                LinkedList<float> backspaceStack = new LinkedList<float>();

                float letterPosX = 0;
                float maxLetterPosX = 0;
                float minLetterPosX = 0;
                void setLetterPosX(float value)
                {
                    letterPosX = value;
                    minLetterPosX = Min(minLetterPosX, value);
                    maxLetterPosX = Max(maxLetterPosX, value);
                }
                void addLetterPosX(float delta)
                    => setLetterPosX(letterPosX + delta);

                bool rtl = horizontalAlign == Align.Right;
                for (int i = rtl ? line.Length - 1 : 0; rtl ? (i >= 0) : i < line.Length; i = rtl ? (i - 1) : (i + 1))
                {
                    UnicodeChars c = line[i];
                    GlyphDimensions charDesc = args.font.GetGlyph(c);

                    if (!ignoreControlCharacters && c == (UnicodeChars)'\r')
                    {
                        virtualLineIndex++;
                        lineStartIndices[virtualLineIndex] = totalCharCounter + 1;
                        charPositions[totalLineOffset + i] = new Vector2f(letterPosX, charPos.y);
                        setLetterPosX(0);
                        continue;
                    }
                    else if (!ignoreControlCharacters && c == (UnicodeChars)'\t')
                    {
                        if (horizontalAlign == Align.Center)
                        {
                            addLetterPosX(args.layout.tabStops[alignCenterTabCounter]);
                            textBounds.Expand(new Vector2f(letterPosX / 2, textBounds.center.y));
                            textBounds.Expand(new Vector2f(-letterPosX / 2, textBounds.center.y));
                        }
                        else
                        {
                            int nextTabstopIndex = 0;
                            float tabStopPosition = 0;
                            while (rtl
                                 ? letterPosX <= (tabStopPosition -= args.layout.tabStops[Min(nextTabstopIndex++, args.layout.tabStops.length - 1)])
                                 : letterPosX >= (tabStopPosition += args.layout.tabStops[Min(nextTabstopIndex++, args.layout.tabStops.length - 1)])
                                )
                                continue;

                            charPositions[totalLineOffset + i] = new Vector2f(letterPosX, charPos.y);
                            setLetterPosX(tabStopPosition);
                            endOfLastChar = new Vector2f(letterPosX, charPositions[totalLineOffset + i].y);
                            textBounds.Expand(new Vector2f(endOfLastChar.x, textBounds.center.y));
                        }
                    }
                    else if (!ignoreControlCharacters && c == (UnicodeChars)'\b')
                        addLetterPosX(-(backspaceStack.empty ? 0 : (backspaceStack.RemoveAt(backspaceStack.length - 1) * letterDistance.x)));
                    else
                    {
                        float charScale = GetEscapeData(charIndex + lineIndex, sizeCodes, 1);
                        float cursorAdvanceX = (monoSpace ? 1 : (charScale * charDesc.advanceCursor.x)) * letterDistance.x;
                        if (rtl)
                            addLetterPosX(-cursorAdvanceX);
                        if (horizontalAlign == Align.Center)
                            charPos.x = letterPosX - lineWidths[virtualLineIndex] / 2f;
                        else
                            charPos.x = letterPosX;

                        charPositions[totalLineOffset + i] = charPos;
                        endOfLastChar = charPos + new Vector2f(cursorAdvanceX, 0);
                        backspaceStack.Add(cursorAdvanceX);

                        Bounds2f texCoordsSDF = new Bounds2f(
                                                        charDesc.sdfCoords.low.x / (float)args.dimensions.sdfBounds.x,
                                                        charDesc.sdfCoords.low.y / (float)args.dimensions.sdfBounds.y,
                                                        charDesc.sdfCoords.high.x / (float)args.dimensions.sdfBounds.x,
                                                        charDesc.sdfCoords.high.y / (float)args.dimensions.sdfBounds.y);
                        Bounds2f texCoordsCOL = new Bounds2f(
                                                        charDesc.colCoords.low.x / (float)args.dimensions.colBounds.x,
                                                        charDesc.colCoords.low.y / (float)args.dimensions.colBounds.y,
                                                        charDesc.colCoords.high.x / (float)args.dimensions.colBounds.x,
                                                        charDesc.colCoords.high.y / (float)args.dimensions.colBounds.y);

                        float scaleX = charScale * charDesc.sdfCoords.width / (charDesc.sdfCoords.width - 2 * args.dimensions.sdfRadius);
                        float scaleY = charScale * charDesc.sdfCoords.height / (charDesc.sdfCoords.height - 2 * args.dimensions.sdfRadius);

                        charPos.x += cursorAdvanceX / 2;

                        int vertexIndex = charIndex * 4;

                        col[vertexIndex] = col[vertexIndex + 1] = col[vertexIndex + 2] = col[vertexIndex + 3] = GetEscapeData(totalLineOffset + i, colorCodes, Rgba.OPAQUE_WHITE);

                        pos[vertexIndex] = new Vector3f(charPos.x + charDesc.charBounds.left * scaleX, charPos.y - charDesc.charBounds.bottom * scaleY, charScale);
                        pos[vertexIndex + 1] = new Vector3f(charPos.x + charDesc.charBounds.right * scaleX, charPos.y - charDesc.charBounds.bottom * scaleY, charScale);
                        pos[vertexIndex + 2] = new Vector3f(charPos.x + charDesc.charBounds.left * scaleX, charPos.y - charDesc.charBounds.top * scaleY, charScale);
                        pos[vertexIndex + 3] = new Vector3f(charPos.x + charDesc.charBounds.right * scaleX, charPos.y - charDesc.charBounds.top * scaleY, charScale);

                        tex[vertexIndex] = new Vector4f(texCoordsSDF.left, texCoordsSDF.bottom, texCoordsCOL.left, texCoordsCOL.bottom);
                        tex[vertexIndex + 1] = new Vector4f(texCoordsSDF.right, texCoordsSDF.bottom, texCoordsCOL.right, texCoordsCOL.bottom);
                        tex[vertexIndex + 2] = new Vector4f(texCoordsSDF.left, texCoordsSDF.top, texCoordsCOL.left, texCoordsCOL.top);
                        tex[vertexIndex + 3] = new Vector4f(texCoordsSDF.right, texCoordsSDF.top, texCoordsCOL.right, texCoordsCOL.top);

                        Rgba charColor = GetEscapeData(totalLineOffset + i, colorCodes, Rgba.OPAQUE_WHITE);
                        vertices[vertexIndex] = new TextVertex
                        {
                            position = new Vector4f(charPos.x + charDesc.charBounds.left * scaleX, charPos.y - charDesc.charBounds.bottom * scaleY, charScale, 0),
                            texCoord = new Vector4f(texCoordsSDF.left, texCoordsSDF.bottom, texCoordsCOL.left, texCoordsCOL.bottom),
                            color = charColor
                        };
                        vertices[vertexIndex + 1] = new TextVertex
                        {
                            position = new Vector4f(charPos.x + charDesc.charBounds.right * scaleX, charPos.y - charDesc.charBounds.bottom * scaleY, charScale, 0),
                            texCoord = new Vector4f(texCoordsSDF.right, texCoordsSDF.bottom, texCoordsCOL.right, texCoordsCOL.bottom),
                            color = charColor
                        };
                        vertices[vertexIndex + 2] = new TextVertex
                        {
                            position = new Vector4f(charPos.x + charDesc.charBounds.left * scaleX, charPos.y - charDesc.charBounds.top * scaleY, charScale, 0),
                            texCoord = new Vector4f(texCoordsSDF.left, texCoordsSDF.top, texCoordsCOL.left, texCoordsCOL.top),
                            color = charColor
                        };
                        vertices[vertexIndex + 3] = new TextVertex
                        {
                            position = new Vector4f(charPos.x + charDesc.charBounds.right * scaleX, charPos.y - charDesc.charBounds.top * scaleY, charScale, 0),
                            texCoord = new Vector4f(texCoordsSDF.right, texCoordsSDF.top, texCoordsCOL.right, texCoordsCOL.top),
                            color = charColor
                        };

                        if (!rtl)
                            addLetterPosX(cursorAdvanceX);

                        bool blank = charDesc.charBounds.area <= 0;

                        if (blank)
                        {
                            // expand by one unit char
                            textBounds.Expand(new Vector2f(charPos.x - 0.5f, charPos.y - 0.5f));
                            textBounds.Expand(new Vector2f(charPos.x + 0.5f, charPos.y + 0.5f));
                        }
                        else
                        {
                            geometryBounds.low.x = Min(geometryBounds.low.x, charPos.x + charDesc.charBounds.low.x);
                            geometryBounds.high.x = Max(geometryBounds.high.x, charPos.x + charDesc.charBounds.high.x);
                            geometryBounds.low.y = Min(geometryBounds.low.y, charPos.y + charDesc.charBounds.low.y);
                            geometryBounds.high.y = Max(geometryBounds.high.y, charPos.y + charDesc.charBounds.high.y);
                            textBounds.Expand(geometryBounds);
                        }

                        charIndex++;

                        if ((maxLetterPosX - minLetterPosX) > args.layout.maxLineWidth)
                            for (int charIndexInLine = i; charIndexInLine >= 0; charIndexInLine--)
                                if (TextGeometryDescription.POSSIBLE_LINE_BREAKS.IndexOf(line[charIndexInLine]) >= 0)
                                {
                                    int lineStartLength = 0;
                                    LinkedList<UnicodeChars> newLine = new LinkedList<UnicodeChars>();
                                    foreach (UnicodeChars lineStartChar in line)
                                        if (TextGeometryDescription.LINE_INDENTATION_CHARS.IndexOf(lineStartChar) > 0)
                                        {
                                            newLine.Add(lineStartChar);
                                            lineStartLength++;
                                        }
                                        else
                                            break;

                                    totalCharCounter += lineStartLength;

                                    int autoLineBreakPosition = charIndexInLine + totalLineOffset - lineIndex;

                                    SysCol.Dictionary<int, Rgba> tmpColorCodes = colorCodes;
                                    colorCodes = new SysCol.Dictionary<int, Rgba>();
                                    foreach (SysCol.KeyValuePair<int, Rgba> colorCode in tmpColorCodes)
                                        if (colorCode.Key >= autoLineBreakPosition)
                                            colorCodes[colorCode.Key + lineStartLength] = colorCode.Value;
                                        else
                                            colorCodes[colorCode.Key] = colorCode.Value;

                                    SysCol.Dictionary<int, float> tmpSizeCodes = sizeCodes;
                                    sizeCodes = new SysCol.Dictionary<int, float>();
                                    foreach (SysCol.KeyValuePair<int, float> sizeCode in tmpSizeCodes)
                                        if (sizeCode.Key >= autoLineBreakPosition)
                                            sizeCodes[sizeCode.Key + lineStartLength] = sizeCode.Value;
                                        else
                                            sizeCodes[sizeCode.Key] = sizeCode.Value;

                                    for (int restOfLine = charIndexInLine + 1; restOfLine < line.Length; restOfLine++)
                                        newLine.Add(line[restOfLine]);

                                    UnicodeChars[][] linesCopy = lines;
                                    lines = new UnicodeChars[linesCopy.Length + 1][];
                                    for (int x = 0; x < lineIndex; x++)
                                        lines[x] = linesCopy[x];
                                    lines[lineIndex] = line.Substring(0, charIndexInLine).GetUnicodeChars();
                                    lines[lineIndex + 1] = newLine.ToArray();

                                    for (int x = lineIndex + 2; x < lines.Length; x++)
                                        lines[x] = linesCopy[x - 1];

                                    goto buildGeometry;
                                }
                    }
                }
                if (currentLine < lines.Length)
                    charPositions[totalLineOffset + line.Length] = new Vector2f(
                        (lines[currentLine].Length == 0 || align.Horizontal() == Align.Right) ? 0 : endOfLastChar.x,
                        charPos.y
                        );

                currentLine++;
                letterPosY++;
                totalLineOffset += line.Length + 1;
                virtualLineIndex++;
            }

            MeshData.CustomStream[] customData = new MeshData.CustomStream[] {
                new MeshData.CustomStreamDataArray<Rgba>(col, "COLOR", "COLOR0"),
                new MeshData.CustomStreamDataArray<Vector4f>(tex, "TEXCOORD", "TEXCOORD0")
            };

            meshData = new MeshData(0, pos, null, null, null, ind, customData);
        }

        public T GetEscapeData<T>(int charIndex, SysCol.Dictionary<int, T> data, T defaultValue)
        {
            if (!enableEscapeCodes)
                return defaultValue;

            T value = defaultValue;
            foreach (var entry in data)
                if (charIndex < entry.Key)
                    return value;
                else
                    value = entry.Value;

            return value;
        }

        public Vector2f GetCursorPosFromIndex(int cursor)
            => charPositions.Length == 0
             ? Vector2f.EMPTY
             : charPositions[Min(cursor, charPositions.Length - 1)];

        public int GetCursorIndexFromPos(Vector2f cursor)
        {
            float unitHeight = lines.Length * args.layout.letterDistance.y;

            int y = 0;
            switch (verticalAlign)
            {
                case Align.Center: y = (int)System.Math.Round(unitHeight / 2 - cursor.y - 0.5f); break;
                case Align.Bottom: y = (int)System.Math.Round(unitHeight - cursor.y - 0.5f); break;
                case Align.Top: y = (int)System.Math.Round(-cursor.y - 0.5f); break;
            }
            if (y < 0)
                return 0;

            if (y >= lines.Length)
                return charPositions.Length;

            int lineStart = lineStartIndices[y];
            int lineEnd = (y == lines.Length - 1 ? charPositions.Length : lineStartIndices[y + 1]) - 1;

            if (cursor.x < charPositions[lineStart].x)
                return lineStart;

            while (lineStart != lineEnd)
            {
                int current = (lineStart + lineEnd) / 2;
                if (current == lineStart || current == lineEnd)
                    return lineEnd;

                if (cursor.x > (charPositions[current].x + charPositions[current + 1].x) / 2)
                    lineStart = current;
                else
                    lineEnd = current;
            }

            return lineEnd;
        }

        public override bool Equals(object obj)
            => Equals(obj as TextGeometry);

        public bool Equals(TextGeometry text)
            => !ReferenceEquals(null, text) && text.args == args;

        public override int GetHashCode()
            => text.GetHashCode();
    }
}
