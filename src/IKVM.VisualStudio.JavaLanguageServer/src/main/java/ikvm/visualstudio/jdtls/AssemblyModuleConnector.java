package ikvm.visualstudio.jdtls;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

import org.osgi.framework.BundleActivator;
import org.osgi.framework.Constants;
import org.osgi.framework.connect.ConnectModule;
import org.osgi.framework.connect.ModuleConnector;

import cli.System.Reflection.Assembly;

/**
 * Connects bundles to assemblies compiled by IKVM: bundles are installed at locations of the form
 * {@code assembly:name}, and the system bundle is the assembly of Equinox.
 */
final class AssemblyModuleConnector implements ModuleConnector {

    /**
     * The prefix of the locations of bundles that are assemblies.
     */
    static final String LOCATION_PREFIX = "assembly:";

    /**
     * The identities given to jars that are not bundles, by assembly name.
     */
    private static final Map<String, SyntheticBundle> SYNTHETIC;

    static {
        Map<String, SyntheticBundle> synthetic = new HashMap<>();
        synthetic.put("log4j", new SyntheticBundle("org.apache.log4j", "1.2.15"));
        SYNTHETIC = Collections.unmodifiableMap(synthetic);
    }

    private final Assembly frameworkAssembly;
    private final Map<String, AssemblyConnectContent> contents = new ConcurrentHashMap<>();

    /**
     * Initializes a new instance.
     */
    AssemblyModuleConnector() {
        this.frameworkAssembly = assemblyOf(org.eclipse.osgi.launch.Equinox.class);
    }

    /**
     * Gets the assembly a class is compiled into.
     */
    static Assembly assemblyOf(Class<?> cls) {
        return ikvm.runtime.Util.getInstanceTypeFromClass(cls).get_Assembly();
    }

    /**
     * Gets the assembly of the framework, which is the system bundle.
     */
    Assembly getFrameworkAssembly() {
        return frameworkAssembly;
    }

    /**
     * Finds the bundles among assemblies: those whose embedded jar has an OSGi manifest, and the jars given an
     * identity.
     */
    List<AssemblyConnectContent> discover(Iterable<Assembly> assemblies) throws IOException {
        List<AssemblyConnectContent> bundles = new ArrayList<>();
        for (Assembly assembly : assemblies) {
            if (assembly.get_IsDynamic() || assembly == frameworkAssembly) {
                continue;
            }

            String name = assembly.GetName().get_Name();
            AssemblyConnectContent content = AssemblyConnectContent.read(assembly, SYNTHETIC.get(name));
            if (content != null && content.isBundle()) {
                contents.put(name, content);
                bundles.add(content);
            }
        }

        return bundles;
    }

    /**
     * Gets the location to install the bundle of an assembly at.
     */
    static String locationOf(AssemblyConnectContent content) {
        return LOCATION_PREFIX + content.getAssembly().GetName().get_Name();
    }

    /**
     * Initializes the connector, which needs no storage or configuration.
     */
    @Override
    public void initialize(File storage, Map<String, String> configuration) {
    }

    /**
     * Connects a bundle location to its assembly.
     */
    @Override
    public Optional<ConnectModule> connect(String location) throws org.osgi.framework.BundleException {
        try {
            if (Constants.SYSTEM_BUNDLE_LOCATION.equals(location)) {
                return Optional.of(new AssemblyConnectModule(AssemblyConnectContent.read(frameworkAssembly, null)));
            }

            if (!location.startsWith(LOCATION_PREFIX)) {
                return Optional.empty();
            }

            AssemblyConnectContent content = contents.get(location.substring(LOCATION_PREFIX.length()));
            return content == null ? Optional.empty() : Optional.of(new AssemblyConnectModule(content));
        } catch (IOException e) {
            throw new org.osgi.framework.BundleException("Failed to read the assembly of " + location + ".", e);
        }
    }

    /**
     * Gets an activator for the framework, which needs none.
     */
    @Override
    public Optional<BundleActivator> newBundleActivator() {
        return Optional.empty();
    }

}
