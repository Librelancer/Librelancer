using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.RandomMissions;


namespace LibreLancer.ContentEdit.RandomMissions;

public class VignetteParamsCompiler
{
    abstract class VStatement
    {
        public string Source;
        public Token Def;

        protected VStatement(string src, Token def)
        {
            Source = src;
            Def = def;
        }

        public static VStatement Parse(Lexer lexer, bool toplevel)
        {
            if (lexer.IsIdentifier("sub"))
            {
                if (!toplevel)
                    throw new Exception("'sub' can only appear in the top level");
                return new VSub(lexer);
            }
            if (lexer.IsIdentifier("group"))
            {
                if (!toplevel)
                    throw new Exception("'group' can only appear in the top level");
                return new VGroup(lexer);
            }
            if (lexer.IsIdentifier("if"))
                return new VIfElse(lexer);
            if(lexer.IsIdentifier("call"))
                return new VCall(lexer);
            if (lexer.IsIdentifier("err_unimplemented"))
                return new VUnimplemented(lexer);
            if (lexer.IsIdentifier("doc"))
                return new VDoc(lexer);
            if (lexer.IsIdentifier("comm_sequence"))
                return new VCommSequence(lexer);
            if (lexer.IsIdentifier("failure_text"))
                return new VFailureText(lexer);
            if(lexer.IsIdentifier("reward_text"))
                return new VRewardText(lexer);
            if (lexer.IsIdentifier("objective_text"))
                return new VObjectiveText(lexer);
            if (lexer.IsIdentifier("offer_text"))
                return new VOfferText(lexer);
            if (lexer.IsIdentifier("difficulty"))
                return new VDifficulty(lexer);
            if (lexer.IsIdentifier("weight"))
                return new VWeight(lexer);
            if (lexer.IsIdentifier("allowable_zone_types"))
                return new VAllowableZoneTypes(lexer);
            if (lexer.IsIdentifier("offer_group"))
                return new VOfferGroup(lexer);
            if (lexer.IsIdentifier("hostile_group"))
                return new VHostileGroup(lexer);
            if (lexer.Current.Kind == TokenKind.EndOfFile)
            {
                throw new CompileErrorException(lexer, "Unexpected end of file");
            }

            throw new CompileErrorException(lexer, $"Unexpected '{lexer.Current.Value}'");
        }

        protected static void AssertNext(Lexer lexer, TokenKind kind)
        {
            AssertKind(lexer, kind);
            lexer.Next();
        }

        protected static void AssertKind(Lexer lexer, TokenKind kind)
        {
            string exp = kind switch
            {
                TokenKind.Float => "number",
                TokenKind.Integer => "integer",
                TokenKind.Comma => "','",
                TokenKind.Semicolon => "';'",
                TokenKind.LeftParen => "'('",
                TokenKind.RightParen => "')'",
                TokenKind.Identifier => "identifier",
                _ => throw new InvalidOperationException()
            };
            if (kind == TokenKind.Float)
            {
                if (lexer.Current.Kind != TokenKind.Integer && lexer.Current.Kind != TokenKind.Float)
                {
                    throw new CompileErrorException(lexer, "Expected number");
                }
            }
            else if (lexer.Current.Kind != kind)
                throw new CompileErrorException(lexer, $"Expected {exp}");
        }

        public abstract void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context);

