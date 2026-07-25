using System.Text.Json;

namespace VocaNova.API.Features.SuperAdmin.Services;

public sealed class JsonAdminUserAssignmentStore : IAdminUserAssignmentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public JsonAdminUserAssignmentStore(IHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "Data", "admin-user-assignments.json");
    }

    public async Task<IReadOnlyDictionary<uint, IReadOnlyCollection<uint>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadUnsafeAsync(cancellationToken);
            return data.Assignments.ToDictionary(
                item => item.Key,
                item => (IReadOnlyCollection<uint>)item.Value.Distinct().Order().ToArray());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<uint>> GetUserIdsAsync(
        uint adminId,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.TryGetValue(adminId, out var ids) ? ids : [];
    }

    public async Task ReplaceAsync(
        uint adminId,
        IReadOnlyCollection<uint> userIds,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadUnsafeAsync(cancellationToken);
            var selected = userIds.Distinct().Order().ToArray();

            foreach (var key in data.Assignments.Keys.ToArray())
            {
                if (key == adminId) continue;
                data.Assignments[key].RemoveAll(id => selected.Contains(id));
                if (data.Assignments[key].Count == 0) data.Assignments.Remove(key);
            }

            if (selected.Length == 0) data.Assignments.Remove(adminId);
            else data.Assignments[adminId] = selected.ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(data, JsonOptions), cancellationToken);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AssignmentFile> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath)) return new AssignmentFile();
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<AssignmentFile>(stream, JsonOptions, cancellationToken)
            ?? new AssignmentFile();
    }

    private sealed class AssignmentFile
    {
        public Dictionary<uint, List<uint>> Assignments { get; set; } = [];
    }
}
