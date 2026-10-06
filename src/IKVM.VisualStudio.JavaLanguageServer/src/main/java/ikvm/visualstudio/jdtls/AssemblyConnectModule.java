package ikvm.visualstudio.jdtls;

import org.osgi.framework.connect.ConnectContent;
import org.osgi.framework.connect.ConnectModule;

/**
 * A bundle that is an assembly compiled by IKVM.
 */
final class AssemblyConnectModule implements ConnectModule {

    private final ConnectContent content;

    /**
     * Initializes a new instance.
     */
    AssemblyConnectModule(ConnectContent content) {
        this.content = content;
    }

    /**
     * Gets the content of the bundle.
     */
    @Override
    public ConnectContent getContent() {
        return content;
    }

}
