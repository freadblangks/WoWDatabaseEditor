using Antlr4.Runtime;
using WDE.PacketViewer.Filtering.Antlr;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Filtering
{
    public class DatabaseExpressionEvaluator
    {
        private readonly IPacketViewModelStore store;
        private SyntaxLexer lexer;
        private CommonTokenStream tokens;
        private SyntaxParser parser;
        private ExpressionVisitor visitor;
        
        public DatabaseExpressionEvaluator(string expression, UniversalGuid playerGuid, IPacketViewModelStore store)
        {
            this.store = store;
            lexer = new SyntaxLexer(new AntlrInputStream(expression));
            tokens = new CommonTokenStream(lexer);
            parser = new SyntaxParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();

            visitor = new ExpressionVisitor(new IsPacketSpecificPlayerProcessor(playerGuid), store);
        }

        public object? Evaluate(PacketViewModel entity)
        {
            lexer.Reset();
            tokens.Reset();
            parser.Reset();
            visitor.SetContext(entity);
            return visitor.Visit(parser.expr());
        }
    }
}