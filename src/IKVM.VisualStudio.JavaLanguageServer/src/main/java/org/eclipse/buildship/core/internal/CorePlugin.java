package org.eclipse.buildship.core.internal;

import java.io.File;

import org.eclipse.buildship.core.internal.event.ListenerRegistry;
import org.eclipse.core.runtime.IPath;
import org.eclipse.core.runtime.Path;

/**
 * Gives JDT LS the state location of Buildship, which holds no Gradle models, and a listener registry nobody listens
 * to.
 */
public final class CorePlugin {

    private static final CorePlugin INSTANCE = new CorePlugin();

    private static final ListenerRegistry LISTENER_REGISTRY = event -> { };

    private CorePlugin() {
    }

    /**
     * Gets the plug-in.
     */
    public static CorePlugin getInstance() {
        return INSTANCE;
    }

    /**
     * Gets the registry of listeners to Buildship events.
     */
    public static ListenerRegistry listenerRegistry() {
        return LISTENER_REGISTRY;
    }

    /**
     * Gets where Buildship keeps its state: a directory that does not exist, as there is none.
     */
    public IPath getStateLocation() {
        return Path.fromOSString(new File(System.getProperty("java.io.tmpdir"), "ikvm-jdtls-buildship").getPath());
    }

}
