using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Renci.SshNet;

partial class Build
{
    [PathVariable] public Tool Scp;
    [PathVariable] public Tool Ssh;

    [Parameter] readonly int MaxReleasesOnServer = 2;
    [Parameter] readonly string DirectoryForReleases = "releases";

    [Parameter] readonly int SshPort = 10366;
    [Parameter] readonly string SshUser = "frog";
    [Parameter] readonly string SshServer = "frog02.mikr.us";
    ConnectionInfo SshConnectionInfo => new(SshServer, port: SshPort, SshUser, new PrivateKeyAuthenticationMethod(SshUser, new PrivateKeyFile(SshPrivateKey)));

    AbsolutePath SshPrivateKey = RootDirectory / "ztrtemplates";
    AbsolutePath VelopackReleaseMirroredFromRemoteServer => VelopackRootDirectory / DirectoryForReleases;
    string NameProjectDirectoryOnTheServer => NameOfProjectToBePublished.ToLower();

    Target DownloadServerToLocal => _ => _
        .Executes(() =>
        {
            using var sshClient = new SshClient(SshConnectionInfo);
            sshClient.Connect();
            using var cmd = sshClient.RunCommand($"mkdir -p /var/www/html/{NameProjectDirectoryOnTheServer}/{DirectoryForReleases}");

            VelopackReleaseMirroredFromRemoteServer.CreateOrCleanDirectory();
            Scp.Invoke(
                $"-i {SshPrivateKey} -r -P {SshPort} {SshUser}@{SshServer}:/var/www/html/{NameProjectDirectoryOnTheServer}/{DirectoryForReleases} {VelopackReleaseMirroredFromRemoteServer}");

            Vpk.Invoke($"vpk download local --path {VelopackReleaseMirroredFromRemoteServer} --channel {Channel} --outputDir {VelopackPublish}");
        });

    Target UploadLocalToServer => _ => _
        .DependsOn(ConfigureAppSettings)
        .DependsOn(PackWithVelopack)
        .DependsOn(DownloadServerToLocal)
        .Executes(() =>
        {

            Vpk.Invoke($"vpk upload local --path {VelopackReleaseMirroredFromRemoteServer} --channel {Channel} --keepMaxReleases {MaxReleasesOnServer} --outputDir {VelopackPublish} --regenerate");
            Scp.Invoke($"-i {SshPrivateKey} -r -P {SshPort} {VelopackReleaseMirroredFromRemoteServer} {SshUser}@{SshServer}:/var/www/html/{NameProjectDirectoryOnTheServer}/");

            using var sshClient = new SshClient(SshConnectionInfo);
            sshClient.Connect();
            using var cmd = sshClient.RunCommand($"chmod 755 /var/www/html/{NameProjectDirectoryOnTheServer}/{DirectoryForReleases}'");
        });
}
