package org.eclipse.m2e.core.internal;

/**
 * The identifiers of m2e that JDT LS refers to.
 */
public interface IMavenConstants {

    String PLUGIN_ID = "org.eclipse.m2e.core";

    String NATURE_ID = PLUGIN_ID + ".maven2Nature";

    String MARKER_ID = PLUGIN_ID + ".maven2Problem";

    String MARKER_CONFIGURATION_ID = MARKER_ID + ".configuration";

    String MARKER_COLUMN_START = "columnStart";

    String MARKER_COLUMN_END = "columnEnd";

}
