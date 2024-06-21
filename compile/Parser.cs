using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using compile.Debug;
using compile.Utility;

namespace compile
{
    internal class Parser
    {
        private TreeNode _root;
        private TreeNode _local;

        private List<Scanner.Token> _tokens = new List<Scanner.Token>();

        private List<Dictionary<string, VarDecl>> _variables = new List<Dictionary<string, VarDecl>>();
        private Dictionary<string, FuncDecl> _functions = new Dictionary<string, FuncDecl>();

        private int _current;
        private int _bodyDepth;

        private void ParseToken()
        {
            switch (_tokens[_current++].Type)
            {
                case Scanner.TokenType.Int:
                case Scanner.TokenType.Float:
                case Scanner.TokenType.Bool:
                case Scanner.TokenType.Char:
                case Scanner.TokenType.Void:
                case Scanner.TokenType.Short: IdentifyUsage(); break;
                case Scanner.TokenType.Assembly: DeclareAssembly(); break;
                case Scanner.TokenType.RightBrace: EndActiveBody(); break;
                default:
                    throw new NotImplementedException($"Unrecognized token: {_tokens[_current-1].Type}({_tokens[_current - 1].Value})");
            }
        }

        private void EndActiveBody()
        {
            _variables[_bodyDepth--].Clear();
            if (_functions.ContainsKey(_local.Parent.Name))
                _local = _local.Parent.Parent;
            else
                _local = _local.Parent;
        }

        private void IdentifyUsage()
        {
            _current--;
            TypeSpec spec = GetTypeSpec();
            string identifier = GetIdentifier();

            if (_tokens[_current++].Type == Scanner.TokenType.LeftParen)
                DeclareFunction(spec, identifier);
            else
                DeclareVariable(spec, identifier);
        }

        private void DeclareFunction(TypeSpec spec, string identifier)
        {
            if (_functions.ContainsKey(identifier))
                throw new Exception($"Function: {identifier} already exists!");

            FuncDecl decleration = new FuncDecl();
            decleration.Name = identifier;
            decleration.Parameters = new Dictionary<string, TypeSpec>();
            if ((spec.Type == Scanner.TokenType.Void && spec.IsPointer) || (spec.Type != Scanner.TokenType.Void))
                decleration.Return = spec;

            if (_tokens[_current].Type != Scanner.TokenType.RightParen)
            {
                while (true)
                {
                    TypeSpec argumentSpec = GetTypeSpec();
                    string argumentId = GetIdentifier();

                    if (!decleration.Parameters.TryAdd(argumentId, argumentSpec) || IsVariableDeclared(argumentId))
                        throw new Exception($"Function argument: {argumentSpec.ToString()} {argumentId} is already defined!");

                    if (_tokens[_current].Type == Scanner.TokenType.RightParen)
                        break;
                    else if (_tokens[_current].Type != Scanner.TokenType.Comma)
                        throw new Exception("Missing function argument seperator!");
                }

                _current++;
            }
            else
                _current++;

            _functions.Add(decleration.Name, decleration);
            if (_tokens[_current].Type == Scanner.TokenType.LeftBrace)
                DeclareFunctionBody(identifier);
            else if (_tokens[_current++].Type != Scanner.TokenType.Semicolon)
                throw new Exception("Missing function body!");
        }

        private bool IsVariableDeclared(string id)
        {
            for (int i = 0; i < _bodyDepth; i++)
                if (_variables[i].ContainsKey(id))
                    return true;
            return false;
        }

