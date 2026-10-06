package ikvm.visualstudio.jdtls;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import org.eclipse.osgi.launch.EquinoxFactory;
import org.osgi.framework.Bundle;
import org.osgi.framework.BundleContext;
import org.osgi.framework.Constants;
import org.osgi.framework.launch.Framework;
import org.osgi.framework.wiring.BundleWire;
import org.osgi.framework.wiring.BundleWiring;
import org.osgi.framework.wiring.FrameworkWiring;

import cli.System.IO.BinaryReader;
import cli.System.Reflection.Assembly;
import cli.System.Reflection.AssemblyName;
import cli.System.Runtime.Loader.AssemblyLoadContext;

/**
 * Runs Equinox in OSGi Connect mode, with every bundle of JDT LS an assembly compiled by IKVM, once per process.
 */
final class EclipseFramework {

    private static EclipseFramework instance;
    private static WorkArea area;

    /**
     * Gets the framework of the process, starting it the first time: it cannot start again once stopped, as Eclipse
     * keeps the job manager in static fields, so it runs for the life of the process.
     */
    static synchronized EclipseFramework get() throws Exception {
        if (instance == null) {
            instance = start();
        }

        return instance;
    }

    /**
     * Starts the framework.
     */
    private static EclipseFramework start() throws Exception {
        File installDirectory = new File(AssemblyModuleConnector.assemblyOf(EclipseFramework.class).get_Location()).getParentFile();
        if (area == null) {
            String localAppData = System.getenv("LOCALAPPDATA");
            area = WorkArea.acquire(new File(localAppData != null ? localAppData : System.getProperty("user.home"), "IKVM" + File.separator + "JavaLanguageServer"));
        }

        // Equinox's framework log closes the standard error stream, which the host process shares
        PrintStream log = new PrintStream(new FileOutputStream(area.resolve("ikvm.log"), true), true, "UTF-8") {

            @Override
            public void println(String line) {
                super.println(java.time.LocalTime.now() + " " + line);
            }

        };
        try {
            long started = System.currentTimeMillis();
            List<Assembly> assemblies = loadReferencedAssemblies(log);
            log.println("loaded " + assemblies.size() + " assemblies in " + (System.currentTimeMillis() - started) + "ms");

            Map<String, String> configuration = new HashMap<>();
            configuration.put(Constants.FRAMEWORK_STORAGE, area.resolve("storage").getPath());
            configuration.put(Constants.FRAMEWORK_STORAGE_CLEAN, Constants.FRAMEWORK_STORAGE_CLEAN_ONFIRSTINIT);
            configuration.put("osgi.instance.area", area.resolve("workspace").toURI().toString());
            configuration.put("osgi.install.area", installDirectory.toURI().toString());
            configuration.put("eclipse.application", "org.eclipse.jdt.ls.core.id1");
            configuration.put("eclipse.product", "org.eclipse.jdt.ls.core.product");
            configuration.put("eclipse.application.launchDefault", "false");
            // the thread that keeps the framework active must not keep the host process alive
            configuration.put("osgi.framework.activeThreadType", "daemon");

            AssemblyModuleConnector connector = new AssemblyModuleConnector();
            Framework framework = new EquinoxFactory().newFramework(configuration, connector);
            framework.init();
            log.println("framework initialized");
            AssemblyFrameworkUtilHelper.register(connector.getFrameworkAssembly(), framework);

            BundleContext context = framework.getBundleContext();
            List<Bundle> installed = new ArrayList<>();
            for (AssemblyConnectContent content : connector.discover(assemblies)) {
                Bundle bundle = context.installBundle(AssemblyModuleConnector.locationOf(content));
                AssemblyFrameworkUtilHelper.register(content.getAssembly(), bundle);
                installed.add(bundle);
            }

            log.println("installed " + installed.size() + " bundles");
            framework.start();
            framework.adapt(FrameworkWiring.class).resolveBundles(null);
            for (Bundle bundle : installed) {
                if (bundle.getState() == Bundle.INSTALLED) {
                    log.println("not resolved: " + bundle.getSymbolicName());
                }
            }

            // classes come from the class loader IKVM shares between assemblies, so the framework never sees a bundle
            // load a class, and lazy activation never happens: every bundle is started, after those it depends on
            Set<Long> visited = new HashSet<>();
            for (Bundle bundle : installed) {
                startBundle(bundle, visited, log);
            }

            log.println("framework started in " + (System.currentTimeMillis() - started) + "ms with " + installed.size() + " bundles");

            IkvmRuntime.install(new File(installDirectory, "jdk" + File.separator + "rt.jar"), area, log);
            return new EclipseFramework(framework, log);
        } catch (Exception | Error e) {
            e.printStackTrace(log);
            log.close();
            throw e;
        }
    }

    /**
     * Loads the assemblies this one was compiled against, into the load context of this one: IKVM's shared class
     * loader only finds classes in loaded assemblies, and the bundles are found among them. The importer records them
     * in the assembly's {@code ikvm.exports} resource: a count, then each assembly's name and the hashes of its types.
     */
    private static List<Assembly> loadReferencedAssemblies(PrintStream log) {
        Assembly self = AssemblyModuleConnector.assemblyOf(EclipseFramework.class);
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(self);
        List<Assembly> assemblies = new ArrayList<>();

        BinaryReader reader = new BinaryReader(self.GetManifestResourceStream("ikvm.exports"));
        try {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                String name = reader.ReadString();
                for (int types = reader.ReadInt32(); types > 0; types--) {
                    reader.ReadInt32();
                }

                try {
                    assemblies.add(context.LoadFromAssemblyName(new AssemblyName(name)));
                } catch (Throwable e) {
                    log.println("failed to load " + name + ": " + e);
                }
            }
        } finally {
            reader.Dispose();
        }

        return assemblies;
    }

    /**
     * Starts a bundle, after the bundles it is wired to.
     */
    private static void startBundle(Bundle bundle, Set<Long> visited, PrintStream log) {
        if (!visited.add(bundle.getBundleId()) || bundle.getBundleId() == 0) {
            return;
        }

        BundleWiring wiring = bundle.adapt(BundleWiring.class);
        if (wiring != null) {
            for (BundleWire wire : wiring.getRequiredWires(null)) {
                startBundle(wire.getProvider().getBundle(), visited, log);
            }
        }

        if (bundle.getHeaders().get(Constants.FRAGMENT_HOST) != null) {
            return;
        }

        try {
            log.println("starting " + bundle.getSymbolicName());
            bundle.start();
        } catch (Exception e) {
            log.println("failed to start " + bundle.getSymbolicName() + ":");
            e.printStackTrace(log);
        }
    }

    private final Framework framework;
    private final PrintStream log;

    /**
     * Initializes a new instance.
     */
    private EclipseFramework(Framework framework, PrintStream log) {
        this.framework = framework;
        this.log = log;
    }

    /**
     * Gets the framework.
     */
    Framework getFramework() {
        return framework;
    }

    /**
     * Gets the log of the language server's own startup and connections.
     */
    PrintStream getLog() {
        return log;
    }

}
