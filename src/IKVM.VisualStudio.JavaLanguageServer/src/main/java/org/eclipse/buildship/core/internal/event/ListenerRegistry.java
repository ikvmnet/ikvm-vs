package org.eclipse.buildship.core.internal.event;

/**
 * Dispatches Buildship events to their listeners.
 */
public interface ListenerRegistry {

    /**
     * Dispatches an event to its listeners.
     */
    void dispatch(Event event);

}
