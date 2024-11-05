// Copyright (C) 2023 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of TrustVPN applcation
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Docker.DotNet;
using Docker.DotNet.Models;

namespace TrustVpn.Services;

public class TrustVpnBaseContainer
{
    private readonly string _containerName;
    private string? _containerId;
    private static readonly string[] _shell = ["/bin/bash", "-c"];
    private readonly ILogger _logger;

    public TrustVpnBaseContainer(string containerName, ILogger logger)
    {
        _containerName = containerName;
        _logger = logger;
    }

    public async Task<string?> GetContainerId()
    {
        _containerId = null;
        try {
            var dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters());
            foreach (var container in containers) {
                if (container.Names.Contains($"/{_containerName}")) {
                    _containerId = container.ID;
                    break;
                }
            }
        }
        catch (Exception e) {
            _logger.LogError("Failed to fetch container id for '{name}':\n{msg}", _containerName.ToString(), e);
        }

        return _containerId;
    }

    public async Task<string?> RunInContainer(string cmd)
    {
        string? output = null;
        string? stderr;

        if (_containerId == null) await GetContainerId();
        if (_containerId == null) return output;

        string[] command = [.. _shell, cmd];

        _logger.LogDebug("Executing command '{cmd}' in '{cnt}'", cmd.ToString(), _containerName.ToString());

        try {
            var dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
            var execCreateParameters = new ContainerExecCreateParameters() {
                AttachStderr = true,
                AttachStdout = true,
                Cmd = command
            };
            var execCreateResponse = await dockerClient.Exec.ExecCreateContainerAsync(_containerId, execCreateParameters);

            var stream = await dockerClient.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false);
            var execInspectResponse = await dockerClient.Exec.InspectContainerExecAsync(execCreateResponse.ID);

            var cancellationToken = new CancellationToken();
            (output, stderr) = await stream.ReadOutputToEndAsync(cancellationToken);


            if (execInspectResponse.ExitCode != 0) {
                _logger.LogDebug("Command '{cmd}' in '{cnt}' exited with code {code}\nStdout >>>>\n{stdout}\n<<<< End of stdout\nStderr >>>>\n{stderr}\n<<<< End of stderr\n",
                                  cmd.ToString(), _containerName.ToString(), execInspectResponse.ExitCode, output, stderr);
                output = stderr;
            }
            else
            {
                _logger.LogDebug("Command '{cmd}' in '{cnt}' completed successfully (exit code 0). Sensitive output information was hidden from the log.", cmd, _containerName);
            }
        }
        catch (Exception e) {
            _logger.LogError("Failed to execute command '{cmd}' in '{cnt}':\n{msg}", cmd.ToString(), _containerName.ToString(), e.Message);
        }

        return output;
    }
}
