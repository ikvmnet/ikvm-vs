package ikvm.visualstudio.jdtls;

import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintStream;
import java.lang.reflect.Field;
import java.util.concurrent.CancellationException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import org.eclipse.jdt.ls.core.internal.JavaClientConnection.JavaLanguageClient;
import org.eclipse.jdt.ls.core.internal.JavaLanguageServerPlugin;
import org.eclipse.jdt.ls.core.internal.LanguageServer;
import org.eclipse.lsp4j.jsonrpc.Launcher;

import cli.System.IO.Stream;

/**
 * Serves the Language Server Protocol for Java, with Eclipse JDT LS, over streams the host provides. The framework
 * starts with the first connection, and stays for the life of the process; one connection is served at a time.
 */
public final class JavaLanguageServer {

    private static final Object LOCK = new Object();

    private JavaLanguageServer() {
    }

    /**
     * Serves a connection over a .NET stream, until the client exits or the stream ends.
     */
    public static void serve(Stream stream) throws Exception {
        serve(new ikvm.io.InputStreamWrapper(stream), new StreamOutputStream(stream));
    }

    /**
     * Serves a connection, until the client exits or the input ends.
     */
    public static void serve(InputStream in, OutputStream out) throws Exception {
        synchronized (LOCK) {
            EclipseFramework framework = EclipseFramework.get();
            PrintStream log = framework.getLog();
            JavaLanguageServerPlugin plugin = JavaLanguageServerPlugin.getInstance();
            if (plugin == null) {
                throw new IllegalStateException("JDT LS did not start.");
            }

            // the plug-in knows its server and protocol from its own startup, which reads standard input and output
            setLanguageServer(plugin, new LanguageServer());
            HostedJDTLanguageServer protocol = new HostedJDTLanguageServer(JavaLanguageServerPlugin.getProjectsManager(), JavaLanguageServerPlugin.getPreferencesManager());
            plugin.setProtocol(protocol);

            ExecutorService executor = Executors.newCachedThreadPool();
            try {
                Launcher<JavaLanguageClient> launcher = Launcher.createLauncher(protocol, JavaLanguageClient.class, in, out, executor, consumer -> consumer);
                protocol.connectClient(launcher.getRemoteProxy());
                Future<Void> listening = launcher.startListening();
                protocol.setListening(listening);
                log.println("connected");

                try {
                    listening.get();
                } catch (CancellationException e) {
                    // the client exited
                }
            } finally {
                protocol.disconnectClient();
                executor.shutdownNow();
                log.println("disconnected");
            }
        }
    }

    /**
     * Sets the language server the plug-in knows, which the protocol asks to record the client's process.
     */
    private static void setLanguageServer(JavaLanguageServerPlugin plugin, LanguageServer server) throws ReflectiveOperationException {
        Field field = JavaLanguageServerPlugin.class.getDeclaredField("languageServer");
        field.setAccessible(true);
        if (field.get(plugin) == null) {
            field.set(plugin, server);
        }
    }

}
