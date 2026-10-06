package ikvm.visualstudio.jdtls;

import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

import org.osgi.framework.Bundle;
import org.osgi.framework.connect.FrameworkUtilHelper;

import cli.System.Reflection.Assembly;

/**
 * Finds the bundle of a class for {@code FrameworkUtil.getBundle}: the bundle installed from the class's assembly.
 * Every assembly compiled by IKVM shares one class loader, so the class loader cannot tell. Registered as a service
 * in {@code META-INF/services}.
 */
public final class AssemblyFrameworkUtilHelper implements FrameworkUtilHelper {

    private static final Map<Assembly, Bundle> BUNDLES = new ConcurrentHashMap<>();

    /**
     * Records the bundle installed from an assembly.
     */
    static void register(Assembly assembly, Bundle bundle) {
        BUNDLES.put(assembly, bundle);
    }

    /**
     * Initializes a new instance, for the service loader.
     */
    public AssemblyFrameworkUtilHelper() {
    }

    /**
     * Gets the bundle of a class.
     */
    @Override
    public Optional<Bundle> getBundle(Class<?> classFromBundle) {
        cli.System.Type type = ikvm.runtime.Util.getInstanceTypeFromClass(classFromBundle);
        return type == null ? Optional.empty() : Optional.ofNullable(BUNDLES.get(type.get_Assembly()));
    }

}
