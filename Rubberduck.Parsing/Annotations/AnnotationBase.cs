﻿using System;
using System.Linq;
using Rubberduck.Parsing.Grammar;
using Rubberduck.VBEditor;

namespace Rubberduck.Parsing.Annotations
{
    public abstract class AnnotationBase : IAnnotation
    {
        public bool AllowMultiple { get; }
        public string Name { get; }
        public AnnotationTarget Target { get; }

        public AnnotationBase(string name, AnnotationTarget target, bool allowMultiple = false)
        {
            Name = name;
            Target = target;
            AllowMultiple = allowMultiple;
        }
    }
}
