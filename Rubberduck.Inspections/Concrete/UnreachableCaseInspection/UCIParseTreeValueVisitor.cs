﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.Inspections.Concrete.UnreachableCaseInspection
{
    public interface IUCIParseTreeValueVisitor : IParseTreeVisitor<IUCIValueResults>
    {
        event EventHandler<ValueResultEventArgs> OnValueResultCreated;
    }

    public class UCIParseTreeValueVisitor : IUCIParseTreeValueVisitor
    {
        private IUCIValueResults _contextValues;
        private RubberduckParserState _state;
        private IUCIValueFactory _inspValueFactory;

        public UCIParseTreeValueVisitor(RubberduckParserState state, IUCIValueFactory valueFactory)
        {
            _state = state;
            _inspValueFactory = valueFactory;
            Calculator = new UCIValueExpressionEvaluator(valueFactory);
            _contextValues = new UCIValueResults();
            OnValueResultCreated += _contextValues.OnNewValueResult;
        }

        public event EventHandler<ValueResultEventArgs> OnValueResultCreated;

        public IUCIValueExpressionEvaluator Calculator { set; get; }

        public virtual IUCIValueResults Visit(IParseTree tree)
        {
            if (tree is ParserRuleContext context)
            {
                Visit(context);
            }
            return _contextValues;
        }

        public virtual IUCIValueResults VisitChildren(IRuleNode node)
        {
            if (node is ParserRuleContext context)
            {
                foreach (var child in context.children)
                {
                    Visit(child);
                }
            }
            return _contextValues;
        }

        public virtual IUCIValueResults VisitTerminal(ITerminalNode node)
        {
            return _contextValues;
        }

        public virtual IUCIValueResults VisitErrorNode(IErrorNode node)
        {
            return _contextValues;
        }

        internal static bool IsMathContext<T>(T context)
        {
            return IsBinaryMathContext(context) || IsUnaryMathContext(context);
        }

        internal static bool IsLogicalContext<T>(T context)
        {
            return IsBinaryLogicalContext(context) || IsUnaryLogicalContext(context);
        }

        private void StoreVisitResult(ParserRuleContext context, IUCIValue inspValue)
        {
            OnValueResultCreated(this, new ValueResultEventArgs(context, inspValue));
        }

        private bool ContextHasResult(ParserRuleContext context)
        {
            return _contextValues.Contains(context);
        }

        private void Visit(ParserRuleContext parserRuleContext)
        {
            if (IsUnaryResultContext(parserRuleContext))
            {
                VisitUnaryResultContext(parserRuleContext);
            }
            else if (parserRuleContext is VBAParser.LExprContext lExpr)
            {
                Visit(lExpr);
            }
            else if (parserRuleContext is VBAParser.LiteralExprContext litExpr)
            {
                Visit(litExpr);
            }
            else if (parserRuleContext is VBAParser.CaseClauseContext
                || parserRuleContext is VBAParser.RangeClauseContext)
            {
                VisitImpl(parserRuleContext);
                StoreVisitResult(parserRuleContext, _inspValueFactory.Create(parserRuleContext.GetText()));
            }
            else if (IsBinaryMathContext(parserRuleContext) || IsBinaryLogicalContext(parserRuleContext))
            {
                VisitBinaryOpEvaluationContext(parserRuleContext);
            }
            else if  (IsUnaryLogicalContext(parserRuleContext) || IsUnaryMathContext(parserRuleContext))
            {
                VisitUnaryOpEvaluationContext(parserRuleContext);
            }
        }

        private void Visit(VBAParser.LExprContext context)
        {
            if (ContextHasResult(context))
            {
                return;
            }

            IUCIValue newResult = null;
            if (TryGetLExprValue(context, out string lexprValue, out string declaredType))
            {
                newResult = _inspValueFactory.Create(lexprValue, declaredType);
            }
            else
            {
                var smplNameExprTypeName = string.Empty;
                var smplName = context.GetDescendent<VBAParser.SimpleNameExprContext>();
                if (TryGetIdentifierReferenceForContext(smplName, out IdentifierReference idRef))
                {
                    var declarationTypeName = GetBaseTypeForDeclaration(idRef.Declaration);
                    newResult = _inspValueFactory.Create(context.GetText(), declarationTypeName);
                }
            }

            if (newResult != null)
            {
                StoreVisitResult(context, newResult);
            }
        }

        private void Visit(VBAParser.LiteralExprContext context)
        {
            if (!ContextHasResult(context))
            {
                var nResult = _inspValueFactory.Create(context.GetText());
                StoreVisitResult(context, nResult);
            }
        }

        private void VisitBinaryOpEvaluationContext(ParserRuleContext context)
        {
            VisitImpl(context);

            var operands = RetrieveRelevantOpData(context, out string opSymbol);
            if (operands.Count != 2 || operands.All(opr => opr is null))
            {
                return;
            }

            var nResult = Calculator.Evaluate(operands[0], operands[1], opSymbol);

            StoreVisitResult(context, nResult);
        }

        private void VisitUnaryOpEvaluationContext(ParserRuleContext context)
        {
            VisitImpl(context);
            var operands = RetrieveRelevantOpData(context, out string opSymbol);
            if (operands.Count != 1 || operands.All(opr => opr is null))
            {
                return;
            }

            var result = Calculator.Evaluate(operands[0], opSymbol);
            StoreVisitResult(context, result);
        }

        private List<IUCIValue> RetrieveRelevantOpData(ParserRuleContext context, out string opSymbol)
        {
            opSymbol = string.Empty;
            var values = new List<IUCIValue>();
            var contextsOfInterest = NonWhitespaceChildren(context);
            for (var idx = 0; idx < contextsOfInterest.Count(); idx++)
            {
                if (contextsOfInterest.ElementAt(idx) is ParserRuleContext ctxt)
                {
                    values.Add(_contextValues.GetValue(ctxt));
                }
                else
                {
                    opSymbol = contextsOfInterest.ElementAt(idx).GetText();
                }
            }
            return values;
        }

        private void VisitUnaryResultContext(ParserRuleContext parserRuleContext)
        {
            VisitImpl(parserRuleContext);

            var contextsOfInterest = ParserRuleContextChildren(parserRuleContext);
            for (var idx = 0; idx < contextsOfInterest.Count(); idx++)
            {
                var ctxt = contextsOfInterest.ElementAt(idx);
                if (_contextValues.Contains(ctxt))
                {
                    var value = _contextValues.GetValue(ctxt);
                    StoreVisitResult(parserRuleContext, value);
                }
            }
        }

        private void VisitImpl(ParserRuleContext context)
        {
            if (!ContextHasResult(context))
            {
                foreach (var ctxt in ParserRuleContextChildren(context))
                {
                    Visit(ctxt);
                }
            }
        }

        private static IEnumerable<ParserRuleContext> ParserRuleContextChildren(IParseTree ptParent)
        {
            return ((ParserRuleContext)ptParent).children.Where(ch => !(ch is VBAParser.WhiteSpaceContext) && ch is ParserRuleContext).Select(item => (ParserRuleContext)item);
        }

        private static IEnumerable<IParseTree> NonWhitespaceChildren(ParserRuleContext ptParent)
        {
            return ptParent.children.Where(ch => !(ch is VBAParser.WhiteSpaceContext));
        }

        private bool TryGetLExprValue(VBAParser.LExprContext lExprContext, out string expressionValue, out string declaredTypeName)
        {
            expressionValue = string.Empty;
            declaredTypeName = string.Empty;
            var isMemberAccess = lExprContext.TryGetChildContext(out VBAParser.MemberAccessExprContext memberAccess);
            var isSimpleName = lExprContext.TryGetChildContext(out VBAParser.SimpleNameExprContext smplName);

            if (!(isMemberAccess || isSimpleName))
            {
                return false;
            }

            if (isMemberAccess)
            {
                var member = memberAccess.GetChild<VBAParser.UnrestrictedIdentifierContext>();

                if (TryGetIdentifierReferenceForContext(member, out IdentifierReference idRef)
                    && idRef.Declaration.DeclarationType.HasFlag(DeclarationType.EnumerationMember)
                    && idRef.Declaration.Context is VBAParser.EnumerationStmt_ConstantContext)
                {
                    var dec = idRef.Declaration;
                    var theCtxt = dec.Context;
                    expressionValue = GetConstantDeclarationValueToken(dec);
                    declaredTypeName = dec.AsTypeIsBaseType ? dec.AsTypeName : dec.AsTypeDeclaration.AsTypeName;
                    return true;
                }
                return false;
            }

            if (isSimpleName)
            {
                if (TryGetIdentifierReferenceForContext(smplName, out IdentifierReference rangeClauseIdentifierReference))
                {
                    var declaration = rangeClauseIdentifierReference.Declaration;
                    if (declaration.DeclarationType.HasFlag(DeclarationType.Constant)
                        || declaration.DeclarationType.HasFlag(DeclarationType.EnumerationMember))
                    {
                        expressionValue = GetConstantDeclarationValueToken(declaration);
                        declaredTypeName = declaration.AsTypeName;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryGetIdentifierReferenceForContext(ParserRuleContext context, out IdentifierReference idRef)
        {
            idRef = null;
            var identifierReferences = (_state.DeclarationFinder.MatchName(context.GetText()).Select(dec => dec.References)).SelectMany(rf => rf);
            if (identifierReferences.Any())
            {
                idRef = identifierReferences.First(rf => rf.Context == context);
                return true;
            }
            return false;
        }

        private string GetConstantDeclarationValueToken(Declaration valueDeclaration)
        {
            var contextsOfInterest = new List<ParserRuleContext>();
            var contexts = valueDeclaration.Context.children.ToList();
            var eqIndex = contexts.FindIndex(ch => ch.GetText().Equals(CompareTokens.EQ));
            for (int idx = eqIndex + 1; idx < contexts.Count(); idx++)
            {
                var childCtxt = contexts[idx];
                if (!(childCtxt is VBAParser.WhiteSpaceContext))
                {
                    contextsOfInterest.Add((ParserRuleContext)childCtxt);
                }
            }

            foreach (var child in contextsOfInterest)
            {
                Visit(child);
                if (_contextValues.TryGetValue(child, out IUCIValue value))
                {
                    return value.ValueText;
                }
            }
            return string.Empty;
        }

        private static string GetBaseTypeForDeclaration(Declaration declaration)
        {
            if (!declaration.AsTypeIsBaseType)
            {
                return GetBaseTypeForDeclaration(declaration.AsTypeDeclaration);
            }
            return declaration.AsTypeName;
        }

        private static bool IsBinaryMathContext<T>(T context)
        {
            return context is VBAParser.MultOpContext   //MultOpContext includes both * and /
                || context is VBAParser.AddOpContext    //AddOpContet includes both + and -
                || context is VBAParser.PowOpContext
                || context is VBAParser.ModOpContext;
        }

        private static bool IsUnaryMathContext<T>(T context)
        {
            return context is VBAParser.UnaryMinusOpContext;
        }

        private static bool IsUnaryResultContext<T>(T context)
        {
            return context is VBAParser.SelectStartValueContext
                || context is VBAParser.SelectEndValueContext
                || context is VBAParser.ParenthesizedExprContext
                || context is VBAParser.SelectExpressionContext;
        }

        private static bool IsBinaryLogicalContext<T>(T context)
        {
            return context is VBAParser.RelationalOpContext
                || context is VBAParser.LogicalXorOpContext
                || context is VBAParser.LogicalAndOpContext
                || context is VBAParser.LogicalOrOpContext
                || context is VBAParser.LogicalEqvOpContext;
        }

        private static bool IsUnaryLogicalContext<T>(T context)
        {
            return context is VBAParser.LogicalNotOpContext;
        }
    }
    
    public class ValueResultEventArgs : EventArgs
    {
        public ValueResultEventArgs(ParserRuleContext context, IUCIValue value)
        {
            Context = context;
            Value = value;
        }

        public ParserRuleContext Context { set; get; }
        public IUCIValue Value { set; get; }
    }
}
