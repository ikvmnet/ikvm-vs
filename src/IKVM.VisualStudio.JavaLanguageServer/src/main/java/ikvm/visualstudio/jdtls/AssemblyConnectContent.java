package ikvm.visualstudio.jdtls;

import java.io.IOException;
import java.io.InputStream;
import java.util.Collections;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.jar.Manifest;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

import org.osgi.framework.connect.ConnectContent;

import cli.System.Reflection.Assembly;

/**
 * The content of a bundle that is an assembly compiled by IKVM: its entries are those of the jar ikvmc embeds in the
 * assembly, which holds the resources of the original jar, its manifest included; its classes come from the assembly's
 * class loader.
 */
final class AssemblyConnectContent implements ConnectContent {

    /**
     * Reads the jar embedded in an assembly, if it has one.
     */
    static AssemblyConnectContent read(Assembly assembly, SyntheticBundle synthetic) throws IOException {
        String jar = null;
        for (String name : assembly.GetManifestResourceNames()) {
            if (name.endsWith(".jar")) {
                jar = name;
                break;
            }
        }

        if (jar == null) {
            return null;
        }

        Map<String, AssemblyConnectEntry> entries = new LinkedHashMap<>();
        try (ZipInputStream zip = new ZipInputStream(new ikvm.io.InputStreamWrapper(assembly.GetManifestResourceStream(jar)))) {
            for (ZipEntry entry; (entry = zip.getNextEntry()) != null;) {
                entries.put(entry.getName(), new AssemblyConnectEntry(entry.getName(), entry.getTime(), readAll(zip)));
            }
        }

        return new AssemblyConnectContent(assembly, entries, synthetic);
    }

    /**
     * Reads the rest of a stream.
     */
    private static byte[] readAll(InputStream in) throws IOException {
        java.io.ByteArrayOutputStream out = new java.io.ByteArrayOutputStream();
        byte[] buffer = new byte[8192];
        for (int n; (n = in.read(buffer)) > 0;) {
            out.write(buffer, 0, n);
        }

        return out.toByteArray();
    }

    private final Assembly assembly;
    private final Map<String, AssemblyConnectEntry> entries;
    private final SyntheticBundle synthetic;

    /**
     * Initializes a new instance.
     */
    private AssemblyConnectContent(Assembly assembly, Map<String, AssemblyConnectEntry> entries, SyntheticBundle synthetic) {
        this.assembly = assembly;
        this.entries = entries;
        this.synthetic = synthetic;
    }

    /**
     * Gets the assembly.
     */
    Assembly getAssembly() {
        return assembly;
    }

    /**
     * Gets whether the content is a bundle: whether its jar has an OSGi manifest, or it is given one.
     */
    boolean isBundle() throws IOException {
        if (synthetic != null) {
            return true;
        }

        AssemblyConnectEntry entry = entries.get("META-INF/MANIFEST.MF");
        if (entry == null) {
            return false;
        }

        try (InputStream in = entry.getInputStream()) {
            return new Manifest(in).getMainAttributes().getValue("Bundle-SymbolicName") != null;
        }
    }

    /**
     * Gets the headers of a jar that is not a bundle, which export every package it has; or none, so that the
     * framework reads the jar's manifest.
     */
    @Override
    public Optional<Map<String, String>> getHeaders() {
        if (synthetic == null) {
            return Optional.empty();
        }

        Set<String> packages = new LinkedHashSet<>();
        for (String name : entries.keySet()) {
            int slash = name.lastIndexOf('/');
            if (slash > 0 && !name.endsWith("/") && !name.startsWith("META-INF/")) {
                packages.add(name.substring(0, slash).replace('/', '.'));
            }
        }

        StringBuilder exports = new StringBuilder();
        for (String p : packages) {
            if (exports.length() > 0) {
                exports.append(',');
            }

            exports.append(p).append(";version=\"").append(synthetic.getVersion()).append('"');
        }

        Map<String, String> headers = new HashMap<>();
        headers.put("Bundle-ManifestVersion", "2");
        headers.put("Bundle-SymbolicName", synthetic.getSymbolicName());
        headers.put("Bundle-Version", synthetic.getVersion());
        headers.put("Export-Package", exports.toString());
        return Optional.of(headers);
    }

    /**
     * Gets the names of the entries.
     */
    @Override
    public Iterable<String> getEntries() {
        return Collections.unmodifiableSet(entries.keySet());
    }

    /**
     * Gets an entry by name.
     */
    @Override
    public Optional<ConnectEntry> getEntry(String path) {
        AssemblyConnectEntry entry = entries.get(path.startsWith("/") ? path.substring(1) : path);
        return Optional.ofNullable(entry);
    }

    /**
     * Gets the class loader of the assembly, which loads the bundle's classes.
     */
    @Override
    public Optional<ClassLoader> getClassLoader() {
        return Optional.of(ikvm.runtime.AssemblyClassLoader.getAssemblyClassLoader(assembly));
    }

    /**
     * Opens the content, which is read already.
     */
    @Override
    public void open() {
    }

    /**
     * Closes the content, which holds nothing open.
     */
    @Override
    public void close() {
    }

}
