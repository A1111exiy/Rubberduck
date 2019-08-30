﻿using System;
using Rubberduck.VBEditor;
using System.Collections.Generic;
using System.Linq;
using Rubberduck.Parsing.Grammar;

namespace Rubberduck.Parsing.Annotations
{
    /// <summary>
    /// Marks a method that the test engine will execute as a unit test.
    /// </summary>
    public sealed class TestMethodAnnotation : AnnotationBase
    {
        public TestMethodAnnotation()
            : base("TestMethod", AnnotationTarget.Member)
        {
            // FIXME unify handling of quoted arguments to annotations. 
            //// That should probably be part of VBAParserAnnotationFactory's handling of the annotationArguments context
            //var firstParameter = parameters.FirstOrDefault();
            //if ((firstParameter?.StartsWith("\"") ?? false) && firstParameter.EndsWith("\""))
            //{
            //    // Strip surrounding double quotes
            //    firstParameter = firstParameter.Substring(1, firstParameter.Length - 2);
            //}

            //Category = string.IsNullOrWhiteSpace(firstParameter) ? string.Empty : firstParameter;
        }
    }
}
