using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Reads what a project says about Maven: its repositories.
/// </summary>
static class MavenProjectQueries
{

    const string RepositoriesProperty = "MavenRepositories";

    /// <summary>
    /// Gets the repositories of a project, as <c>MavenRepositories</c> lists them: <c>id=url</c>, separated by
    /// semicolons.
    /// </summary>
    public static async Task<IReadOnlyList<MavenRepository>> GetRepositoriesAsync(ConfiguredProject project)
    {
        try
        {
            var properties = project.Services.ProjectPropertiesProvider!.GetCommonProperties();
            var value = await properties.GetEvaluatedPropertyValueAsync(RepositoriesProperty);

            var result = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Split(new[] { '=' }, 2))
                .Where(i => i.Length == 2 && i[0].Trim().Length > 0 && i[1].Trim().Length > 0)
                .Select(i => new MavenRepository(i[0].Trim(), i[1].Trim()))
                .ToList();

            return result;
        }
        catch (Exception e)
        {
            ActivityLog.TryLogWarning(nameof(MavenProjectQueries), $"Could not read the Maven repositories: {e}");
            return Array.Empty<MavenRepository>();
        }
    }

}
