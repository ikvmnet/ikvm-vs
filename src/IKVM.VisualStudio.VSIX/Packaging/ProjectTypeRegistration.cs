using IKVM.VisualStudio.Vsix.Packaging;
using IKVM.VisualStudio.Vsix.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem.VS;

[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.Ikvm,
    displayName: "#21", // "IKVM"
    displayProjectFileExtensions: "#22", // "IKVM Project Files (*.ikvmproj);*.ikvmproj"
    defaultProjectExtension: "ikvmproj",
    language: "Java",
    resourcePackageGuid: IkvmPackage.PackageGuid,
    Capabilities = ProjectTypeCapabilities.Default,
    DisableAsynchronousProjectTreeLoad = true,
    PossibleProjectExtensions = "ikvmproj",
    NewProjectRequireNewFolderVsTemplate = true,
    SupportsCodespaces = true,
    SupportsSolutionChangeWithoutReload = true)]
