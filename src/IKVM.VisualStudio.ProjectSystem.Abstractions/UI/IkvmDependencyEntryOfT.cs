using System;
using System.Collections.Generic;
using System.Linq;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// An entry whose values are kept by target framework, in a <typeparamref name="TValues"/> each. The entry shows and
/// edits them for the target frameworks being edited: a value is shown when they agree, and setting one sets it for
/// all of them.
/// </summary>
/// <typeparam name="TValues">The values the entry has for one target framework.</typeparam>
public abstract class IkvmDependencyEntry<TValues> : IkvmDependencyEntry
    where TValues : class, new()
{

    readonly Dictionary<string, TValues> _values = new Dictionary<string, TValues>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes an entry for an item already in the project, or for a new one when <paramref name="original"/> is
    /// <c>null</c>, with new values for each target framework.
    /// </summary>
    protected IkvmDependencyEntry(IkvmDependencyEntryContext context, string itemType, IkvmDependencyElement? original) :
        base(context, itemType, original)
    {
        foreach (var key in Keys)
            _values[key] = new TValues();
    }

    /// <summary>
    /// Gets the values for the target framework with the given key.
    /// </summary>
    protected TValues GetValues(string key) => _values[key];

    /// <summary>
    /// Gets the values for each target framework being edited.
    /// </summary>
    protected IEnumerable<TValues> ShownValues => ShownKeys.Select(i => _values[i]);

    /// <summary>
    /// Gets a value for the target frameworks being edited: theirs when they agree, else an empty string.
    /// </summary>
    protected string Common(Func<TValues, string> get)
    {
        var values = ShownValues.Select(get).Distinct().ToList();
        return values.Count == 1 ? values[0] : "";
    }

    /// <summary>
    /// Gets a value for the target frameworks being edited: theirs when they agree, else <c>null</c>.
    /// </summary>
    protected T? Common<T>(Func<TValues, T> get) where T : struct
    {
        var values = ShownValues.Select(get).Distinct().ToList();
        return values.Count == 1 ? values[0] : null;
    }

    /// <summary>
    /// Gets whether a value differs between the target frameworks being edited.
    /// </summary>
    protected bool Varies<T>(Func<TValues, T> get)
    {
        return ShownValues.Select(get).Distinct().Count() > 1;
    }

    /// <summary>
    /// Lists a value that differs between the target frameworks being edited, one line each, such as
    /// <c>net472: a</c>; <c>null</c> when it does not differ.
    /// </summary>
    protected string? Variations(Func<TValues, string> get)
    {
        if (Varies(get) == false)
            return null;

        return string.Join("\n", ShownKeys.Select(i => (Key: i, Value: get(_values[i]))).Select(i => $"{i.Key}: {(i.Value.Length > 0 ? i.Value : "not set")}"));
    }

    /// <summary>
    /// Gets the state a field shows: not valid, differing between the target frameworks being edited, or neither.
    /// </summary>
    protected static FieldState GetFieldState(bool invalid, bool varies) => invalid ? FieldState.Error : varies ? FieldState.Varies : FieldState.Normal;

    /// <summary>
    /// Gets whether the entry has errors, and a value is not valid for a target framework being edited that the entry
    /// is used in.
    /// </summary>
    protected bool IsInvalid(Func<TValues, bool> isValid) => HasErrors && ShownKeys.Any(i => AppliesTo(i) && isValid(_values[i]) == false);

    /// <summary>
    /// Changes the values of every target framework being edited.
    /// </summary>
    protected void Apply(Action<TValues> change)
    {
        foreach (var values in ShownValues)
            change(values);

        Refresh();
    }

    /// <summary>
    /// Writes a metadata value: for every target framework when it is the same for each the entry is used in, else
    /// for each. An empty value is not written.
    /// </summary>
    protected void SetMetadata(IkvmDependencyElementBuilder builder, string name, Func<TValues, string> get)
    {
        builder.SetMetadata(name, key => get(_values[key]));
    }

    /// <summary>
    /// Adds an error naming each target framework the entry is used in whose values are not valid.
    /// </summary>
    protected void AddError(IkvmDependencyValidation validation, Func<TValues, bool> isValid, string message)
    {
        validation.AddError(UsedKeys.Where(i => isValid(_values[i]) == false), message);
    }

}
