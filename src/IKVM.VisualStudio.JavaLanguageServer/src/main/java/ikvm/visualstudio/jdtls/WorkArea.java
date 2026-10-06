package ikvm.visualstudio.jdtls;

import java.io.File;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.channels.FileLock;

/**
 * The directory a language server keeps its Eclipse workspace and framework storage in. Each process takes the first
 * of the numbered directories no other process holds, so that the workspaces it has used, and their indexes, are
 * reused by later processes.
 */
final class WorkArea {

    /**
     * Takes the first free work area under a root directory, which stays taken until the process exits.
     */
    static WorkArea acquire(File root) throws IOException {
        for (int i = 0; ; i++) {
            File directory = new File(root, Integer.toString(i));
            if (!directory.isDirectory() && !directory.mkdirs()) {
                throw new IOException("Failed to create " + directory + ".");
            }

            RandomAccessFile file = new RandomAccessFile(new File(directory, ".lock"), "rw");
            FileLock lock = file.getChannel().tryLock();
            if (lock != null) {
                return new WorkArea(directory, lock);
            }

            file.close();
        }
    }

    private final File directory;
    private final FileLock lock;

    /**
     * Initializes a new instance.
     */
    private WorkArea(File directory, FileLock lock) {
        this.directory = directory;
        this.lock = lock;
    }

    /**
     * Gets the directory.
     */
    File getDirectory() {
        return directory;
    }

    /**
     * Gets a file or directory in the work area.
     */
    File resolve(String name) {
        return new File(directory, name);
    }

}
