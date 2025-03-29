using System;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    /// <summary>
    /// Represents the managed project types in Visual Studio.
    /// </summary>
    internal static class ProjectType
    {

        /// <summary>
        ///     A <see cref="string"/> representing the IKVM project type based on the Common Project System (CPS).
        /// </summary>
        public const string Ikvm = "DAEA77DE-8320-43BA-BA7C-EF5C12478AB5";

        /// <summary>
        ///     A <see cref="Guid"/> representing the IKVM project type based on the Common Project System (CPS).
        /// </summary>
        public static readonly Guid IkvmGuid = new(Ikvm);

    }

}
