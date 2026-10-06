package ikvm.visualstudio.jdtls;

import java.io.File;
import java.io.IOException;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.Arrays;
import java.util.Hashtable;

import org.eclipse.core.runtime.CoreException;
import org.eclipse.jdt.core.JavaCore;
import org.eclipse.jdt.launching.IVMInstall;
import org.eclipse.jdt.launching.JavaRuntime;
import org.eclipse.jdt.launching.VMStandin;

/**
 * Makes IKVM the JRE that Java projects compile against: a JRE described by an execution environment file, so that
 * JDT does not run a Java executable to find out about it, whose class library is the rt.jar of JDK 8.
 */
final class IkvmRuntime {

    private IkvmRuntime() {
    }

    /**
     * Installs IKVM as the default JRE, at Java 8.
     */
    static void install(File runtimeJar, WorkArea area, PrintStream log) throws IOException, CoreException {
        if (!runtimeJar.isFile()) {
            throw new IOException("The class library " + runtimeJar + " does not exist.");
        }

        File definition = area.resolve("ikvm.ee");
        Files.write(definition.toPath(), Arrays.asList(
            "-Djava.home=" + System.getProperty("java.home"),
            "-Dee.executable=" + cli.System.Environment.get_ProcessPath(),
            "-Dee.bootclasspath=" + runtimeJar.getPath(),
            "-Dee.language.level=1.8",
            "-Dee.class.library.level=JavaSE-1.8",
            "-Dee.name=IKVM " + System.getProperty("java.version")), StandardCharsets.UTF_8);

        VMStandin standin = JavaRuntime.createVMFromDefinitionFile(definition, "IKVM", "ikvm");
        IVMInstall vm = standin.convertToRealVM();
        JavaRuntime.setDefaultVMInstall(vm, null, true);

        // new projects comply with the language level of the JRE
        Hashtable<String, String> options = JavaCore.getOptions();
        JavaCore.setComplianceOptions(JavaCore.VERSION_1_8, options);
        JavaCore.setOptions(options);

        log.println("default JRE: " + vm.getName() + ", class library " + runtimeJar);
    }

}
