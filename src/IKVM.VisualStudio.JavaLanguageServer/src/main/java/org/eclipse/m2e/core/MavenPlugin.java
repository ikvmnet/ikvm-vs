package org.eclipse.m2e.core;

import org.eclipse.m2e.core.embedder.IMavenConfiguration;

/**
 * Gives JDT LS the Maven configuration it reads and writes the user settings file of.
 */
public final class MavenPlugin {

    private static final IMavenConfiguration CONFIGURATION = new IMavenConfiguration() {

        private String userSettingsFile;

        /**
         * Gets the Maven user settings file.
         */
        @Override
        public String getUserSettingsFile() {
            return userSettingsFile;
        }

        /**
         * Sets the Maven user settings file.
         */
        @Override
        public void setUserSettingsFile(String file) {
            userSettingsFile = file;
        }

    };

    private MavenPlugin() {
    }

    /**
     * Gets the Maven configuration.
     */
    public static IMavenConfiguration getMavenConfiguration() {
        return CONFIGURATION;
    }

}
