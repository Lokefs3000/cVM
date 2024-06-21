using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compile
{
    internal class Preprosessor
    {
        private List<string> _includes = new List<string>();

        private List<string> _logicalLines = new List<string>();
        private List<Token> _lineTokens = new List<Token>();

        private Dictionary<string, Define> _defines = new Dictionary<string, Define>();

        private int _lineIndex = 0;
        private int _charIndex = 0;

        private int _ifDepth = 0;

        private List<int> _fileStarts = new List<int>();

        private void TokenizeLine(string line)
        {
            char ch = AdvanceChar(line);

            switch (ch)
            {
                case '#': DirectiveToken(line); break;
                case '(': PushToken(TokenType.LeftParanthesis); break;
                case ')': PushToken(TokenType.RightParanthesis); break;
                case '\'':
                case '\"': StringToken(line); break;
                case '\n':
                case '\r':
                case ' ':
                case '\0':
                    break;
                default: IdentifierToken(line); break;
            }
        }

        private void DirectiveToken(string line)
        {
            int start = _charIndex - 1;
            while (!char.IsWhiteSpace(PeekChar(line)) && PeekChar(line) != '\0') AdvanceChar(line);
            PushToken(TokenType.Directive, line.Substring(start, _charIndex - start));
        }

        private void StringToken(string line)
        {
            int start = _charIndex;
            while (PeekChar(line) != '\'' && PeekChar(line) != '\"' && PeekChar(line) != '\r' && PeekChar(line) != '\n' && PeekChar(line) != '\0') AdvanceChar(line);
            PushToken(TokenType.String, line.Substring(start, _charIndex - start));
        }

        private void IdentifierToken(string line)
        {
            int start = _charIndex - 1;
            while (!char.IsWhiteSpace(PeekChar(line)) && PeekChar(line) != '\0') AdvanceChar(line);
            PushToken(TokenType.Identifier, line.Substring(start, _charIndex - start));
        }

        private void PushToken(TokenType type, string value = "") => _lineTokens.Add(new Token { Type = type, Value = value });
        private char PeekChar(string line) => IsEOFChar(line) ? '\0' : line[_charIndex];
        private char AdvanceChar(string line) => line[_charIndex++];
        private bool IsEOFChar(string line) => _charIndex >= line.Length;

        private void ProcessLine()
        {
            string line = Advance();

            _lineTokens.Clear();
            _charIndex = 0;
            while (!IsEOFChar(line)) TokenizeLine(line);

            if (_lineTokens.Count > 0 && _lineTokens[0].Type == TokenType.Directive)
            {
                switch (_lineTokens[0].Value)
                {
                    case "#define":
                        DefineDirective();
                        break;
                    case "#ifdef":
                        IfdefDirective();
                        break;
                    case "#ifndef":
                        IfndefDirective();
                        break;
                    case "#else":
                        ElseDirective();
                        break;
                    case "#elif":
                        ElifDirective();
                        break;
                    case "#endif":
                        EndifDirective();
                        break;
                    case "#include":
                        IncludeDirective();
                        break;
                    default:
                        break;
                }
            }
        }

        private void DefineDirective()
        {
            Define define = new Define();
            define.Name = _lineTokens[1].Value;
            define.Values = new List<string>();

            int i = 2;
            if (_lineTokens.Count > 2 && _lineTokens[2].Type == TokenType.LeftParanthesis)
            {
                while (_lineTokens.Count >= i && _lineTokens[i++].Type != TokenType.RightParanthesis)
                {
                    string value = _lineTokens[i].Value.Trim(',', '\0', ' ');
                    define.Values.Add(value);
                }
            }

            StringBuilder bodyBuilder = new StringBuilder();
            for (; i < _lineTokens.Count; i++)
                bodyBuilder.Append($"{_lineTokens[i].Value} ");
            define.Body = bodyBuilder.ToString().Trim();

            if (!_defines.TryAdd(define.Name, define))
                _defines[define.Name] = define;

            _logicalLines[_lineIndex - 1] = string.Empty;
        }

        private void ElifDirective()
        {

        }

        private void ElseDirective()
        {

        }

        private void EndifDirective()
        {
            if (_ifDepth > 0)
                _ifDepth--;
            else
                throw new ArgumentOutOfRangeException("Endif directive left without initializer");
            _logicalLines[_lineIndex - 1] = string.Empty;
        }

        private void IfdefDirective()
        {
            _logicalLines[_lineIndex - 1] = string.Empty;
            if (!_defines.ContainsKey(_lineTokens[1].Value))
            {
                while (Peek().Trim() != "#endif" && !IsEOF())
                {
                    _logicalLines[_lineIndex] = string.Empty;
                    Advance();
                }
            }
            else
                _ifDepth++;
        }

        private void IfndefDirective()
        {
            _logicalLines[_lineIndex - 1] = string.Empty;
            if (_defines.ContainsKey(_lineTokens[1].Value))
            {
                while (Peek().Trim() != "#endif" && !IsEOF())
                {
                    _logicalLines[_lineIndex] = string.Empty;
                    Advance();
                }
            }
            else
                _ifDepth++;
        }

        private void IncludeDirective()
        {
            _logicalLines[_lineIndex - 1] = string.Empty;

            if (_lineTokens[1].Type == TokenType.String)
            {
                string file = _lineTokens[1].Value;
                if (!File.Exists(file))
                    throw new FileNotFoundException($"Failed to find include file: {file}!");

                string[] newLines = File.ReadAllLines(file);
                _logicalLines.InsertRange(_lineIndex, newLines);
                _lineIndex--;
            }
            else
                throw new FileNotFoundException($"Include path missing!");
        }

        private string Peek() => IsEOF() ? string.Empty : _logicalLines[_lineIndex];
        private string Advance() => _logicalLines[_lineIndex++];
        private bool IsEOF() => _lineIndex >= _logicalLines.Count;

        public void Execute(string path, List<string> includes)
        {
            _includes = includes;

            _logicalLines.Clear();
            _logicalLines.AddRange(File.ReadAllLines(path));
            _defines.Clear();
            _ifDepth = 0;
            _fileStarts.Clear();

            while (!IsEOF())
            {
                ProcessLine();
            }
        }

        public string RetrieveAsString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string line in _logicalLines)
                builder.AppendLine(line);
            return builder.ToString();
        }

        public int[] GetFileStartLocations() => _fileStarts.ToArray();

        private enum TokenType
        {
            Directive,
            Identifier,
            LeftParanthesis,
            RightParanthesis,
            String
        }

        private struct Token
        {
            public TokenType Type;
            public string Value;
            public int Position;
        }

        private struct Define
        {
            public string Name;
            public List<string> Values;
            public string Body;
        }
    }
}