        private void DeclareFunctionBody(string identifier)
        {
            _current++;
            if (_functions.TryGetValue(identifier, out FuncDecl decleration))
            {
                if (decleration.Body == null)
                {
                    decleration.Body = new TreeNode();
                    decleration.Body.Parent = _local;
                    _local.Nodes.Add(identifier, decleration.Body);
                    _local = decleration.Body;
                    _local.Name = identifier;
                    _bodyDepth++;

                    if (_variables.Count <= _bodyDepth)
                        while (_bodyDepth >= _variables.Count)
                            _variables.Add(new Dictionary<string, VarDecl>());

                    TreeNode args = new TreeNode();
                    args.Name = "Arguments";
                    args.Parent = _local;
                    foreach (KeyValuePair<string, TypeSpec> param in decleration.Parameters)
                    {
                        TreeNode arg = new TreeNode();
                        arg.Name = param.Key;
                        arg.Literals.Add("Name", param.Key);
                        arg.Literals.Add("Type", param.Value.Type);
                        arg.Literals.Add("Ptr", param.Value.IsPointer);
                        arg.Parent = args;
                        args.Nodes.Add($"Argument{args.Nodes.Count}", arg);

                        _variables[_bodyDepth].Add(param.Key, new VarDecl { Name = param.Key, Type = param.Value });
                    }
                    _local.Nodes.Add("Arguments", args);

                    TreeNode body = new TreeNode();
                    body.Name = "Body";
                    body.Parent = _local;
                    _local.Nodes.Add("Body", body);

                    _local = body;
                }
                else
                    throw new Exception($"Cannot redefine body of function: {identifier}");
            }
            else
                throw new Exception($"Could not find function for body: {identifier}");
        }

        private void DeclareVariable(TypeSpec spec, string identifier)
        {
            if (spec.Type == Scanner.TokenType.Void && !spec.IsPointer)
                throw new Exception("Incomplete variable not allowed");

            VarDecl decleration = new VarDecl();
            decleration.Name = identifier;
            decleration.Type = spec;

            Scanner.Token follower = _tokens[_current-1];
            if (follower.Type == Scanner.TokenType.Equal)
            {
                follower = _tokens[_current++];
                if (follower.Literal == null)
                    throw new Exception("Value literal null!");

                if (follower.Type == Scanner.TokenType.Fractional)
                {
                    if (spec.IsPointer)
                        throw new Exception("Pointer cannot have fractional value");

                    switch (spec.Type)
                    {
                        case Scanner.TokenType.Int: decleration.Value = (int)Math.Round((double)follower.Literal); break;
                        case Scanner.TokenType.Float: decleration.Value = (float)Math.Round((double)follower.Literal); break;
                        case Scanner.TokenType.Bool: decleration.Value = 0; break;
                        case Scanner.TokenType.Char: decleration.Value = (byte)Math.Round((double)follower.Literal); break;
                        case Scanner.TokenType.Short: decleration.Value = (short)Math.Round((double)follower.Literal); break;
                        default:
                            throw new Exception("Unkown variable type");
                    }
                }
                else if (follower.Type == Scanner.TokenType.Number)
                {
                    switch (spec.Type)
                    {
                        case Scanner.TokenType.Int: decleration.Value = (int)(ulong)follower.Literal; break;
                        case Scanner.TokenType.Float: decleration.Value = (float)(ulong)follower.Literal; break;
                        case Scanner.TokenType.Bool: decleration.Value = 0; break;
                        case Scanner.TokenType.Char: decleration.Value = (byte)(ulong)follower.Literal; break;
                        case Scanner.TokenType.Short: decleration.Value = (short)(ulong)follower.Literal; break;
                        default:
                            throw new Exception("Unkown variable type");
                    }
                }
                else
                    throw new Exception("Missing type variable decleration");

                follower = _tokens[_current++];
            }
            if (follower.Type != Scanner.TokenType.Semicolon)
                throw new Exception("Variable decleration not terminated");

            if (_variables.Count <= _bodyDepth)
                while (_bodyDepth >= _variables.Count)
                    _variables.Add(new Dictionary<string, VarDecl>());
            _variables[_bodyDepth].Add(identifier, decleration);

            TreeNode var = new TreeNode();
            var.Name = $"Variable{_local.Nodes.Count}";
            var.Literals.Add("Type", spec.Type);
            var.Literals.Add("Ptr", spec.IsPointer);
            if (decleration.Value != null)
                var.Literals.Add("Value", decleration.Value);
            _local.Nodes.Add(var.Name, var);
        }

