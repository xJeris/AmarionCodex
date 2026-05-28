/*
 * MiniJSON — Minimal JSON parser/serializer for C#/.NET
 * Originally by Calvin Rien (https://gist.github.com/darktable/1411710)
 * Public domain / MIT license
 *
 * Used here because Unity's JsonUtility cannot reliably deserialize
 * nested List<T> of [Serializable] classes in all Mono runtimes.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AmarionCodex.Data
{
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        public static Dictionary<string, object> DeserializeObject(string json)
        {
            return Deserialize(json) as Dictionary<string, object>;
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            StringReader reader;

            Parser(string jsonString)
            {
                reader = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                reader.Dispose();
            }

            char PeekChar
            {
                get
                {
                    return Convert.ToChar(reader.Peek());
                }
            }

            char NextChar
            {
                get
                {
                    return Convert.ToChar(reader.Read());
                }
            }

            string NextWord
            {
                get
                {
                    StringBuilder word = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);
                        if (reader.Peek() == -1) break;
                    }
                    return word.ToString();
                }
            }

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            bool IsWordBreak(char c)
            {
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (reader.Peek() == -1) return TOKEN.NONE;

                    switch (PeekChar)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': reader.Read(); return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARED_OPEN;
                        case ']': reader.Read(); return TOKEN.SQUARED_CLOSE;
                        case ',': reader.Read(); return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '0': case '1': case '2': case '3': case '4':
                        case '5': case '6': case '7': case '8': case '9':
                        case '-': return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "false": return TOKEN.FALSE;
                        case "true": return TOKEN.TRUE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            void EatWhitespace()
            {
                while (reader.Peek() != -1 && Char.IsWhiteSpace(PeekChar))
                    reader.Read();
            }

            object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING: return ParseString();
                    case TOKEN.NUMBER: return ParseNumber();
                    case TOKEN.CURLY_OPEN: return ParseObject();
                    case TOKEN.SQUARED_OPEN: return ParseArray();
                    case TOKEN.TRUE: return true;
                    case TOKEN.FALSE: return false;
                    case TOKEN.NULL: return null;
                    default: return null;
                }
            }

            Dictionary<string, object> ParseObject()
            {
                reader.Read(); // {
                var table = new Dictionary<string, object>();

                while (true)
                {
                    TOKEN nextToken = NextToken;
                    switch (nextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.COMMA: continue;
                        case TOKEN.CURLY_CLOSE: return table;
                        default:
                            string name = ParseString();
                            if (name == null) return null;

                            // :
                            if (NextToken != TOKEN.COLON) return null;
                            reader.Read();

                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            List<object> ParseArray()
            {
                reader.Read(); // [
                var array = new List<object>();

                while (true)
                {
                    TOKEN nextToken = NextToken;
                    switch (nextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.COMMA: continue;
                        case TOKEN.SQUARED_CLOSE: return array;
                        default:
                            array.Add(ParseByToken(nextToken));
                            break;
                    }
                }
            }

            string ParseString()
            {
                reader.Read(); // opening "
                StringBuilder s = new StringBuilder();

                while (true)
                {
                    if (reader.Peek() == -1) break;
                    char c = NextChar;
                    switch (c)
                    {
                        case '"': return s.ToString();
                        case '\\':
                            if (reader.Peek() == -1) break;
                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/': s.Append(c); break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                case 'u':
                                    var hex = new char[4];
                                    for (int i = 0; i < 4; i++)
                                        hex[i] = NextChar;
                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;
                if (number.IndexOf('.') == -1 && number.IndexOf('E') == -1 && number.IndexOf('e') == -1)
                {
                    long parsedLong;
                    if (Int64.TryParse(number, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out parsedLong))
                        return parsedLong;
                }
                double parsedDouble;
                Double.TryParse(number, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out parsedDouble);
                return parsedDouble;
            }
        }
    }
}