        protected AstData NewData(VignetteAst parent, VignetteTree tree)
        {
            if (parent is AstDecision dec && dec.Children.Count >= 2)
            {
                throw new CompileErrorException(Source, Def.Column, Def.Line, "Control flow cannot continue past if");
            }
            var id = tree.NextId();
            var n = new AstData(id, new DataNode());
            tree.Nodes[id] = n;
            parent.Children.Add(n);
            return n;
        }
    }

    abstract class VString : VStatement
    {
        public VignetteString String;

        protected VString(string src, Token def) : base(src, def)
        {
        }

        protected void Parse(Lexer lexer, string target)
        {
            String.Target = target;
            AssertKind(lexer, TokenKind.Integer);
            var args = new List<string>();
            String.Ids = int.Parse(lexer.Current.Value);
            lexer.Next();
            while (lexer.Current.Kind == TokenKind.Comma)
            {
                lexer.Next(); //skip comma
                AssertKind(lexer, TokenKind.Identifier);
                args.Add(lexer.Current.Value);
                lexer.Next(); // go to comma
            }
            AssertNext(lexer, TokenKind.Semicolon);
            String.Arguments = args.ToArray();
        }
    }

    class VFailureText : VString
    {
        public VFailureText(Lexer lexer) :
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("failure_text"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            Parse(lexer, "failure_text");
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.KindMatch(DataNodeKind.None, DataNodeKind.Objective))
            {
                dat.Data.FailureText = String;
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.FailureText = String;
                node = n;
            }
        }
    }

    class VRewardText : VString
    {
        public VRewardText(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("reward_text"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            Parse(lexer, "reward_text");
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.KindMatch(DataNodeKind.None, DataNodeKind.Objective))
            {
                dat.Data.RewardText = String;
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.RewardText = String;
                node = n;
            }
        }
    }

    class VObjectiveText : VString
    {
        public VObjectiveText(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("objective_text"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            var tgt = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            Parse(lexer, tgt);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.KindMatch(DataNodeKind.None, DataNodeKind.Objective))
            {
                dat.Data.ObjectiveTexts.Add(String);
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.ObjectiveTexts.Add(String);
                node = n;
            }
        }

    }

    class VGroup : VStatement
    {
        public string Name;
        public List<string> Factions = new List<string>();

        public VGroup(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("group"))
                throw new InvalidOperationException();
           lexer.Next();
           AssertKind(lexer, TokenKind.Identifier);
           Name = lexer.Current.Value;
           do
           {
                // skip def/comma
               lexer.Next();
               AssertKind(lexer, TokenKind.Identifier);
               Factions.Add(lexer.Current.Value);
               lexer.Next(); // go to comma
           } while (lexer.Current.Kind == TokenKind.Comma);

            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            throw new InvalidOperationException("Compile() called on VGroup");
        }
    }

    class VSub : VStatement
    {
        public string Name;
        public List<VStatement> Statements = new List<VStatement>();

        public VSub(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("sub"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            if (lexer.Current.Kind != TokenKind.Identifier)
                throw new InvalidOperationException();
            Name = lexer.Current.Value;
            lexer.Next();
            while (!lexer.IsIdentifier("end") && lexer.Current.Kind != TokenKind.EndOfFile)
                Statements.Add(Parse(lexer, false));
            if (lexer.Current.Kind == TokenKind.EndOfFile)
                throw new Exception("Unexpected end of file");
            lexer.Next();
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            throw new InvalidOperationException("Compile() called on VSub");
        }
    }

    class VCall : VStatement
    {
        public string Target;

        public VCall(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("call"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            Target = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            node.Children.Add(context.Subs[Target]);
        }
    }

    class VDoc : VStatement
    {
        public string Doc;

        public VDoc(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("doc"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            Doc = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            var p = node;
            var id = tree.NextId();
            var d = new AstDoc(id, new DocumentationNode() { Documentation = Doc });
            tree.Nodes[id] = d;
            node = d;
            p?.Children?.Add(d);
        }
    }

    class VUnimplemented : VStatement
    {
        public VUnimplemented(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("err_unimplemented"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat)
            {
                dat.Data.Implemented = false;
            }
            else
            {
                var d = NewData(node, tree);
                d.Data.Implemented = false;
                node = d;
            }
        }
    }

    class VCommSequence : VStatement
    {
        public CommSequence CommSequence;

        public VCommSequence(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("comm_sequence"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            var ev = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Identifier);
            var tgt = Enum.Parse<CommSequenceTarget>(lexer.Current.Value, true);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Float);
            var unk1 = float.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Float);
            var unk2 = float.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Float);
            var unk3 = float.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Identifier);
            var src = Enum.Parse<CommSequenceSource>(lexer.Current.Value, true);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Identifier);
            var comm = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);

            CommSequence = new CommSequence()
            {
                Event = ev, Target = tgt, Unknown1 = unk1,
                Unknown2 = unk2, Unknown3 = unk3, Source = src, Comm = comm
            };
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.KindMatch(DataNodeKind.None, DataNodeKind.CommSequence))
            {
                dat.Data.CommSequences.Add(CommSequence);
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.CommSequences.Add(CommSequence);
                node = n;
            }
        }
    }

    class VDifficulty : VStatement
    {
        public Vector2 Difficulty;

        public VDifficulty(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if(!lexer.IsIdentifier("difficulty"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Float);
            Difficulty.X = float.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Comma);
            AssertKind(lexer, TokenKind.Float);
            Difficulty.Y = float.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.KindMatch(DataNodeKind.None, DataNodeKind.Difficulty))
            {
                dat.Data.Difficulty = Difficulty;
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.Difficulty = Difficulty;
                node = n;
            }
        }
    }

    class VWeight : VStatement
    {
        public int Weight;

        public VWeight(Lexer lexer)
            : base(lexer.Source, lexer.Current)
        {
            if(!lexer.IsIdentifier("weight"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            lexer.Next();
            AssertKind(lexer, TokenKind.Integer);
            Weight = int.Parse(lexer.Current.Value, CultureInfo.InvariantCulture);
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.GetKind() == DataNodeKind.None)
            {
                dat.Data.Weight = Weight;
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.Weight = Weight;
                node = n;
            }
        }
    }

    class VIfElse : VStatement
    {
        public List<IfBlock> Blocks = new List<IfBlock>();

        public class IfBlock
        {
            public BlockKind Kind;
            public string Condition;
            public List<VStatement> Statements = new();
        }

        public enum BlockKind
        {
            Condition,
            Group,
            Else
        }

        public VIfElse(Lexer lexer):
            base(lexer.Source, lexer.Current)
        {
            if(!lexer.IsIdentifier("if"))
                throw new InvalidOperationException();
            Def = lexer.Current;
            Blocks.Add(GetBlock(lexer, true));
            while(!lexer.IsIdentifier("end"))
            {
                var nb = GetBlock(lexer, false);
                if (nb == null)
                    break;
                Blocks.Add(nb);
                if (nb.Kind == BlockKind.Else)
                    break;
            }

            if (!lexer.IsIdentifier("end"))
                throw new CompileErrorException(lexer, "Expected 'end'");
            if (Blocks.Count == 1)
                throw new CompileErrorException(lexer, "if expecting elif or else");
            lexer.Next();
        }

        IfBlock GetBlock(Lexer lexer, bool isIf)
        {
            AssertKind(lexer, TokenKind.Identifier);
            if (lexer.IsIdentifier("end"))
                return null;
            IfBlock blk = new IfBlock();
            if (lexer.IsIdentifier("else"))
            {
                blk.Kind = BlockKind.Else;
                lexer.Next();
            }
            else if (lexer.IsIdentifier("elif") ||
                     (isIf && lexer.IsIdentifier("if")))
            {
                lexer.Next();
                if(lexer.IsIdentifier("group"))
                {
                    lexer.Next();
                    AssertNext(lexer, TokenKind.LeftParen);
                    blk.Kind = BlockKind.Group;
                    blk.Condition = lexer.Current.Value;
                    lexer.Next();
                    AssertNext(lexer, TokenKind.RightParen);
                }
                else
                {
                    AssertKind(lexer, TokenKind.Identifier);
                    blk.Kind = BlockKind.Condition;
                    blk.Condition = lexer.Current.Value;
                    lexer.Next();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
            while (!lexer.IsIdentifier("end") && !lexer.IsIdentifier("else") &&
                   !lexer.IsIdentifier("elif") && lexer.Current.Kind != TokenKind.EndOfFile)
            {
                blk.Statements.Add(Parse(lexer, false));
            }
            if (lexer.Current.Kind == TokenKind.EndOfFile)
            {
                throw new Exception("Unexpected end of file");
            }
            return blk;
        }

        void CompileBlock(VignetteAst parent, IfBlock block, VignetteTree tree, CompilerData context)
        {
            if (block.Kind != BlockKind.Group &&
                block.Statements[0] is VIfElse ie)
            {
                if (block.Statements.Count > 1)
                {
                    throw new CompileErrorException(Source, block.Statements[1].Def.Column,
                        block.Statements[1].Def.Line,
                        "Control flow cannot continue past if");
                }
                ie.Compile(ref parent, tree, context);
            }
            else
            {
                var dn = NewData(parent, tree);
                if (block.Kind == BlockKind.Group) {
                    dn.Data.OfferGroup = context.Groups[block.Condition];
                }
                VignetteAst top = dn;
                foreach(var s in block.Statements)
                    s.Compile(ref top, tree, context);
            }
        }


        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            int mainId = tree.NextId();
            AstDecision dec = new AstDecision(mainId, new DecisionNode());
            dec.Decision.Nickname = Blocks[0].Kind == BlockKind.Group ? "branch" : Blocks[0].Condition;
            tree.Nodes[mainId] = dec;
            node.Children.Add(dec);
            node = dec;

            CompileBlock(dec, Blocks[0], tree, context);

            for (int i = 1; i < Blocks.Count; i++)
            {
                if (i == Blocks.Count - 1)
                {
                    CompileBlock(dec, Blocks[i], tree, context);
                }
                else
                {
                    var newId = tree.NextId();
                    var newDec = new AstDecision(newId, new DecisionNode());
                    newDec.Decision.Nickname = Blocks[i].Kind == BlockKind.Group ? "branch" : Blocks[i].Condition;
                    tree.Nodes[newId] = newDec;
                    dec.Children.Add(newDec);
                    CompileBlock(newDec, Blocks[i], tree, context);
                    dec = newDec;
                }
            }
        }
    }

    class VAllowableZoneTypes : VStatement
    {
        public List<string> Types = new();

        public VAllowableZoneTypes(Lexer lexer) :
            base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("allowable_zone_types"))
                throw new InvalidOperationException();
            do
            {
                lexer.Next();
                AssertKind(lexer, TokenKind.Identifier);
                Types.Add(lexer.Current.Value);
                lexer.Next();
            } while (lexer.Current.Kind == TokenKind.Comma);
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.GetKind() == DataNodeKind.None)
            {
                dat.Data.AllowableZoneTypes = Types.ToArray();
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.AllowableZoneTypes = Types.ToArray();
                node = n;
            }
        }
    }

    class VOfferGroup : VStatement
    {
        public string Group;
        public VOfferGroup(Lexer lexer)
            : base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("offer_group"))
                throw new InvalidOperationException();
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            Group = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.GetKind() == DataNodeKind.None)
            {
                dat.Data.OfferGroup = context.Groups[Group];
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.OfferGroup = context.Groups[Group];
                node = n;
            }
        }
    }

    class VHostileGroup : VStatement
    {
        public string Group;
        public VHostileGroup(Lexer lexer)
            : base(lexer.Source, lexer.Current)
        {
            if (!lexer.IsIdentifier("hostile_group"))
                throw new InvalidOperationException();
            lexer.Next();
            AssertKind(lexer, TokenKind.Identifier);
            Group = lexer.Current.Value;
            lexer.Next();
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.GetKind() == DataNodeKind.None)
            {
                dat.Data.HostileGroup = context.Groups[Group];
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.HostileGroup = context.Groups[Group];
                node = n;
            }
        }
    }

    class VOfferText : VStatement
    {
        public List<OfferTextEntry> Entries = new();

        public VOfferText(Lexer lexer)
            : base(lexer.Source, lexer.Current)
        {
            if(!lexer.IsIdentifier("offer_text"))
                throw new InvalidOperationException();
            lexer.Next();
            AssertKind(lexer, TokenKind.LeftParen);
            do
            {
                lexer.Next();
                AssertKind(lexer, TokenKind.Identifier);
                if (!Enum.TryParse<OfferTextOp>(lexer.Current.Value, true, out var op)) {
                    throw new CompileErrorException(lexer, "Expected 'append' or 'replace'");
                }
                lexer.Next();
                AssertNext(lexer, TokenKind.LeftParen);
                var ent = new OfferTextEntry() { Op = op };
                var items = new List<OfferTextItem>();
                do
                {
                    var t = OfferTextType.none;
                    if (lexer.IsIdentifier("singular"))
                    {
                        t = OfferTextType.singular;
                        lexer.Next();
                        AssertNext(lexer, TokenKind.Comma);
                    }
                    else if (lexer.IsIdentifier("plural"))
                    {
                        t = OfferTextType.plural;
                        lexer.Next();
                        AssertNext(lexer, TokenKind.Comma);
                    }
                    AssertKind(lexer, TokenKind.Integer);
                    int ids = int.Parse(lexer.Current.Value);
                    lexer.Next();
                    var args = new List<string>();
                    while (lexer.Current.Kind == TokenKind.Comma)
                    {
                        lexer.Next();
                        if (lexer.Current.Kind == TokenKind.Identifier &&
                            !lexer.IsIdentifier("singular") &&
                            !lexer.IsIdentifier("plural"))
                        {
                            args.Add(lexer.Current.Value);
                            lexer.Next();
                        }
                        else
                        {
                            break;
                        }
                    }
                    items.Add(new OfferTextItem() { Ids = ids, Type = t, Args = args.ToArray() });
                } while (lexer.Current.Kind != TokenKind.RightParen);
                ent.Items = items.ToArray();
                Entries.Add(ent);
                AssertNext(lexer, TokenKind.RightParen);
            } while (lexer.Current.Kind == TokenKind.Comma);
            AssertNext(lexer, TokenKind.RightParen);
            AssertNext(lexer, TokenKind.Semicolon);
        }

        public override void Compile(ref VignetteAst node, VignetteTree tree, CompilerData context)
        {
            if (node is AstData dat && dat.GetKind() == DataNodeKind.None)
            {
                dat.Data.OfferTexts = Entries;
            }
            else
            {
                var n = NewData(node, tree);
                n.Data.OfferTexts = Entries;
                node = n;
            }
        }
    }

    class CompilerData
    {
        public Dictionary<string, VignetteAst> Subs = new();
        public Dictionary<string, string[]> Groups = new();
    }


    static void Write(VignetteAst node, IniBuilder b)
    {
        IniBuilder.IniSectionBuilder s;
        if (node is AstData data)
        {
            s = b.Section("DataNode");
            s.Entry("node_id", data.Id);
            if (data.Data.Weight != null)
                s.Entry("Weight", data.Data.Weight.Value);
            if (!data.Data.Implemented)
                s.Entry("Implemented", false);
            if(data.Data.OfferGroup?.Length > 0)
                s.Entry("Offer_group", data.Data.OfferGroup);
            if (data.Data.HostileGroup?.Length > 0)
                s.Entry("Hostile_group", data.Data.HostileGroup);
            if(data.Data.AllowableZoneTypes?.Length > 0)
                s.Entry("allowable_zone_types", data.Data.AllowableZoneTypes);
            if (data.Data.Difficulty != null)
                s.Entry("difficulty", data.Data.Difficulty.Value.X, data.Data.Difficulty.Value.Y);

            if (data.Data.FailureText.Target != null)
            {
                var v = new List<ValueBase>() {  data.Data.FailureText.Ids};
                v.AddRange(data.Data.FailureText.Arguments.Select(x => (ValueBase)x));
                s.Entry("Failure_text", v.ToArray());
            }
            if (data.Data.RewardText.Target != null)
            {
                var v = new List<ValueBase>() {  data.Data.RewardText.Ids};
                v.AddRange(data.Data.RewardText.Arguments.Select(x => (ValueBase)x));
                s.Entry("Reward_text", v.ToArray());
            }
            foreach (var t in data.Data.ObjectiveTexts)
            {
                var v = new List<ValueBase>() { t.Target, t.Ids };
                v.AddRange(t.Arguments.Select(x => (ValueBase)x));
                s.Entry("objective_text", v.ToArray());
            }
            foreach (var ot in data.Data.OfferTexts)
            {
                var v = new List<ValueBase>() { ot.Op.ToString() };
                foreach (var it in ot.Items)
                {
                    if(it.Type != OfferTextType.none)
                        v.Add(it.Type.ToString());
                    v.Add(it.Ids);
                    v.AddRange(it.Args.Select(x => (ValueBase)x));
                }

                s.Entry("offer_text", v.ToArray());
            }
            foreach (var cs in data.Data.CommSequences)
            {
                s.Entry("comm_sequence", cs.Event, cs.Target.ToString(),
                    cs.Unknown1, cs.Unknown2, cs.Unknown3,
                    cs.Source.ToString(), cs.Comm);
            }
        }
        else if (node is AstDecision decision)
        {
            s = b.Section("DecisionNode")
                .Entry("node_id", decision.Id)
                .Entry("nickname", decision.Decision.Nickname);
        }
        else if (node is AstDoc doc)
        {
            s = b.Section("DocumentationNode")
                .Entry("node_id", doc.Id)
                .Entry("documentation", doc.Docs.Documentation);
        }
        else
        {
            throw new InvalidOperationException($"Internal error: Tried to dump {node.GetType().Name}");
        }

        foreach (var c in node.Children)
        {
            s.Entry("child_node", c.Id);
        }
    }




    public static List<Section> Compile(string text, string source)
    {
        var lex = new Lexer(text, source);
        var tree = new VignetteTree();
        var data = new CompilerData();

        // Parse top level statements
        var subs = new Dictionary<string, VSub>();
        var statements = new List<VStatement>();
        while (lex.Current.Kind != TokenKind.EndOfFile)
        {
            var next = VStatement.Parse(lex, true);
            if (next is VGroup g)
            {
                if (!data.Groups.TryAdd(g.Name, g.Factions.ToArray()))
                    throw new CompileErrorException(source, g.Def.Column, g.Def.Line, $"Duplicate group '{g.Name}'");
            }
            else if (next is VSub sub)
            {
                if(!subs.TryAdd(sub.Name, sub))
                    throw new CompileErrorException(source, sub.Def.Column, sub.Def.Line, $"Duplicate sub '{sub.Name}'");
            }
            else
            {
                statements.Add(next);
            }
        }

        // Allocate empty nodes for sub resolve
        foreach (var s in subs)
        {
            var id = tree.NextId();
            var node = new AstData(id, new DataNode());
            tree.Nodes[id] = node;
            data.Subs[s.Key] = node;
        }

        // Compile root first, attaching to entry point node ID 2
        var rootNode = new AstData(2, new DataNode());
        VignetteAst currentTop = rootNode;
        tree.Nodes[2] = rootNode;
        foreach (var s in statements)
        {
            s.Compile(ref currentTop, tree, data);
        }
        // Compile subs after, attaching to their root node
        foreach (var s in subs)
        {
            currentTop = data.Subs[s.Key];
            foreach (var v in s.Value.Statements)
            {
                v.Compile(ref currentTop, tree, data);
            }
        }

        tree.FlattenEmptyNodes(rootNode);

        if (VignetteTree.IsEmptyData(rootNode) &&
            rootNode.Children.Count == 1)
        {
            //Flatten root, specifically setting ID to 2
            var newRoot = rootNode.Children[0];
            tree.Nodes.Remove(newRoot.Id);
            newRoot.Id = 2;
            tree.Nodes[2] = newRoot;
        }

        // Create ini
        var ib = new IniBuilder();
        foreach (var n in tree.Nodes.OrderBy(x => x.Key))
        {
            Write(n.Value, ib);
        }

        return ib.Sections;
    }
}
