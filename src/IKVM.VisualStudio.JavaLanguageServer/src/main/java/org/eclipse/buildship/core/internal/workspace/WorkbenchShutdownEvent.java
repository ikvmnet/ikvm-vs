package org.eclipse.buildship.core.internal.workspace;

import org.eclipse.buildship.core.internal.event.Event;

/**
 * Tells Buildship the workbench is shutting down, so that it saves its Gradle models.
 */
public final class WorkbenchShutdownEvent implements Event {

}