        private void DeclareAssembly()
        {
            if (_tokens[_current++].Type != Scanner.TokenType.LeftBrace)
                throw new Exception("Missing assembly block body");

            TreeNode asm = new TreeNode();
            asm.Name = "Assembly";

            while (true)
            {
                Scanner.Token token = _tokens[_current++];
                if (token.Type == Scanner.TokenType.RightBrace)
                    break;
                else if (token.Type != Scanner.TokenType.String)
                    throw new Exception($"Expected string! Got: {token.Value} ({token.Type})");

                string str = token.Value;

                TreeNode block = new TreeNode { Name = "SourceBlock" };
                block.Literals.Add("Source", str);
                asm.Nodes.Add($"SourceBlock{asm.Nodes.Count}", block);
            }

            _local.Nodes.Add($"Assembly{_local.Nodes.Count}", asm);
        }

        private TypeSpec GetTypeSpec()
        {
            Scanner.Token token = _tokens[_current++];
            if (!IsValidVariableType(token.Type))
                throw new Exception($"Unkown type specifictation: {token.Value} ({token.Type})");

            bool isPointer = false;
            if (_tokens[_current++].Type == Scanner.TokenType.Star)
                isPointer = true;
            else
                _current--;

            return new TypeSpec
            {
                Type = token.Type,
                IsPointer = isPointer,
            };
        }

        private string GetIdentifier()
        {
            Scanner.Token token = _tokens[_current++];
            if (token.Type != Scanner.TokenType.Identifier)
                throw new Exception($"Expected identifier! Got: {token.Value} ({token.Type})");
            return token.Value;
        }

        private bool IsValidVariableType(Scanner.TokenType type)
        {
            switch (type)
            {
                case Scanner.TokenType.Int:
                case Scanner.TokenType.Float:
                case Scanner.TokenType.Bool:
                case Scanner.TokenType.Char:
                case Scanner.TokenType.Short:
                case Scanner.TokenType.Void:
                    return true;
                default:
                    return false;
            }
        }

        public void Execute(Scanner.Token[] tokens)
        {
            _root = new TreeNode();
            _root.Nodes.Add("Body", new TreeNode());
            _local = _root.Nodes["Body"];
            _local.Parent = _root;

            _tokens.Clear();
            _tokens.AddRange(tokens);

            _variables.Clear();
            _functions.Clear();

            _current = 0;
            _bodyDepth = 0;

            bool success = true;
            while (_current < _tokens.Count)
            {
                try
                {
                    ParseToken();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Utils.DecodeX(_tokens[_current - 1].XY)}:{Utils.DecodeY(_tokens[_current - 1].XY)}]: {ex.Message}");
                    success = false;
                    //throw;
                }
            }

            ParserDebug.Write(_root);

            if (!success)
                Environment.Exit(-1);
        }

        private struct TypeSpec
        {
            public Scanner.TokenType Type;
            public bool IsPointer;

            public override string ToString() => $"{Type.ToString()}{(IsPointer ? "*" : string.Empty)}";
        }

        private class FuncDecl
        {
            public string Name;
            public Dictionary<string, TypeSpec> Parameters;
            public TypeSpec? Return;
            public TreeNode? Body;
        }

        private class VarDecl
        {
            public string Name;
            public TypeSpec Type;
            public object? Value;
        }

        public class TreeNode
        {
            public string Name;
            public Dictionary<string, object> Literals = new Dictionary<string, object>();
            public Dictionary<string, TreeNode> Nodes = new Dictionary<string, TreeNode>();

            public TreeNode Parent;

            public TreeNode() { }
        }
    }
}
