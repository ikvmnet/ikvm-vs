package ikvm.visualstudio.jdtls;

import java.io.ByteArrayInputStream;
import java.io.InputStream;

import org.osgi.framework.connect.ConnectContent.ConnectEntry;

/**
 * An entry of the jar embedded in an assembly.
 */
final class AssemblyConnectEntry implements ConnectEntry {

    private final String name;
    private final long lastModified;
    private final byte[] bytes;

    /**
     * Initializes a new instance.
     */
    AssemblyConnectEntry(String name, long lastModified, byte[] bytes) {
        this.name = name;
        this.lastModified = lastModified;
        this.bytes = bytes;
    }

    /**
     * Gets the name of the entry.
     */
    @Override
    public String getName() {
        return name;
    }

    /**
     * Gets the length of the entry.
     */
    @Override
    public long getContentLength() {
        return bytes.length;
    }

    /**
     * Gets when the entry was last modified.
     */
    @Override
    public long getLastModified() {
        return lastModified;
    }

    /**
     * Gets the bytes of the entry.
     */
    @Override
    public byte[] getBytes() {
        return bytes.clone();
    }

    /**
     * Opens the entry.
     */
    @Override
    public InputStream getInputStream() {
        return new ByteArrayInputStream(bytes);
    }

}
