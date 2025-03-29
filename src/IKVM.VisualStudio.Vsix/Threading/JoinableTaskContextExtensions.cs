using System.Runtime.InteropServices;

using Microsoft;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace IKVM.VisualStudio.Vsix.Threading
{

    public static class JoinableTaskContextExtensions
    {

        public static void VerifyIsOnMainThread(this JoinableTaskContext joinableTaskContext)
        {
            Requires.NotNull(joinableTaskContext);

            if (!joinableTaskContext.IsOnMainThread)
            {
                throw new COMException("This method must be called on the UI thread.", HResult.WrongThread);
            }

        }

    }

}
