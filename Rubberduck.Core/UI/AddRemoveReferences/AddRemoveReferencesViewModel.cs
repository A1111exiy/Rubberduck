﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Rubberduck.AddRemoveReferences;
using Rubberduck.VBEditor.SafeComWrappers;

namespace Rubberduck.UI.AddRemoveReferences
{
    public class AddRemoveReferencesViewModel : ViewModelBase
    {
        public AddRemoveReferencesViewModel(IReadOnlyList<ReferenceModel> model)
        {
            ComLibraries = model.Where(item => item.Type == ReferenceKind.TypeLibrary);
            VbaProjects = model.Where(item => item.Type == ReferenceKind.Project);
        }

        /// <summary>
        /// Prompts user for a .tlb, .dll, or .ocx file, and attempts to append it to <see cref="ProjectReferences"/>.
        /// </summary>
        public ICommand BrowseCommand { get; }

        /// <summary>
        /// Applies all changes to project references.
        /// </summary>
        public ICommand ApplyCommand { get; }

        /// <summary>
        /// Moves the <see cref="SelectedReference"/> up on the 'Priority' tab.
        /// </summary>
        public ICommand MoveUpCommand { get; }

        /// <summary>
        /// Moves the <see cref="SelectedReference"/> down on the 'Priority' tab.
        /// </summary>
        public ICommand MoveDownCommand { get; }

        public IEnumerable<ReferenceModel> ComLibraries { get; }
        public IEnumerable<ReferenceModel> VbaProjects { get; }

        public ReferenceModel SelectedLibrary { get; set; }
        public ReferenceModel SelectedProject { get; set; }
        public ReferenceModel SelectedReference { get; set; }

        /// <summary>
        /// Gets all selected COM libraries and VBA projects.
        /// </summary>
        public ICollection<ReferenceModel> ProjectReferences { get; }
    }
}
