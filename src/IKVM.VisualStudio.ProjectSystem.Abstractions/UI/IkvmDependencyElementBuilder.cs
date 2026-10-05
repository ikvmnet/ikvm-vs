using System;
using System.Collections.Generic;
using System.Linq;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Builds the element an entry is saved as.
/// </summary>
public sealed class IkvmDependencyElementBuilder
{

    readonly HashSet<string> _written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly List<IkvmDependencyMetadata> _metadata = new List<IkvmDependencyMetadata>();

    /// <summary>
    /// Initializes a builder for an entry, starting from the include of its original element, if any.
    /// </summary>
    /// <param name="original">The element as read from the project, or <c>null</c> for a new entry.</param>
    /// <param name="keys">The keys of the target frameworks the entry is used in.</param>
    internal IkvmDependencyElementBuilder(IkvmDependencyElement? original, IReadOnlyList<string> keys)
    {
        Original = original;
        Keys = keys;
        Include = original?.Include ?? "";
    }

    /// <summary>
    /// Gets the element as read from the project, for existing entries.
    /// </summary>
    public IkvmDependencyElement? Original { get; }

    /// <summary>
    /// Gets the keys of the target frameworks the entry is used in: each target framework, or an empty one for a
    /// project with a single one.
    /// </summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>
    /// Gets or sets the include. Defaults to that of the original element.
    /// </summary>
    public string Include { get; set; }

    /// <summary>
    /// Writes a metadata value, given for each target framework the entry is used in: for every target framework when
    /// it is the same for each, else for each. An empty value is not written.
    /// </summary>
    public void SetMetadata(string name, Func<string, string> valueOf)
    {
        _written.Add(name);
        _metadata.RemoveAll(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

        var values = Keys.Select(i => (Key: i, Value: valueOf(i) ?? "")).ToList();

        // the same everywhere: one value for all target frameworks
        if (values.Select(i => i.Value).Distinct().Count() <= 1)
        {
            var value = values.Select(i => i.Value).FirstOrDefault() ?? "";
            if (value.Length > 0)
                _metadata.Add(new IkvmDependencyMetadata(name, value, Array.Empty<string>()));

            return;
        }

        foreach (var group in values.Where(i => i.Value.Length > 0).GroupBy(i => i.Value))
            _metadata.Add(new IkvmDependencyMetadata(name, group.Key, group.Select(i => i.Key).ToList()));
    }

    /// <summary>
    /// Writes a metadata value for every target framework. An empty value is not written.
    /// </summary>
    public void SetMetadata(string name, string value) => SetMetadata(name, _ => value);

    /// <summary>
    /// Builds the editable element of the given item type, limited to the given target frameworks, or empty for all.
    /// Metadata of the original element that was not written is kept, before the metadata written.
    /// </summary>
    internal IkvmDependencyElement Build(string itemType, IReadOnlyList<string> targetFrameworks)
    {
        // metadata the entry does not write is kept
        var metadata = new List<IkvmDependencyMetadata>();
        if (Original != null)
            metadata.AddRange(Original.Metadata.Where(i => _written.Contains(i.Name) == false));

        metadata.AddRange(_metadata);
        return new IkvmDependencyElement(itemType, Include, targetFrameworks, metadata, true);
    }

}
