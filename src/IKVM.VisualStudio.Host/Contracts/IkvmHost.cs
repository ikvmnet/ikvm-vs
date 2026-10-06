using System.Collections.Generic;

using Microsoft.ServiceHub.Framework;

namespace IKVM.VisualStudio.Host.Contracts;

/// <summary>
/// The IKVM host: the process ServiceHub runs the services of the extension that need IKVM in, apart from Visual
/// Studio, and from the hosts of other extensions, since IKVM keeps process-wide state another copy of it would clash
/// with. Its services are requested with <see cref="ActivationOptions"/>, which names its host group.
/// </summary>
public static class IkvmHost
{

    /// <summary>
    /// The ServiceHub host group of the IKVM host.
    /// </summary>
    public const string HostGroup = "IKVM";

    /// <summary>
    /// The activation argument ServiceHub reads the host group of a request from.
    /// </summary>
    public const string HostGroupActivationArgument = "__servicehub__ServiceHubHostGroup";

    /// <summary>
    /// The options that request a service in the IKVM host.
    /// </summary>
    public static ServiceActivationOptions ActivationOptions => new ServiceActivationOptions()
    {
        ActivationArguments = new Dictionary<string, string>() { [HostGroupActivationArgument] = HostGroup },
    };

}
