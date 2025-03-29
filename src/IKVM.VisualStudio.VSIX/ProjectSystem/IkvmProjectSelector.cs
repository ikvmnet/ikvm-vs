using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace IKVM.VisualStudio.VSIX.ProjectSystem
{

    [Guid("D6819451-BBEA-4DF2-BC9F-1AD59B0E8DB3")]
    [ProvideObject(typeof(IkvmProjectSelector))]
    internal class IkvmProjectSelector : IVsProjectSelector, IDisposable
    {

        readonly JoinableTaskContext _context;
        readonly IProjectThreadingService _threadingService;

        IVsRegisterProjectSelector? _projectSelector;
        uint _cookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="threadingService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ImportingConstructor]
        public IkvmProjectSelector(JoinableTaskContext context, IProjectThreadingService threadingService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _threadingService = threadingService ?? throw new ArgumentNullException(nameof(threadingService));
        }

        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            Assumes.Null(_projectSelector);

            _projectSelector = await asyncServiceProvider.GetServiceAsync<SVsRegisterProjectTypes, IVsRegisterProjectSelector>();
            Guid selectorGuid = GetType().GUID;
            await _context.Factory.SwitchToMainThreadAsync();
            _projectSelector.RegisterProjectSelector(ref selectorGuid, this, out _cookie);
        }

        public void GetProjectFactoryGuid(Guid guidProjectType, string pszFilename, out Guid guidProjectFactory)
        {
            guidProjectFactory = ProjectType.IkvmGuid;
        }

        public void Dispose()
        {
            _threadingService.VerifyOnUIThread();
            if (_cookie != VSConstants.VSCOOKIE_NIL)
                _projectSelector?.UnregisterProjectSelector(_cookie);
        }

    }

}
