package ikvm.visualstudio.jdtls;

import java.io.OutputStream;

import cli.System.IO.Stream;

/**
 * Writes to a .NET stream.
 */
final class StreamOutputStream extends OutputStream {

    private final Stream stream;

    /**
     * Initializes a new instance.
     */
    StreamOutputStream(Stream stream) {
        this.stream = stream;
    }

    /**
     * Writes a byte.
     */
    @Override
    public void write(int b) {
        stream.WriteByte((byte) b);
    }

    /**
     * Writes bytes.
     */
    @Override
    public void write(byte[] b, int off, int len) {
        stream.Write(b, off, len);
    }

    /**
     * Flushes the stream.
     */
    @Override
    public void flush() {
        stream.Flush();
    }

    /**
     * Closes the stream.
     */
    @Override
    public void close() {
        stream.Dispose();
    }

}
