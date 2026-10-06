package org.eclipse.m2e.core.embedder;

/**
 * The Maven configuration: only the user settings file, which JDT LS keeps in step with its client.
 */
public interface IMavenConfiguration {

    /**
     * Gets the Maven user settings file.
     */
    String getUserSettingsFile();

    /**
     * Sets the Maven user settings file.
     */
    void setUserSettingsFile(String file);

}
