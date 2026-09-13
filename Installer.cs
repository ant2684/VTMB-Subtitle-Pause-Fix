using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

#if PROTEAN
[assembly: AssemblyTitle("VTMB Protean Mod Installer")]
[assembly: AssemblyProduct("VTMB Protean Mod Pack")]
[assembly: AssemblyDescription("Installer for the VTMB Protean Mod Pack")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]
[assembly: AssemblyInformationalVersion("1.0.2")]
#else
[assembly: AssemblyTitle("VTMB Subtitle Pause Fix Installer")]
[assembly: AssemblyProduct("VTMB Subtitle Pause Fix")]
[assembly: AssemblyDescription("Installer for VTMB Subtitle Pause Fix")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
#endif
[assembly: AssemblyCompany("Ant2684")]
[assembly: AssemblyCopyright("Copyright © Ant2684 2026")]

namespace VtmbInstaller
{
    public sealed class PackageManifest
    {
        public int formatVersion { get; set; }
        public string packageId { get; set; }
        public string packageName { get; set; }
        public string packageVersion { get; set; }
        public TargetSpec target { get; set; }
        public List<ComponentSpec> components { get; set; }
        public BlockGroupSpec blockGroup { get; set; }
        public ModelSystemSpec modelSystem { get; set; }
    }

    public sealed class TargetSpec
    {
        public string name { get; set; }
        public string versionMarkerPath { get; set; }
        public string versionMarker { get; set; }
        public string plusConfigPath { get; set; }
        public string plusAlias { get; set; }
        public string plusCommand { get; set; }
        public string relativePath { get; set; }
        public string recipe { get; set; }
        public string originalSha256 { get; set; }
        public long originalSize { get; set; }
        public string patchedSha256 { get; set; }
        public long patchedSize { get; set; }
    }

    public sealed class ComponentSpec
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public bool defaultSelected { get; set; }
        public string handler { get; set; }
        public string warning { get; set; }
        public List<FileSpec> files { get; set; }
        public string target { get; set; }
        public string alias { get; set; }
        public string originalCommand { get; set; }
        public string patchedCommand { get; set; }
    }

    public sealed class FileSpec
    {
        public string target { get; set; }
        public string recipe { get; set; }
        public string patchedSha256 { get; set; }
        public List<string> allowedOriginalSha256 { get; set; }
        public List<string> legacyOriginalSha256 { get; set; }
        public List<string> legacyPatchedSha256 { get; set; }
        public string sha256 { get; set; }
    }

    public sealed class BlockGroupSpec
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string @default { get; set; }
        public List<BlockOptionSpec> options { get; set; }
    }

    public sealed class BlockOptionSpec
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public sealed class ModelSystemSpec
    {
        public List<FileSpec> preconditions { get; set; }
        public List<string> managedTargets { get; set; }
        public List<ModelStateSpec> states { get; set; }
    }

    public sealed class ModelStateSpec
    {
        public bool hitFix { get; set; }
        public string blockStyle { get; set; }
        public List<FileSpec> files { get; set; }
    }

    public sealed class FileState
    {
        public bool Exists;
        public string Sha256;
        public long Size;
    }

    public sealed class DesiredFile
    {
        public string Target;
        public bool Exists;
        public string SourcePath;
        public byte[] Bytes;
        public string Sha256;
    }

    public sealed class DesiredMap
    {
        public Dictionary<string, DesiredFile> Files = new Dictionary<string, DesiredFile>(StringComparer.OrdinalIgnoreCase);
        public bool EffectiveHitFix;
        public string EffectiveBlockStyle = "None";
    }

    public sealed class BackupManifest
    {
        public int formatVersion { get; set; }
        public string packageId { get; set; }
        public string packageVersion { get; set; }
        public string gameRoot { get; set; }
        public string createdUtc { get; set; }
        public List<BackupEntry> files { get; set; }
    }

    public sealed class BackupEntry
    {
        public string target { get; set; }
        public bool exists { get; set; }
        public string sha256 { get; set; }
        public long size { get; set; }
        public string backupPath { get; set; }
    }

    public sealed class ExpectedEntry
    {
        public string target { get; set; }
        public bool exists { get; set; }
        public string sha256 { get; set; }
    }

    public sealed class InstallState
    {
        public int formatVersion { get; set; }
        public string packageId { get; set; }
        public string packageVersion { get; set; }
        public string gameRoot { get; set; }
        public string updatedUtc { get; set; }
        public bool backupUsed { get; set; }
        public bool restored { get; set; }
        public List<string> selectedComponents { get; set; }
        public string requestedBlockStyle { get; set; }
        public bool effectiveHitFix { get; set; }
        public string effectiveBlockStyle { get; set; }
        public List<ExpectedEntry> expectedFiles { get; set; }
    }

    public sealed class TransactionManifest
    {
        public int formatVersion { get; set; }
        public string packageId { get; set; }
        public string gameRoot { get; set; }
        public string createdUtc { get; set; }
        public List<TransactionEntry> entries { get; set; }
    }

    public sealed class TransactionEntry
    {
        public string target { get; set; }
        public bool beforeExists { get; set; }
        public string beforeSha256 { get; set; }
        public string beforePath { get; set; }
        public bool afterExists { get; set; }
        public string afterSha256 { get; set; }
        public string afterPath { get; set; }
    }

    public interface IProgressSink { void Report(int value, string text); }
    public sealed class NullProgress : IProgressSink { public void Report(int value, string text) { } }
    public sealed class DelegateProgress : IProgressSink
    {
        private readonly Action<int, string> callback;
        public DelegateProgress(Action<int, string> callback) { this.callback = callback; }
        public void Report(int value, string text) { callback(Math.Max(0, Math.Min(100, value)), text); }
    }

    public sealed class InstallerCore
    {
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 100 };
        public readonly PackageManifest Manifest;
        public readonly string PackageRoot;
        public readonly bool IsProtean;
        public string BackupRoot { get { return Path.Combine(PackageRoot, "Backup"); } }
        public string BackupManifestPath { get { return Path.Combine(BackupRoot, "backup-manifest.json"); } }
        public string StatePath { get { return Path.Combine(PackageRoot, "install-state.json"); } }

        public InstallerCore()
        {
            PackageRoot = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VTMB.Manifest.json"))
            {
                if (stream == null) throw new InvalidOperationException("The embedded package manifest is missing.");
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    Manifest = json.Deserialize<PackageManifest>(reader.ReadToEnd());
            }
            if (Manifest == null || Manifest.formatVersion != 1)
                throw new InvalidOperationException("The embedded package manifest is invalid.");
            IsProtean = string.Equals(Manifest.packageId, "vtmb-protean-mod-pack", StringComparison.Ordinal);
            if (!IsProtean && !string.Equals(Manifest.packageId, "vtmb-subtitle-pause-fix", StringComparison.Ordinal))
                throw new InvalidOperationException("The embedded package identifier is invalid.");
            string expectedVersion = IsProtean ? "1.0.2" : "1.0.0";
            if (!string.Equals(Manifest.packageVersion, expectedVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("The embedded package version is invalid.");
        }

        public string FullChild(string root, string relative)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string normalized = relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(rootFull, normalized));
            string prefix = rootFull + Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path escapes its permitted root: " + relative);
            return full;
        }

        public string HashFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        public string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }

        public FileState GetFileState(string path)
        {
            if (!File.Exists(path)) return new FileState { Exists = false, Size = 0 };
            var info = new FileInfo(path);
            return new FileState { Exists = true, Size = info.Length, Sha256 = HashFile(path) };
        }

        private void ClearReadOnly(string path)
        {
            if (!File.Exists(path)) return;
            FileAttributes value = File.GetAttributes(path);
            if ((value & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, value & ~FileAttributes.ReadOnly);
        }

        private void CopyReplacing(string source, string target)
        {
            FileAttributes attributes = File.GetAttributes(source);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            ClearReadOnly(target);
            File.Copy(source, target, true);
            File.SetAttributes(target, attributes);
        }

        private void AtomicReplace(string source, string target, string temporaryPrefix)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            FileAttributes wanted = File.GetAttributes(source);
            string temporary = Path.Combine(Path.GetDirectoryName(target), temporaryPrefix + Guid.NewGuid().ToString("N") + ".tmp");
            string previous = temporary + ".previous";
            File.Copy(source, temporary, false);
            ClearReadOnly(temporary);
            try
            {
                if (File.Exists(target))
                {
                    ClearReadOnly(target);
                    File.Replace(temporary, target, previous);
                    if (File.Exists(previous)) { ClearReadOnly(previous); File.Delete(previous); }
                }
                else File.Move(temporary, target);
                File.SetAttributes(target, wanted);
            }
            finally
            {
                if (File.Exists(temporary)) { ClearReadOnly(temporary); File.Delete(temporary); }
                if (File.Exists(previous)) { ClearReadOnly(previous); File.Delete(previous); }
            }
        }

        private void RemovePackageDirectory(string path)
        {
            string package = PackageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            if (!full.StartsWith(package, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to remove a directory outside the package: " + full);
            if (!Directory.Exists(full)) return;
            foreach (string file in Directory.GetFiles(full, "*", SearchOption.AllDirectories)) ClearReadOnly(file);
            Directory.Delete(full, true);
        }

        private void WriteJson<T>(string path, T value)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(temporary, json.Serialize(value), new UTF8Encoding(false));
            if (File.Exists(path)) { ClearReadOnly(path); File.Delete(path); }
            File.Move(temporary, path);
        }

        private T ReadJson<T>(string path) { return json.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8)); }

        public void VerifyPackage(IProgressSink progress)
        {
            progress.Report(3, "Checking package...");
            PatchEngine.SelfTest();
            var recipes = new HashSet<string>(StringComparer.Ordinal);
            if (IsProtean)
            {
                foreach (var component in Manifest.components ?? new List<ComponentSpec>())
                    if (component.handler == "fileSet") foreach (var file in component.files)
                        recipes.Add(file.recipe);
                foreach (var state in Manifest.modelSystem.states)
                    foreach (var file in state.files) recipes.Add(file.recipe);
            }
            else recipes.Add(Manifest.target.recipe);
            int index = 0;
            foreach (string recipe in recipes.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (PatchEngine.IsBinaryRecipe(recipe)) PatchEngine.VerifyBinaryRecipe(recipe);
                else if (PatchEngine.IsModelRecipe(recipe)) PatchEngine.VerifyModelRecipe(recipe);
                else throw new InvalidDataException("Unknown patch recipe: " + recipe);
                index++;
                progress.Report(3 + (index * 12 / Math.Max(1, recipes.Count)), "Checking patch recipes...");
            }
        }

        public string ResolveGameRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Select the Bloodlines game folder.");
            string full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (!Directory.Exists(full)) throw new DirectoryNotFoundException("Game folder was not found: " + full);
            return full;
        }

        public void AssertGameClosed()
        {
            if (Process.GetProcessesByName("Vampire").Length > 0)
                throw new InvalidOperationException("Close Vampire: The Masquerade - Bloodlines before continuing.");
        }

        public void AssertGameLayout(string root)
        {
            if (!File.Exists(Path.Combine(root, "Vampire.exe")))
                throw new FileNotFoundException("Vampire.exe was not found in the selected folder.");
            if (!IsProtean)
            {
                string engine = FullChild(root, Manifest.target.relativePath);
                if (!File.Exists(engine)) throw new FileNotFoundException("Bin\\engine.dll was not found.");
                return;
            }
            string marker = FullChild(root, Manifest.target.versionMarkerPath);
            if (!File.Exists(marker) || File.ReadAllText(marker).IndexOf(Manifest.target.versionMarker, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("This package requires Unofficial Patch 11.5 Plus.");
            string config = FullChild(root, Manifest.target.plusConfigPath);
            if (!File.Exists(config)) throw new FileNotFoundException("Unofficial Patch Plus configuration was not found.");
            AliasInfo plus = GetAliasInfo(File.ReadAllBytes(config), Manifest.target.plusAlias);
            if (!plus.Found || !string.Equals(plus.Command, Manifest.target.plusCommand, StringComparison.Ordinal))
                throw new InvalidOperationException("This package requires the Unofficial Patch 11.5 Plus profile.");
        }

        private sealed class AliasInfo { public bool Found; public string Command; public int Offset; public int Length; }
        private AliasInfo GetAliasInfo(byte[] bytes, string alias)
        {
            string text = Encoding.GetEncoding(28591).GetString(bytes);
            string pattern = "(?m)^[\\t ]*alias[\\t ]+" + Regex.Escape(alias) + "[\\t ]+\"([^\"]*)\"[\\t ]*\\r?$";
            MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.CultureInvariant);
            if (matches.Count != 1) return new AliasInfo { Found = false };
            Group group = matches[0].Groups[1];
            return new AliasInfo { Found = true, Command = group.Value, Offset = group.Index, Length = group.Length };
        }

        private byte[] SetAlias(byte[] bytes, ComponentSpec component)
        {
            AliasInfo info = GetAliasInfo(bytes, component.alias);
            if (!info.Found) throw new InvalidDataException("The " + component.alias + " command is missing or ambiguous.");
            if (info.Command == component.patchedCommand) return bytes;
            if (info.Command != component.originalCommand)
                throw new InvalidDataException("Unsupported " + component.alias + " command: " + info.Command);
            byte[] replacement = Encoding.ASCII.GetBytes(component.patchedCommand);
            byte[] result = new byte[bytes.Length - info.Length + replacement.Length];
            Buffer.BlockCopy(bytes, 0, result, 0, info.Offset);
            Buffer.BlockCopy(replacement, 0, result, info.Offset, replacement.Length);
            int tail = bytes.Length - info.Offset - info.Length;
            Buffer.BlockCopy(bytes, info.Offset + info.Length, result, info.Offset + replacement.Length, tail);
            return result;
        }

        public List<string> ManagedTargets()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!IsProtean) set.Add(Manifest.target.relativePath);
            else
            {
                foreach (var component in Manifest.components)
                {
                    if (component.handler == "fileSet") foreach (var file in component.files) set.Add(file.target);
                    else if (component.handler == "textAlias") set.Add(component.target);
                }
                foreach (string target in Manifest.modelSystem.managedTargets) set.Add(target);
            }
            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private ModelStateSpec DetectModelState(Func<string, FileState> lookup)
        {
            var matches = new List<ModelStateSpec>();
            foreach (var state in Manifest.modelSystem.states)
            {
                var expected = state.files.ToDictionary(f => f.target, f => f.sha256, StringComparer.OrdinalIgnoreCase);
                bool ok = true;
                foreach (string target in Manifest.modelSystem.managedTargets)
                {
                    FileState actual = lookup(target);
                    string hash;
                    if (expected.TryGetValue(target, out hash)) ok = actual.Exists && string.Equals(actual.Sha256, hash, StringComparison.OrdinalIgnoreCase);
                    else ok = !actual.Exists;
                    if (!ok) break;
                }
                if (ok) matches.Add(state);
            }
            if (matches.Count != 1) throw new InvalidDataException("Unsupported combination of loose Claws, Fists or Tire Iron models.");
            return matches[0];
        }

        private bool IsLegacyFileState(FileSpec file, string hash)
        {
            return (file.legacyOriginalSha256 ?? new List<string>()).Any(h => Eq(h, hash)) ||
                   (file.legacyPatchedSha256 ?? new List<string>()).Any(h => Eq(h, hash));
        }

        private void AssertNoLegacyProteanState(string root)
        {
            if (!IsProtean) return;
            foreach (var component in Manifest.components.Where(c => c.handler == "fileSet"))
                foreach (var file in component.files)
                    if (IsLegacyFileState(file, HashFile(FullChild(root, file.target))))
                        throw new InvalidOperationException("An older supported UP file is installed: " + file.target + ". Use Restore if a persistent backup is available, then update Unofficial Patch 11.5 Plus before installing.");
        }

        private void AssertKnownLiveState(string root, bool allowLegacy)
        {
            if (!IsProtean)
            {
                FileState engine = GetFileState(FullChild(root, Manifest.target.relativePath));
                bool supported = (engine.Size == Manifest.target.originalSize && Eq(engine.Sha256, Manifest.target.originalSha256)) ||
                                 (engine.Size == Manifest.target.patchedSize && Eq(engine.Sha256, Manifest.target.patchedSha256));
                if (!supported) throw new InvalidDataException("Unsupported or modified Bin\\engine.dll. No files were changed.");
                return;
            }
            foreach (var pre in Manifest.modelSystem.preconditions)
            {
                string path = FullChild(root, pre.target);
                if (!File.Exists(path) || !Eq(HashFile(path), pre.sha256))
                    throw new InvalidDataException("Unsupported UP animation reference: " + pre.target);
            }
            foreach (var component in Manifest.components)
            {
                if (component.handler == "fileSet")
                {
                    foreach (var file in component.files)
                    {
                        string path = FullChild(root, file.target);
                        if (!File.Exists(path)) throw new FileNotFoundException("Required game file is missing: " + file.target);
                        string hash = HashFile(path);
                        bool allowed = Eq(hash, file.patchedSha256) || (file.allowedOriginalSha256 ?? new List<string>()).Any(h => Eq(h, hash)) ||
                                       (allowLegacy && IsLegacyFileState(file, hash));
                        if (!allowed) throw new InvalidDataException("Unsupported or modified file: " + file.target);
                    }
                }
                else if (component.handler == "textAlias")
                {
                    AliasInfo info = GetAliasInfo(File.ReadAllBytes(FullChild(root, component.target)), component.alias);
                    if (!info.Found || (info.Command != component.originalCommand && info.Command != component.patchedCommand))
                        throw new InvalidDataException("Unsupported " + component.alias + " command in " + component.target);
                }
            }
            DetectModelState(t => GetFileState(FullChild(root, t)));
        }

        private static bool Eq(string left, string right) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }

        private bool HasProteanChanges(string root)
        {
            foreach (var component in Manifest.components.Where(c => c.handler == "fileSet"))
                foreach (var file in component.files)
                {
                    string hash = HashFile(FullChild(root, file.target));
                    if (Eq(hash, file.patchedSha256) || (file.legacyPatchedSha256 ?? new List<string>()).Any(h => Eq(h, hash))) return true;
                }
            ComponentSpec alias = Manifest.components.First(c => c.handler == "textAlias");
            AliasInfo info = GetAliasInfo(File.ReadAllBytes(FullChild(root, alias.target)), alias.alias);
            if (info.Found && info.Command == alias.patchedCommand) return true;
            ModelStateSpec model = DetectModelState(t => GetFileState(FullChild(root, t)));
            return model.hitFix || !Eq(model.blockStyle, "None");
        }

        public bool HasBackup { get { return File.Exists(BackupManifestPath); } }

        private BackupManifest ReadBackup(string expectedRoot)
        {
            if (!File.Exists(BackupManifestPath)) throw new InvalidOperationException("A persistent backup is not available.");
            BackupManifest backup = ReadJson<BackupManifest>(BackupManifestPath);
            if (backup == null || backup.formatVersion != 1 || backup.packageId != Manifest.packageId)
                throw new InvalidDataException("The persistent backup is invalid.");
            string backupRoot = ResolveGameRoot(backup.gameRoot);
            if (!string.IsNullOrEmpty(expectedRoot) && !Eq(backupRoot, expectedRoot))
                throw new InvalidDataException("The persistent backup belongs to a different game folder: " + backupRoot);
            var expected = ManagedTargets();
            if (backup.files == null || backup.files.Count != expected.Count)
                throw new InvalidDataException("The persistent backup is incomplete.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in backup.files)
            {
                if (!expected.Contains(entry.target, StringComparer.OrdinalIgnoreCase) || !seen.Add(entry.target))
                    throw new InvalidDataException("Unexpected backup entry: " + entry.target);
                if (entry.exists)
                {
                    string path = FullChild(BackupRoot, entry.backupPath);
                    FileState state = GetFileState(path);
                    if (!state.Exists || state.Size != entry.size || !Eq(state.Sha256, entry.sha256))
                        throw new InvalidDataException("Backed-up file is missing or damaged: " + entry.target);
                }
            }
            return backup;
        }

        private BackupManifest CreateBackup(string root, IProgressSink progress)
        {
            if (Directory.Exists(BackupRoot)) throw new InvalidDataException("The Backup folder already exists but is not valid.");
            string pending = Path.Combine(PackageRoot, ".backup-pending-" + Guid.NewGuid().ToString("N"));
            try
            {
                var backup = new BackupManifest { formatVersion = 1, packageId = Manifest.packageId, packageVersion = Manifest.packageVersion, gameRoot = root, createdUtc = DateTime.UtcNow.ToString("o"), files = new List<BackupEntry>() };
                List<string> targets = ManagedTargets();
                for (int i = 0; i < targets.Count; i++)
                {
                    string relative = targets[i];
                    string source = FullChild(root, relative);
                    FileState state = GetFileState(source);
                    string backupPath = null;
                    if (state.Exists)
                    {
                        backupPath = "Files/" + relative.Replace('\\', '/');
                        string destination = FullChild(pending, backupPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        File.Copy(source, destination, false);
                        if (!Eq(HashFile(destination), state.Sha256)) throw new IOException("Backup verification failed: " + relative);
                    }
                    backup.files.Add(new BackupEntry { target = relative.Replace('\\', '/'), exists = state.Exists, sha256 = state.Sha256, size = state.Size, backupPath = backupPath });
                    progress.Report(18 + ((i + 1) * 17 / Math.Max(1, targets.Count)), "Creating persistent backup...");
                }
                WriteJson(Path.Combine(pending, "backup-manifest.json"), backup);
                Directory.Move(pending, BackupRoot);
                return ReadBackup(root);
            }
            catch
            {
                if (Directory.Exists(pending)) RemovePackageDirectory(pending);
                throw;
            }
        }

        private InstallState ReadState()
        {
            if (!File.Exists(StatePath)) return null;
            InstallState state = ReadJson<InstallState>(StatePath);
            if (state == null || state.formatVersion != 1 || state.packageId != Manifest.packageId)
                throw new InvalidDataException("Installer state is invalid.");
            return state;
        }

        private void AssertCurrentMatches(string root, IEnumerable<ExpectedEntry> entries)
        {
            foreach (var entry in entries)
            {
                FileState current = GetFileState(FullChild(root, entry.target));
                if (current.Exists != entry.exists || (current.Exists && !Eq(current.Sha256, entry.sha256)))
                    throw new InvalidDataException("A managed file changed outside the installer: " + entry.target);
            }
        }

        private Dictionary<string, BackupEntry> BackupLookup(BackupManifest backup)
        {
            return backup.files.ToDictionary(x => x.target, x => x, StringComparer.OrdinalIgnoreCase);
        }

        private ModelStateSpec SnapshotModelState(string root, BackupManifest backup)
        {
            if (backup == null) return DetectModelState(t => GetFileState(FullChild(root, t)));
            var lookup = BackupLookup(backup);
            return DetectModelState(t =>
            {
                BackupEntry e = lookup[t];
                return new FileState { Exists = e.exists, Sha256 = e.sha256, Size = e.size };
            });
        }

        private DesiredMap BuildProteanDesired(string root, List<string> selected, string blockStyle, BackupManifest backup)
        {
            var result = new DesiredMap();
            Dictionary<string, BackupEntry> lookup = backup == null ? null : BackupLookup(backup);
            foreach (string relative in ManagedTargets())
            {
                if (lookup != null)
                {
                    BackupEntry entry = lookup[relative];
                    result.Files[relative] = entry.exists
                        ? new DesiredFile { Target = relative, Exists = true, SourcePath = FullChild(BackupRoot, entry.backupPath), Sha256 = entry.sha256 }
                        : new DesiredFile { Target = relative, Exists = false };
                }
                else
                {
                    string path = FullChild(root, relative);
                    FileState state = GetFileState(path);
                    result.Files[relative] = new DesiredFile { Target = relative, Exists = state.Exists, SourcePath = state.Exists ? path : null, Sha256 = state.Sha256 };
                }
            }
            foreach (var component in Manifest.components.Where(c => c.handler == "fileSet" && selected.Contains(c.id, StringComparer.OrdinalIgnoreCase)))
                foreach (var file in component.files)
                    result.Files[file.target] = BuildPatchedDesired(root, result.Files[file.target], file, file.patchedSha256);

            ModelStateSpec baseline = SnapshotModelState(root, backup);
            bool hit = selected.Contains("claws-hit-fix", StringComparer.OrdinalIgnoreCase) || baseline.hitFix;
            string block = !Eq(blockStyle, "None") ? blockStyle : baseline.blockStyle;
            ModelStateSpec model = Manifest.modelSystem.states.SingleOrDefault(s => s.hitFix == hit && Eq(s.blockStyle, block));
            if (model == null) throw new InvalidDataException("Unsupported Claws model state.");
            foreach (string relative in Manifest.modelSystem.managedTargets)
                result.Files[relative] = new DesiredFile { Target = relative, Exists = false };
            foreach (var file in model.files)
                result.Files[file.target] = BuildPatchedDesired(root, null, file, file.sha256);
            result.EffectiveHitFix = hit;
            result.EffectiveBlockStyle = block;

            ComponentSpec alias = Manifest.components.First(c => c.handler == "textAlias");
            if (selected.Contains(alias.id, StringComparer.OrdinalIgnoreCase))
            {
                DesiredFile baseFile = result.Files[alias.target];
                byte[] bytes = baseFile.Bytes ?? File.ReadAllBytes(baseFile.SourcePath);
                byte[] patched = SetAlias(bytes, alias);
                result.Files[alias.target] = new DesiredFile { Target = alias.target, Exists = true, Bytes = patched, Sha256 = HashBytes(patched) };
            }
            return result;
        }

        private DesiredFile BuildPatchedDesired(string root, DesiredFile baseFile, FileSpec file, string expectedHash)
        {
            byte[] bytes;
            if (PatchEngine.IsBinaryRecipe(file.recipe))
            {
                if (baseFile == null || !baseFile.Exists) throw new FileNotFoundException("Required source file is missing: " + file.target);
                byte[] source = baseFile.Bytes ?? File.ReadAllBytes(baseFile.SourcePath);
                bytes = PatchEngine.ApplyBinary(file.recipe, source);
            }
            else if (PatchEngine.IsModelRecipe(file.recipe)) bytes = PatchEngine.BuildModel(root, file.recipe);
            else throw new InvalidDataException("Unknown patch recipe: " + file.recipe);
            string hash = HashBytes(bytes);
            if (!Eq(hash, expectedHash)) throw new InvalidDataException("Generated file checksum mismatch: " + file.target);
            return new DesiredFile { Target = file.target, Exists = true, Bytes = bytes, Sha256 = hash };
        }

        private DesiredMap BuildSubtitleDesired(string root, BackupManifest backup)
        {
            var result = new DesiredMap();
            string source;
            if (backup != null)
            {
                BackupEntry entry = BackupLookup(backup)[Manifest.target.relativePath];
                if (!entry.exists) throw new InvalidDataException("The persistent backup does not contain Bin\\engine.dll.");
                source = FullChild(BackupRoot, entry.backupPath);
            }
            else source = FullChild(root, Manifest.target.relativePath);
            byte[] bytes = PatchEngine.ApplyBinary(Manifest.target.recipe, File.ReadAllBytes(source));
            string hash = HashBytes(bytes);
            if (!Eq(hash, Manifest.target.patchedSha256)) throw new InvalidDataException("Generated Bin\\engine.dll checksum mismatch.");
            result.Files[Manifest.target.relativePath] = new DesiredFile { Target = Manifest.target.relativePath, Exists = true, Bytes = bytes, Sha256 = hash };
            return result;
        }

        private string NewTransaction(string root, DesiredMap desired, IProgressSink progress)
        {
            string transaction = Path.Combine(PackageRoot, ".transaction-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(transaction);
            try
            {
                var manifest = new TransactionManifest { formatVersion = 1, packageId = Manifest.packageId, gameRoot = root, createdUtc = DateTime.UtcNow.ToString("o"), entries = new List<TransactionEntry>() };
                var ordered = desired.Files.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    string relative = ordered[i];
                    string target = FullChild(root, relative);
                    FileState current = GetFileState(target);
                    DesiredFile wanted = desired.Files[relative];
                    if (current.Exists == wanted.Exists && (!current.Exists || Eq(current.Sha256, wanted.Sha256))) continue;
                    var entry = new TransactionEntry { target = relative.Replace('\\', '/'), beforeExists = current.Exists, beforeSha256 = current.Sha256, afterExists = wanted.Exists, afterSha256 = wanted.Sha256 };
                    if (current.Exists)
                    {
                        entry.beforePath = "Before/" + entry.target;
                        string before = FullChild(transaction, entry.beforePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(before));
                        File.Copy(target, before, false);
                        if (!Eq(HashFile(before), current.Sha256)) throw new IOException("Transaction snapshot failed: " + relative);
                    }
                    if (wanted.Exists)
                    {
                        entry.afterPath = "After/" + entry.target;
                        string after = FullChild(transaction, entry.afterPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(after));
                        if (wanted.Bytes != null) File.WriteAllBytes(after, wanted.Bytes); else File.Copy(wanted.SourcePath, after, false);
                        if (!Eq(HashFile(after), wanted.Sha256)) throw new IOException("Staged mod verification failed: " + relative);
                    }
                    manifest.entries.Add(entry);
                    progress.Report(36 + ((i + 1) * 14 / Math.Max(1, ordered.Count)), "Preparing changes...");
                }
                WriteJson(Path.Combine(transaction, "recovery.json"), manifest);
                return transaction;
            }
            catch
            {
                if (Directory.Exists(transaction)) RemovePackageDirectory(transaction);
                throw;
            }
        }

        private TransactionManifest ReadTransaction(string transaction)
        {
            string path = Path.Combine(transaction, "recovery.json");
            if (!File.Exists(path)) throw new InvalidDataException("Incomplete transaction: " + transaction);
            TransactionManifest value = ReadJson<TransactionManifest>(path);
            // rc.1/rc.2 Protean transactions did not include packageId.  Accept that
            // exact legacy shape so an interrupted old installer can still recover.
            if (value == null || value.formatVersion != 1 || (!string.IsNullOrEmpty(value.packageId) && value.packageId != Manifest.packageId))
                throw new InvalidDataException("Unsupported transaction: " + transaction);
            return value;
        }

        private void RestoreTransaction(string transaction)
        {
            TransactionManifest recovery = ReadTransaction(transaction);
            string root = ResolveGameRoot(recovery.gameRoot);
            AssertGameClosed();
            foreach (var entry in recovery.entries)
            {
                string target = FullChild(root, entry.target);
                if (entry.beforeExists)
                {
                    string source = FullChild(transaction, entry.beforePath);
                    if (!File.Exists(source) || !Eq(HashFile(source), entry.beforeSha256)) throw new InvalidDataException("Transaction snapshot is damaged: " + entry.target);
                    CopyReplacing(source, target);
                    if (!Eq(HashFile(target), entry.beforeSha256)) throw new IOException("Transaction recovery failed: " + entry.target);
                }
                else if (File.Exists(target)) { ClearReadOnly(target); File.Delete(target); }
            }
        }

        public void RecoverPendingTransactions()
        {
            foreach (string transaction in Directory.GetDirectories(PackageRoot, ".transaction-*", SearchOption.TopDirectoryOnly))
            {
                RestoreTransaction(transaction);
                RemovePackageDirectory(transaction);
            }
        }

        private void ApplyTransaction(string transaction, IProgressSink progress)
        {
            TransactionManifest recovery = ReadTransaction(transaction);
            string root = ResolveGameRoot(recovery.gameRoot);
            try
            {
                for (int i = 0; i < recovery.entries.Count; i++)
                {
                    TransactionEntry entry = recovery.entries[i];
                    string target = FullChild(root, entry.target);
                    if (entry.afterExists)
                    {
                        string source = FullChild(transaction, entry.afterPath);
                        if (!File.Exists(source) || !Eq(HashFile(source), entry.afterSha256)) throw new InvalidDataException("Staged mod is damaged: " + entry.target);
                        AtomicReplace(source, target, ".vtmb-installer-");
                        if (!Eq(HashFile(target), entry.afterSha256)) throw new IOException("Post-install verification failed: " + entry.target);
                    }
                    else if (File.Exists(target)) { ClearReadOnly(target); File.Delete(target); }
                    progress.Report(50 + ((i + 1) * 38 / Math.Max(1, recovery.entries.Count)), "Applying changes...");
                }
            }
            catch
            {
                RestoreTransaction(transaction);
                throw;
            }
        }

        private void SaveState(string root, DesiredMap desired, List<string> selected, string blockStyle, bool backupUsed, bool restored)
        {
            var state = new InstallState
            {
                formatVersion = 1, packageId = Manifest.packageId, packageVersion = Manifest.packageVersion,
                gameRoot = root, updatedUtc = DateTime.UtcNow.ToString("o"), backupUsed = backupUsed, restored = restored,
                selectedComponents = selected ?? new List<string>(), requestedBlockStyle = blockStyle ?? "None",
                effectiveHitFix = desired.EffectiveHitFix, effectiveBlockStyle = desired.EffectiveBlockStyle,
                expectedFiles = desired.Files.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new ExpectedEntry { target = x.Key.Replace('\\', '/'), exists = x.Value.Exists, sha256 = x.Value.Sha256 }).ToList()
            };
            WriteJson(StatePath, state);
        }

        public List<string> DefaultComponents()
        {
            return IsProtean ? Manifest.components.Where(c => c.defaultSelected).Select(c => c.id).ToList() : new List<string>();
        }

        public void Install(string gameRoot, List<string> selected, string blockStyle, bool useBackup, IProgressSink progress)
        {
            progress = progress ?? new NullProgress();
            progress.Report(1, "Checking game...");
            AssertGameClosed();
            string root = ResolveGameRoot(gameRoot);
            AssertGameLayout(root);
            VerifyPackage(progress);
            InstallState state = ReadState();
            if (state != null)
            {
                if (!Eq(state.gameRoot, root)) throw new InvalidDataException("Installer state belongs to a different game folder.");
                AssertCurrentMatches(root, state.expectedFiles);
                AssertNoLegacyProteanState(root);
                if (!state.backupUsed && IsProtean && (!SequenceEqualIgnoreCase(state.selectedComponents, selected) || !Eq(state.requestedBlockStyle, blockStyle)))
                    throw new InvalidOperationException("Components cannot be modified because the package was installed without a persistent backup.");
                // A backup created after the first installation would contain modded
                // files and could not restore the original state.
                if (!state.backupUsed) useBackup = false;
            }
            else
            {
                AssertNoLegacyProteanState(root);
                AssertKnownLiveState(root, false);
                if (IsProtean && !HasBackup && HasProteanChanges(root)) useBackup = false;
            }
            progress.Report(16, "Checking current state...");

            // A pre-patched Subtitle DLL may come from a manual install or another copy of
            // this package.  It is supported, but must never become its own "original"
            // persistent backup because that would make Restore meaningless.
            if (!IsProtean)
            {
                FileState currentEngine = GetFileState(FullChild(root, Manifest.target.relativePath));
                if (currentEngine.Size == Manifest.target.patchedSize && Eq(currentEngine.Sha256, Manifest.target.patchedSha256))
                {
                    BackupManifest existingBackup = HasBackup ? ReadBackup(root) : null;
                    if (existingBackup != null)
                        SaveState(root, BuildSubtitleDesired(root, existingBackup), new List<string>(), "None", true, false);
                    progress.Report(100, "Installation Complete.");
                    return;
                }
            }

            BackupManifest backup = HasBackup ? ReadBackup(root) : (useBackup ? CreateBackup(root, progress) : null);
            DesiredMap desired;
            if (IsProtean)
            {
                ValidateSelection(selected, blockStyle);
                desired = BuildProteanDesired(root, selected, blockStyle, backup);
            }
            else
            {
                desired = BuildSubtitleDesired(root, backup);
            }
            string transaction = NewTransaction(root, desired, progress);
            try
            {
                ApplyTransaction(transaction, progress);
                progress.Report(92, "Verifying installation...");
                AssertCurrentMatches(root, desired.Files.Select(x => new ExpectedEntry { target = x.Key, exists = x.Value.Exists, sha256 = x.Value.Sha256 }));
                SaveState(root, desired, selected, blockStyle, backup != null, false);
                RemovePackageDirectory(transaction);
                progress.Report(100, "Installation Complete.");
            }
            catch { throw; }
        }

        public void Restore(string gameRoot, IProgressSink progress)
        {
            progress = progress ?? new NullProgress();
            progress.Report(2, "Checking persistent backup...");
            AssertGameClosed();
            string root = ResolveGameRoot(gameRoot);
            AssertGameLayout(root);
            BackupManifest backup = ReadBackup(root);
            InstallState state = ReadState();
            if (state != null) AssertCurrentMatches(root, state.expectedFiles); else AssertKnownLiveState(root, true);
            var desired = new DesiredMap();
            foreach (var entry in backup.files)
                desired.Files[entry.target] = entry.exists
                    ? new DesiredFile { Target = entry.target, Exists = true, SourcePath = FullChild(BackupRoot, entry.backupPath), Sha256 = entry.sha256 }
                    : new DesiredFile { Target = entry.target, Exists = false };
            if (IsProtean)
            {
                var model = SnapshotModelState(root, backup);
                desired.EffectiveHitFix = model.hitFix;
                desired.EffectiveBlockStyle = model.blockStyle;
            }
            string transaction = NewTransaction(root, desired, progress);
            ApplyTransaction(transaction, progress);
            progress.Report(92, "Verifying restored files...");
            AssertCurrentMatches(root, desired.Files.Select(x => new ExpectedEntry { target = x.Key, exists = x.Value.Exists, sha256 = x.Value.Sha256 }));
            SaveState(root, desired, new List<string>(), "None", true, true);
            RemovePackageDirectory(transaction);
            progress.Report(100, "Restore Complete.");
        }

        private void ValidateSelection(List<string> selected, string blockStyle)
        {
            var valid = new HashSet<string>(Manifest.components.Select(c => c.id), StringComparer.OrdinalIgnoreCase);
            foreach (string value in selected) if (!valid.Contains(value)) throw new InvalidDataException("Unknown component: " + value);
            if (!new[] { "None", "Fists", "TireIron" }.Contains(blockStyle, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("Unknown block style: " + blockStyle);
        }

        private bool SequenceEqualIgnoreCase(List<string> left, List<string> right)
        {
            left = left ?? new List<string>(); right = right ?? new List<string>();
            return left.Count == right.Count && left.All(x => right.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        public string Status(string gameRoot)
        {
            string root = ResolveGameRoot(gameRoot);
            AssertGameLayout(root);
            if (!IsProtean)
            {
                FileState state = GetFileState(FullChild(root, Manifest.target.relativePath));
                string name = state.Size == Manifest.target.originalSize && Eq(state.Sha256, Manifest.target.originalSha256) ? "Original" :
                              state.Size == Manifest.target.patchedSize && Eq(state.Sha256, Manifest.target.patchedSha256) ? "Installed" : "Unsupported";
                return string.Format("Game root: {0}\r\nSubtitle Pause Fix: {1}\r\nPersistent backup: {2}", root, name, HasBackup);
            }
            ModelStateSpec model = DetectModelState(t => GetFileState(FullChild(root, t)));
            bool legacy = Manifest.components.Where(c => c.handler == "fileSet").SelectMany(c => c.files)
                .Any(f => IsLegacyFileState(f, HashFile(FullChild(root, f.target))));
            return string.Format("Game root: {0}\r\nModel state: Hit Fix={1}, Block={2}\r\nLegacy file state: {3}\r\nPersistent backup: {4}", root, model.hitFix, model.blockStyle, legacy, HasBackup);
        }

        public string SuggestedGameRoot()
        {
            if (HasBackup) { try { return ReadBackup(null).gameRoot; } catch { } }
            string env = Environment.GetEnvironmentVariable("VTMB_GAME_ROOT");
            if (!string.IsNullOrEmpty(env) && File.Exists(Path.Combine(env, "Vampire.exe"))) return env;
            string[] candidates = {
                @"D:\Games\Vampire - The Masquerade Bloodlines",
                @"C:\Program Files (x86)\Steam\steamapps\common\Vampire The Masquerade - Bloodlines",
                @"C:\GOG Games\Vampire The Masquerade - Bloodlines"
            };
            return candidates.FirstOrDefault(x => File.Exists(Path.Combine(x, "Vampire.exe"))) ?? "";
        }

        public InstallState CurrentState() { try { return ReadState(); } catch { return null; } }
    }

    public sealed class DetailsDialog : Form
    {
        public DetailsDialog(string message, Exception exception)
        {
            Text = "Installation stopped safely";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 172);
            Padding = new Padding(16);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var icon = new PictureBox { Image = SystemIcons.Error.ToBitmap(), SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(18, 22) };
            var text = new Label { Text = message, AutoSize = false, Location = new Point(66, 18), Size = new Size(430, 82) };
            var copy = new Button { Text = "Copy details", Size = new Size(105, 30), Location = new Point(278, 122) };
            var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(105, 30), Location = new Point(391, 122) };
            copy.Click += delegate
            {
                try { Clipboard.SetText(exception.ToString()); copy.Text = "Copied"; }
                catch { copy.Text = "Copy failed"; }
            };
            Controls.AddRange(new Control[] { icon, text, copy, close });
            AcceptButton = close;
            CancelButton = close;
        }
    }

    public sealed class InstallerForm : Form
    {
        private readonly InstallerCore core;
        private readonly TextBox gameRoot = new TextBox();
        private readonly RadioButton installAction = new RadioButton();
        private readonly RadioButton restoreAction = new RadioButton();
        private readonly CheckBox persistentBackup = new CheckBox();
        private readonly Dictionary<string, CheckBox> components = new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RadioButton> blockOptions = new Dictionary<string, RadioButton>(StringComparer.OrdinalIgnoreCase);
        private readonly Label griffithWarning = new Label();
        private readonly Label backupState = new Label();
        private readonly Label stage = new Label();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Button run = new Button();
        private readonly Button browse = new Button();
        private bool busy;

        public InstallerForm(InstallerCore installerCore)
        {
            core = installerCore;
            Text = core.Manifest.packageName + " " + core.Manifest.packageVersion;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(640, core.IsProtean ? 635 : 405);
            Padding = new Padding(18);

            int y = 18;
            AddLabel("Game folder", 18, y, 590, 20, FontStyle.Bold);
            y += 25;
            gameRoot.SetBounds(18, y, 500, 27);
            gameRoot.Text = core.SuggestedGameRoot();
            browse.Text = "Browse...";
            browse.SetBounds(526, y - 1, 95, 29);
            browse.Click += BrowseClick;
            Controls.Add(gameRoot); Controls.Add(browse);
            y += 46;

            AddLabel("Action", 18, y, 590, 20, FontStyle.Bold);
            y += 24;
            var actionPanel = new Panel();
            actionPanel.SetBounds(18, y, 590, 28);
            installAction.Text = "Install";
            installAction.Checked = true;
            installAction.SetBounds(2, 0, 210, 24);
            restoreAction.Text = "Restore persistent backup";
            restoreAction.SetBounds(227, 0, 260, 24);
            installAction.CheckedChanged += delegate { RefreshActionControls(); };
            restoreAction.CheckedChanged += delegate { RefreshActionControls(); };
            actionPanel.Controls.Add(installAction); actionPanel.Controls.Add(restoreAction);
            Controls.Add(actionPanel);
            y += 42;

            if (core.IsProtean)
            {
                AddLabel("Components", 18, y, 590, 20, FontStyle.Bold);
                y += 25;
                foreach (ComponentSpec component in core.Manifest.components)
                {
                    var box = new CheckBox { Text = component.displayName, Checked = component.defaultSelected, AutoSize = false };
                    box.SetBounds(21, y, 585, 23);
                    components[component.id] = box;
                    Controls.Add(box);
                    y += 26;
                    if (!string.IsNullOrEmpty(component.warning))
                    {
                        griffithWarning.Text = component.warning;
                        griffithWarning.ForeColor = Color.DarkOrange;
                        griffithWarning.SetBounds(44, y - 2, 560, 23);
                        griffithWarning.Visible = box.Checked;
                        box.CheckedChanged += delegate { griffithWarning.Visible = box.Checked; };
                        Controls.Add(griffithWarning);
                        y += 25;
                    }
                }

                AddLabel(core.Manifest.blockGroup.displayName, 18, y, 590, 20, FontStyle.Bold);
                y += 24;
                var blockPanel = new Panel();
                blockPanel.SetBounds(18, y, 590, 28);
                int x = 3;
                foreach (BlockOptionSpec option in core.Manifest.blockGroup.options)
                {
                    var radio = new RadioButton { Text = option.displayName, Checked = string.Equals(option.id, core.Manifest.blockGroup.@default, StringComparison.OrdinalIgnoreCase), AutoSize = true };
                    radio.Location = new Point(x, 0);
                    blockOptions[option.id] = radio;
                    blockPanel.Controls.Add(radio);
                    x += Math.Max(145, TextRenderer.MeasureText(option.displayName, Font).Width + 38);
                }
                Controls.Add(blockPanel);
                y += 42;
            }

            persistentBackup.Text = "Create persistent backup (recommended)";
            persistentBackup.Checked = true;
            persistentBackup.SetBounds(20, y, 360, 24);
            Controls.Add(persistentBackup);
            backupState.SetBounds(390, y + 2, 230, 21);
            backupState.TextAlign = ContentAlignment.TopRight;
            Controls.Add(backupState);
            y += 42;

            stage.Text = "Ready";
            stage.SetBounds(18, y, 603, 22);
            Controls.Add(stage);
            y += 24;
            progress.SetBounds(18, y, 603, 20);
            progress.Minimum = 0; progress.Maximum = 100;
            Controls.Add(progress);
            y += 39;

            run.Text = "Install";
            run.SetBounds(496, y, 125, 34);
            run.Click += RunClick;
            Controls.Add(run);
            AcceptButton = run;
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (busy) { e.Cancel = true; System.Media.SystemSounds.Beep.Play(); }
            };
            RefreshState();
        }

        private Label AddLabel(string text, int x, int y, int width, int height, FontStyle style)
        {
            var label = new Label { Text = text, AutoSize = false, Font = new Font(Font, style) };
            label.SetBounds(x, y, width, height); Controls.Add(label); return label;
        }

        private void BrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "Select the Vampire: The Masquerade - Bloodlines folder", SelectedPath = gameRoot.Text, ShowNewFolderButton = false })
                if (dialog.ShowDialog(this) == DialogResult.OK) gameRoot.Text = dialog.SelectedPath;
        }

        private void RefreshState()
        {
            bool backup = core.HasBackup;
            InstallState state = core.CurrentState();
            restoreAction.Enabled = backup;
            backupState.Text = backup ? "Persistent backup available" :
                (state != null && !state.backupUsed ? "Persistent backup unavailable" : "No persistent backup");
            installAction.Text = backup ? "Install / Modify" : "Install";
            if (!backup && restoreAction.Checked) installAction.Checked = true;

            if (core.IsProtean && state != null && !state.restored)
            {
                foreach (var item in components) item.Value.Checked = state.selectedComponents != null && state.selectedComponents.Contains(item.Key, StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(state.requestedBlockStyle) && blockOptions.ContainsKey(state.requestedBlockStyle)) blockOptions[state.requestedBlockStyle].Checked = true;
            }
            RefreshActionControls();
        }

        private void RefreshActionControls()
        {
            bool installing = installAction.Checked;
            InstallState state = core.CurrentState();
            persistentBackup.Enabled = installing && !core.HasBackup && state == null && !busy;
            foreach (CheckBox box in components.Values) box.Enabled = installing && !busy;
            foreach (RadioButton radio in blockOptions.Values) radio.Enabled = installing && !busy;
            gameRoot.Enabled = !busy; browse.Enabled = !busy;
            run.Enabled = !busy && (installing || core.HasBackup);
            run.Text = installing ? (core.HasBackup ? "Install / Modify" : "Install") : "Restore";
        }

        private List<string> SelectedComponents()
        {
            return components.Where(x => x.Value.Checked).Select(x => x.Key).ToList();
        }

        private string SelectedBlockStyle()
        {
            var selected = blockOptions.FirstOrDefault(x => x.Value.Checked);
            return string.IsNullOrEmpty(selected.Key) ? "None" : selected.Key;
        }

        private void SetBusy(bool value)
        {
            busy = value;
            installAction.Enabled = !value;
            restoreAction.Enabled = !value && core.HasBackup;
            RefreshActionControls();
        }

        private async void RunClick(object sender, EventArgs e)
        {
            if (busy) return;
            string root = gameRoot.Text.Trim();
            bool installing = installAction.Checked;
            bool useBackup = persistentBackup.Checked;
            List<string> selected = SelectedComponents();
            string blockStyle = SelectedBlockStyle();
            progress.Value = 0; stage.Text = "Starting..."; SetBusy(true);
            var sink = new DelegateProgress(delegate(int percent, string text)
            {
                if (!IsDisposed) BeginInvoke((Action)delegate { progress.Value = Math.Max(0, Math.Min(100, percent)); stage.Text = text; });
            });
            try
            {
                await Task.Factory.StartNew(delegate
                {
                    if (installing) core.Install(root, selected, blockStyle, useBackup, sink);
                    else core.Restore(root, sink);
                });
                RefreshState();
                stage.Text = installing ? "Installation Complete." : "Restore Complete.";
                progress.Value = 100;
                MessageBox.Show(this, stage.Text, core.Manifest.packageName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                Exception actual = exception is AggregateException ? ((AggregateException)exception).Flatten().InnerExceptions.First() : exception;
                progress.Value = 0; stage.Text = "Stopped safely";
                using (var dialog = new DetailsDialog(FriendlyMessage(actual), actual)) dialog.ShowDialog(this);
            }
            finally { SetBusy(false); RefreshState(); }
        }

        private static string FriendlyMessage(Exception exception)
        {
            if (exception is UnauthorizedAccessException) return "Windows denied access to a game file. Close the game and check the folder permissions, then try again.";
            if (exception is IOException) return "A game file could not be updated. Close the game and any tools using its files, then try again.";
            return exception.Message;
        }
    }

    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AttachConsole(uint processId);
        private const uint AttachParentProcess = 0xFFFFFFFF;

        [STAThread]
        public static int Main(string[] args)
        {
            var core = new InstallerCore();
            if (args.Length == 0)
            {
                try { core.RecoverPendingTransactions(); }
                catch (Exception ex) { Application.EnableVisualStyles(); using (var d = new DetailsDialog(ex.Message, ex)) d.ShowDialog(); return 1; }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new InstallerForm(core));
                return 0;
            }

            AttachOutput();
            try
            {
                var parsed = Parse(args);
                string mode = parsed.ContainsKey("mode") ? parsed["mode"] : "";
                bool quiet = parsed.ContainsKey("quiet");
                string root = Value(parsed, "game-root");
                if (mode != "verify") core.RecoverPendingTransactions();
                var sink = quiet ? (IProgressSink)new NullProgress() : new DelegateProgress((p, s) => Console.WriteLine("{0,3}%  {1}", p, s));
                if (mode == "verify") { core.VerifyPackage(sink); if (!quiet) Console.WriteLine("Package verification passed."); }
                else if (mode == "status") Console.WriteLine(core.Status(root));
                else if (mode == "install")
                {
                    List<string> selected = parsed.ContainsKey("components")
                        ? parsed["components"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList()
                        : core.DefaultComponents();
                    core.Install(root, selected, Value(parsed, "block-style") ?? "None", !parsed.ContainsKey("no-backup"), sink);
                    if (quiet) Console.WriteLine("Installation Complete.");
                }
                else if (mode == "restore") { core.Restore(root, sink); if (quiet) Console.WriteLine("Restore Complete."); }
                else throw new ArgumentException("Choose --verify, --status, --install, or --restore.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }

        private static Dictionary<string, string> Parse(string[] args)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("--")) throw new ArgumentException("Unknown argument: " + arg);
                string key = arg.Substring(2);
                if (new[] { "verify", "status", "install", "restore" }.Contains(key, StringComparer.OrdinalIgnoreCase)) { result["mode"] = key.ToLowerInvariant(); continue; }
                if (new[] { "quiet", "no-backup" }.Contains(key, StringComparer.OrdinalIgnoreCase)) { result[key] = "true"; continue; }
                if (!new[] { "game-root", "components", "block-style" }.Contains(key, StringComparer.OrdinalIgnoreCase) || i + 1 >= args.Length)
                    throw new ArgumentException("Unknown or incomplete argument: " + arg);
                result[key] = args[++i];
            }
            return result;
        }

        private static string Value(Dictionary<string, string> values, string key) { return values.ContainsKey(key) ? values[key] : null; }

        private static void AttachOutput()
        {
            try
            {
                AttachConsole(AttachParentProcess);
                var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetOut(stdout); Console.SetError(stderr);
            }
            catch { }
        }
    }
}
