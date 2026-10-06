package ikvm.visualstudio.jdtls;

/**
 * The identity given to a jar that is not an OSGi bundle, but that bundles import packages from.
 */
final class SyntheticBundle {

    private final String symbolicName;
    private final String version;

    /**
     * Initializes a new instance.
     */
    SyntheticBundle(String symbolicName, String version) {
        this.symbolicName = symbolicName;
        this.version = version;
    }

    /**
     * Gets the symbolic name of the bundle.
     */
    String getSymbolicName() {
        return symbolicName;
    }

    /**
     * Gets the version of the bundle, which is also the version of the packages it exports.
     */
    String getVersion() {
        return version;
    }

}
