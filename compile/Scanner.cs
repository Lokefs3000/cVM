using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using compile.Utility;

namespace compile
{
    internal class Scanner
    {
        private List<Token> _tokens = new List<Token>();
        private string _source = string.Empty;

        private int _start;
        private int _current;

        private int _line;
        private int _row;

        private static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>()
        {
            { "return", TokenType.Return },
            { "else", TokenType.Else },
            { "false", TokenType.False },
            { "for", TokenType.For },
            { "void", TokenType.Void },
            { "if", TokenType.If },
            { "null", TokenType.Null },
            { "true", TokenType.True },
            { "while", TokenType.While },
            { "int", TokenType.Int },
            { "bool", TokenType.Bool },
            { "float", TokenType.Float },
            { "char", TokenType.Char },
            { "short", TokenType.Short },
            { "signed", TokenType.Signed },
            { "unsigned", TokenType.Unsigned },
            { "static", TokenType.Static },
            { "const", TokenType.Const },
            { "__asm__", TokenType.Assembly },
        };

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': PushToken(TokenType.LeftParen); break;
                case ')': PushToken(TokenType.RightParen); break;
                case '[': PushToken(TokenType.LeftBracket); break;
                case ']': PushToken(TokenType.RightBracket); break;
                case '{': PushToken(TokenType.LeftBrace); break;
                case '}': PushToken(TokenType.RightBrace); break;
                case '\'': Char(); break;
                case '\"': String(); break;
                case ',': PushToken(TokenType.Comma); break;
                case '.': PushToken(TokenType.Dot); break;
                case '-': PushToken(TokenType.Minus); break;
                case '+': PushToken(TokenType.Plus); break;
                case ';': PushToken(TokenType.Semicolon); break;
                case '*': PushToken(TokenType.Star); break;
                case '%': PushToken(TokenType.Modulo); break;
                case '!': PushToken(Match('=') ? TokenType.NotEqual : TokenType.Not); break;
                case '=': PushToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
                case '<': PushToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
                case '>': PushToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;
                case ' ':
                case '\r':
                case '\t':
                case '\0':
                    break;
                case '\n':
                    _line++;
                    _row = 0;
                    break;
                case '/':
                    if (Match('/'))
                        while (Peek() != '\n' && !IsEOF()) Advance();
                    else
                        PushToken(TokenType.Slash);
                    break;
                default:
                    if (char.IsDigit(c))
                        Number();
                    else if (char.IsLetter(c) || c == '_')
                        Identifier();
                    else
                        throw new InvalidDataException($"Unexpceted character: '{c}'");
                    break;
            }
        }

        private void String()
        {
            while (Peek() != '\"' && !IsEOF())
            {
                if (Peek() == '\n') _line++;
                Advance();
            }

            if (IsEOF())
                throw new IndexOutOfRangeException($"Unterminated string");

            Advance();
            PushToken(TokenType.String, _source.Substring(_start + 1, _current - 1 - _start));
        }

        private void Char()
        {
            char c = Advance();
            if (c == '\\')
            {
                PushToken(TokenType.Backslash);
                PushToken(TokenType.Char);
            }
            else
            {
                PushToken(TokenType.Char, c);
            }

            if (Advance() != '\'')
                throw new InvalidDataException($"Char literal can only contain a single value");
            else if (IsEOF())
                throw new InvalidDataException($"Unterminated character literal");
        }

        private void Number()
        {
            bool whole = true;

            while (char.IsDigit(Peek())) Advance();

            if (Peek() == '.')
            {
                whole = false;
                Advance();
                while (char.IsDigit(Peek())) Advance();
            }

            if (whole)
            {
                ulong value = ulong.Parse(_source.Substring(_start, _current - _start));
                PushToken(TokenType.Number, value);
            }
            else
            {
                double value = double.Parse(_source.Substring(_start, _current - _start));
                PushToken(TokenType.Fractional, value);
            }
        }

        private void Identifier()
        {
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();

            string text = _source.Substring(_start, _current - _start);
            TokenType type = TokenType.Identifier;
            if (Keywords.TryGetValue(text, out TokenType keyType)) type = keyType;
            PushToken(type);
        }

        private void PushToken(TokenType tokenType) => _tokens.Add(new Token { Type = tokenType, Value = _source.Substring(_start, _current - _start), XY = Utils.EncodeXY((ushort)_line, (ushort)_row) });
        private void PushToken(TokenType tokenType, object literal) => _tokens.Add(new Token { Type = tokenType, Value = _source.Substring(_start, _current - _start), Literal = literal, XY = Utils.EncodeXY((ushort)_line, (ushort)_row) });

        private bool Match(char c) { if (IsEOF()) return false; if (Peek() != c) return false; _row++; _current++; return true; }
        private char Peek() => IsEOF() ? '\0' : _source[_current];
        private char Advance() { _row++; return _source[_current++]; }
        private bool IsEOF() => _source.Length <= _current;

        public void Execute(string source)
        {
            _tokens.Clear();
            _source = source;

            _start = 0;
            _current = 0;
            _line = 0;

            List<string> exceptions = new List<string>();

            while (!IsEOF())
            {
                _start = _current;
                try
                {
                    ScanToken();
                }
                catch (Exception ex)
                {
                    exceptions.Add($"[{_line}:{_row}]: {ex.Message}");
                }
            }

            if (exceptions.Count > 0)
            {
                foreach (string exception in exceptions)
                {
                    Console.WriteLine(exception);
                }

                Console.WriteLine($"Reference source:\n{_source}");

                Environment.Exit(exceptions.Count);
            }
        }

        public Token[] RetrieveTokens() => _tokens.ToArray();

        public enum TokenType
        {
            LeftParen, RightParen, //()
            LeftBracket, RightBracket, //[]
            LeftBrace, RightBrace, //{}
            LeftAngle, RightAngle, //<>

            Slash, Star, Plus, Minus,
            Comma, Dot, Semicolon, Modulo,
            And, Or, Backslash, Identifier,

            NotEqual, Not,
            EqualEqual, Equal,
            LessEqual, Less,
            GreaterEqual, Greater,

            Int, Short, Char, Bool, Float,
            Unsigned, Signed, Static, Const,
            Number, String, Fractional,

            Return, Else, False, For,
            If, Null, True, While, Void,
            Assembly,

            EOF
        }

        public struct Token
        {
            public TokenType Type;
            public string Value;
            public object? Literal;
            public int XY;
        }
    }
}
