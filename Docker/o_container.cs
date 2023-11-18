using System.IO;
using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;


class Program1
{
    static async Task Main(string[] args)
    {
        var dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

        string containerId = "your_container_id";
        string command = "ls";

        var execCreateParameters = new ContainerExecCreateParameters()
        {
            AttachStderr = true,
            AttachStdout = true,
            Cmd = new string[] { "/bin/bash", "-c", command }
        };

        var execCreateResponse = await dockerClient.Exec.ExecCreateContainerAsync(containerId, execCreateParameters);

        var execStartParameters = new ContainerExecStartParameters();


        var stream = await dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false);
        var execInspectResponse = await dockerClient.Exec.InspectContainerExecAsync(execCreateResponse.ID);
        Console.WriteLine(execInspectResponse.ExitCode); // Exit code of the command
        Console.WriteLine(execInspectResponse.Running); // Whether the command is still running

        var cancellationToken = new CancellationToken();
        var output = stream.ReadOutputToEndAsync(cancellationToken);
        Console.WriteLine(output); // Output of the command
    }
}

class Program2
{
    static async Task Main(string[] args)
    {
        var dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

        var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters());

        string containerNameToFind = "your_container_name";

        foreach (var container in containers)
        {
            if (container.Names.Contains($"/{containerNameToFind}"))
            {
                Console.WriteLine($"Container ID for {containerNameToFind}: {container.ID}");
                break;
            }
        }
    }
}
