﻿using Rubberduck.Parsing;
using Rubberduck.Parsing.Grammar;
using System;
using System.Linq;

namespace Rubberduck.Inspections.Concrete.UnreachableCaseInspection
{
    public interface IParseTreeValue
    {
        string ValueText { get; }
        string TypeName { get; }
        bool ParsesToConstantValue { get; set; }
    }

    public class ParseTreeValue : IParseTreeValue
    {
        private string _valueText;
        private string _declaredType;
        private string _derivedType;

        public ParseTreeValue(string value, string declaredType = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException("null 'value' argument passed to UCIValue");
            }

            ParsesToConstantValue = IsStringConstant(value);
            _declaredType = ParsesToConstantValue && (declaredType is null) ? Tokens.String : declaredType;
            _derivedType = DeriveTypeName(value, out bool derivedFromTypeHint);
            if (derivedFromTypeHint)
            {
                _declaredType = _derivedType;
                _valueText = RemoveTypeHintChar(value);
            }
            else
            {
                _valueText = value.Replace("\"", "");
            }
            var conformToTypeName = _declaredType ?? _derivedType;
            ConformValueTextToType(conformToTypeName);
        }

        public string TypeName => _declaredType ?? _derivedType ?? string.Empty;

        public string ValueText => _valueText;

        public bool ParsesToConstantValue { set; get; }

        public override string ToString() => _valueText;

        private static string RemoveTypeHintChar(string inputValue)
        {
            if (inputValue == string.Empty) { return inputValue; }

            var result = inputValue;
            var endingCharacter = inputValue.Last().ToString();
            if (SymbolList.TypeHintToTypeName.ContainsKey(endingCharacter))
            {
                return inputValue.Replace(inputValue.Last().ToString(), "");
            }
            return result;
        }

        private static string DeriveTypeName(string inputString, out bool derivedFromTypeHint)
        {
            derivedFromTypeHint = false;
            var result = string.Empty;
            if (inputString.Length == 0)
            {
                return result;
            }

            if (SymbolList.TypeHintToTypeName.TryGetValue(inputString.Last().ToString(), out string hintResult))
            {
                derivedFromTypeHint = true;
                result =  hintResult;
            }
            else if (IsStringConstant(inputString))
            {
                result = Tokens.String;
            }
            else if (inputString.Contains("."))
            {
                if (double.TryParse(inputString, out _))
                {
                    result = Tokens.Double;
                }
                else if (decimal.TryParse(inputString, out _))
                {
                    result = Tokens.Currency;
                }
            }
            else if (inputString.Count(ch => ch.Equals('E')) == 1)
            {
                if (double.TryParse(inputString, out _))
                {
                    result = Tokens.Double;
                }
            }
            else if (inputString.Equals(Tokens.True) || inputString.Equals(Tokens.False))
            {
                result = Tokens.Boolean;
            }
            else if (long.TryParse(inputString, out _))
            {
                result = Tokens.Long;
            }
            return result;
        }

        private void ConformValueTextToType(string conformTypeName)
        {
            if (_valueText.Equals(double.NaN.ToString()) && !conformTypeName.Equals(Tokens.String))
            {
                return;
            }

            if (conformTypeName.Equals(Tokens.Long) || conformTypeName.Equals(Tokens.Integer) || conformTypeName.Equals(Tokens.Byte))
            {
                if (ParseTreeValueConverter.TryConvertValue(_valueText, out long newVal))
                {
                    _valueText = newVal.ToString();
                    ParsesToConstantValue = true;
                }
            }
            else if (conformTypeName.Equals(Tokens.Double) || conformTypeName.Equals(Tokens.Single))
            {
                if (ParseTreeValueConverter.TryConvertValue(_valueText, out double newVal))
                {
                    _valueText = newVal.ToString();
                    ParsesToConstantValue = true;
                }
            }
            else if (conformTypeName.Equals(Tokens.Boolean))
            {
                if (ParseTreeValueConverter.TryConvertValue(_valueText, out bool newVal))
                {
                    _valueText = newVal.ToString();
                    ParsesToConstantValue = true;
                }
            }
            else if (conformTypeName.Equals(Tokens.String))
            {
                ParsesToConstantValue = true;
            }
            else if (conformTypeName.Equals(Tokens.Currency))
            {
                if (ParseTreeValueConverter.TryConvertValue(_valueText, out decimal newVal))
                {
                    _valueText = newVal.ToString();
                    ParsesToConstantValue = true;
                }
            }
        }

        private static bool IsStringConstant(string input) => input.StartsWith("\"") && input.EndsWith("\"");
    }
}
