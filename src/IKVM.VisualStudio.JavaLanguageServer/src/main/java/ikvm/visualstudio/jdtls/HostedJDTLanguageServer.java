package ikvm.visualstudio.jdtls;

import java.lang.reflect.Method;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Future;

import org.eclipse.jdt.ls.core.internal.handlers.JDTLanguageServer;
import org.eclipse.jdt.ls.core.internal.managers.ProjectsManager;
import org.eclipse.jdt.ls.core.internal.preferences.PreferenceManager;
import org.eclipse.lsp4j.ClientCapabilities;
import org.eclipse.lsp4j.DynamicRegistrationCapabilities;
import org.eclipse.lsp4j.InitializeParams;
import org.eclipse.lsp4j.InitializeResult;
import org.eclipse.lsp4j.TextDocumentClientCapabilities;

/**
 * The protocol of JDT LS for a connection in a process it shares: on exit, it ends the connection, where JDT LS would
 * end the process.
 */
final class HostedJDTLanguageServer extends JDTLanguageServer {

    private volatile Future<Void> listening;

    /**
     * Initializes a new instance.
     */
    HostedJDTLanguageServer(ProjectsManager projects, PreferenceManager preferences) {
        super(projects, preferences);
    }

    /**
     * Sets the listening of the connection, which exit ends.
     */
    void setListening(Future<Void> listening) {
        this.listening = listening;
    }

    /**
     * Initializes the server. Visual Studio says it registers text document features dynamically, so JDT LS leaves them
     * out of its capabilities and registers them later, which Visual Studio does not act on: the client is made to
     * look as if it does not, so that JDT LS states them up front.
     */
    @Override
    public CompletableFuture<InitializeResult> initialize(InitializeParams params) {
        ClientCapabilities capabilities = params.getCapabilities();
        if (capabilities != null && capabilities.getTextDocument() != null) {
            TextDocumentClientCapabilities textDocument = capabilities.getTextDocument();
            for (Method getter : TextDocumentClientCapabilities.class.getMethods()) {
                if (getter.getParameterCount() == 0 && DynamicRegistrationCapabilities.class.isAssignableFrom(getter.getReturnType())) {
                    try {
                        DynamicRegistrationCapabilities capability = (DynamicRegistrationCapabilities) getter.invoke(textDocument);
                        if (capability != null) {
                            capability.setDynamicRegistration(Boolean.FALSE);
                        }
                    } catch (ReflectiveOperationException e) {
                        throw new IllegalStateException(e);
                    }
                }
            }
        }

        return super.initialize(params);
    }

    /**
     * Ends the connection.
     */
    @Override
    public void exit() {
        Future<Void> listening = this.listening;
        if (listening != null) {
            listening.cancel(true);
        }
    }

}
