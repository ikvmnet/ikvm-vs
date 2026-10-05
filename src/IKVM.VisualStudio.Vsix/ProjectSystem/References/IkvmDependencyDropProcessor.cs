using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;

using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Takes files and directories dropped or pasted on the IKVM Dependencies node or one of its target framework
/// folders: the entry providers that take them add them, used in the target framework of the folder, or in all from
/// the root, and they are saved to the project straight away.
/// </summary>
[Export(typeof(IPasteDataObjectProcessor))]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class IkvmDependencyDropProcessor : IPasteDataObjectProcessor
{

    readonly IkvmDependencyService _service;

    /// <summary>
    /// Initializes a new instance with the service that offers dropped paths to the entry providers and saves them.
    /// </summary>
    [ImportingConstructor]
    public IkvmDependencyDropProcessor(IkvmDependencyService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets whether the node is the IKVM Dependencies node or one of its target framework folders.
    /// </summary>
    static bool IsTarget(IProjectTree node) => node.Flags.Contains(IkvmDependencyTreeFlags.Root) || node.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework);

    /// <summary>
    /// Takes data objects holding files or directories dropped on the IKVM Dependencies node or a target framework folder.
    /// </summary>
    public bool CanHandleDataObject(object dataObject, IProjectTree dropTarget, IProjectTreeProvider currentProvider)
    {
        return IsTarget(dropTarget) && GetPaths(dataObject) is { Length: > 0 };
    }

    /// <summary>
    /// Always links: the dropped files are referenced where they are, not copied into the project.
    /// </summary>
    public DropEffects? QueryDropEffect(object dataObject, int grfKeyState, bool draggedFromThisProject)
    {
        return DropEffects.Link;
    }

    /// <summary>
    /// Adds the dropped paths for the target's target framework, or all from the root, and saves them to the project.
    /// </summary>
    public async Task<IEnumerable<ICopyPasteItem>?> ProcessDataObjectAsync(object dataObject, IProjectTree dropTarget, IProjectTreeProvider currentProvider, DropEffects effect)
    {
        if (IsTarget(dropTarget) == false || GetPaths(dataObject) is not { Length: > 0 } paths)
            return null;

        var targetFramework = dropTarget.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework) ? dropTarget.Caption : null;
        var session = await _service.CreateSessionAsync(targetFramework);
        await _service.SaveAddedAsync(session, session.AddPaths(paths));

        // nothing for the project system to copy: the items are written already
        return Array.Empty<ICopyPasteItem>();
    }

    /// <summary>
    /// Does nothing: the items are saved while processing the data object.
    /// </summary>
    public Task ProcessPostFilterAsync(IEnumerable<ICopyPasteItem> items)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the files and directories in a data object from Explorer, if any.
    /// </summary>
    static string[]? GetPaths(object dataObject)
    {
        try
        {
            var data = dataObject switch
            {
                IDataObject managed => managed,
                ComDataObject com => new DataObject(com),
                _ => null,
            };

            return data?.GetData(DataFormats.FileDrop) as string[];
        }
        catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidCastException)
        {
            return null;
        }
    }

}
