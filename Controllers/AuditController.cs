using AuditService.Data;
using AuditService.Dtos;
using AuditService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AuditService.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AuditDbContext _db;

    public AuditController(AuditDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAuditTrailRequest request)
    {
        var audit = new AuditTrail
        {
            TableName = request.TableName,
            RecordId = request.RecordId,
            Action = request.Action,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            ChangedBy = request.ChangedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            AppContext = request.AppContext
        };

        _db.AuditTrails.Add(audit);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = audit.AuditId },
            audit);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var audit = await _db.AuditTrails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuditId == id);

        if (audit is null)
            return NotFound();

        return Ok(audit);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
    [FromQuery] string tableName,
    [FromQuery] string recordId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _db.AuditTrails
            .AsNoTracking()
            .Where(x =>
                x.TableName == tableName &&
                x.RecordId == recordId)
            .OrderByDescending(x => x.ChangedAt);

        var audits = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditTrailResponse(
                x.AuditId,
                x.TableName,
                x.RecordId,
                x.Action,
                x.OldValues,
                x.NewValues,
                x.ChangedBy,
                x.ChangedAt))
            .ToListAsync();

        return Ok(audits);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(
    IEnumerable<CreateAuditTrailRequest> requests)
    {
        var audits = requests.Select(request => new AuditTrail
        {
            TableName = request.TableName,
            RecordId = request.RecordId,
            Action = request.Action,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            ChangedBy = request.ChangedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            AppContext = request.AppContext
        });

        await _db.AuditTrails.AddRangeAsync(audits);

        await _db.SaveChangesAsync();

        return Ok();
    }

    // Optimize for large batch sizes
    [HttpPost("batch-optimized")]
    public async Task<IActionResult> CreateBatchOptimized(
    IEnumerable<CreateAuditTrailRequest> requests)
    {
        const int batchSize = 10_000;

        foreach (var batch in requests.Chunk(batchSize))
        {
            var audits = batch.Select(request => new AuditTrail
            {
                TableName = request.TableName,
                RecordId = request.RecordId,
                Action = request.Action,
                OldValues = request.OldValues,
                NewValues = request.NewValues,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow,
                IpAddress = request.IpAddress,
                AppContext = request.AppContext
            });

            await _db.AuditTrails.AddRangeAsync(audits);

            await _db.SaveChangesAsync();

            _db.ChangeTracker.Clear();
        }

        return Ok();
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var stopwatch = Stopwatch.StartNew();

        var audits = await _db.AuditTrails
            .ToListAsync();

        stopwatch.Stop();

        Console.WriteLine(
            $"Query execution time: {stopwatch.ElapsedMilliseconds} ms");

        return Ok(audits);
    }


    [EnableRateLimiting("audit")]
    [HttpGet("all-optimized")]
    public async Task<IActionResult> GetAllOptimized(string tableName, string recordId, long? beforeId)
    {
        var stopwatch = Stopwatch.StartNew();

        var query = _db.AuditTrails
            .AsNoTracking()
            .Where(x => x.TableName == tableName && x.RecordId == recordId);

        if (beforeId.HasValue)
        {
            query = query.Where(x => x.AuditId < beforeId.Value);
        }

        var audits = await query
            .OrderByDescending(x => x.AuditId)
            .Take(100)
            .ToListAsync();

        stopwatch.Stop();

        Console.WriteLine($"Query execution time: {stopwatch.ElapsedMilliseconds} ms");

        return Ok(audits);
    }

    //Filtering
    [HttpGet("filtering")]
    public async Task<IActionResult> Filtering(string tableName, string recordId)
    {
        var audits = await _db.AuditTrails
            .Where(x => x.TableName == "orders" && x.RecordId == "12345")
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();

        if (!audits.Any())
            return NotFound();

        return Ok(audits);
    }

    //EXAMPLE
    [HttpGet("example")]
    public async Task<IActionResult> Example(string tableName, string recordId, long? beforeId)
    {
        var query = _db.AuditTrails
            .AsNoTracking()
            .Where(x => x.TableName == tableName && x.RecordId == recordId);

        if (beforeId.HasValue)
        {
            query = query.Where(x => x.AuditId < beforeId.Value);
        }

        var audits = await query
            .OrderByDescending(x => x.AuditId)
            .Take(100)
            .ToListAsync();

        return Ok(audits);
    }

    [HttpPost("import-simple")]
    public async Task<IActionResult> Import(IEnumerable<CreateAuditTrailRequest> requests)
    {
        foreach (var request in requests)
        {
            var audit = new AuditTrail
            {
                TableName = request.TableName,
                RecordId = request.RecordId,
                Action = request.Action,
                OldValues = request.OldValues,
                NewValues = request.NewValues,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow,
                IpAddress = request.IpAddress,
                AppContext = request.AppContext
            };

            _db.AuditTrails.Add(audit);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("import-batch")]
    public async Task<IActionResult> ImportBatch(IEnumerable<CreateAuditTrailRequest> requests)
    {
        const int batchSize = 10_000;

        foreach (var batch in requests.Chunk(batchSize))
        {
            var audits = batch.Select(CreateAuditTrail);

            await _db.AuditTrails.AddRangeAsync(audits);

            await _db.SaveChangesAsync();

            _db.ChangeTracker.Clear();
        }

        return Ok();
    }

    [HttpPost("import-batch-by-transaction")]
    public async Task<IActionResult> ImportBatchByTransaction(IEnumerable<CreateAuditTrailRequest> requests)
    {
        const int batchSize = 10_000;

        foreach (var batch in requests.Chunk(batchSize))
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    var audits = batch.Select(CreateAuditTrail);

                    await _db.AuditTrails.AddRangeAsync(audits);

                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            _db.ChangeTracker.Clear();
        }

        return Ok();
    }

    private static AuditTrail CreateAuditTrail(
    CreateAuditTrailRequest request)
    {
        return new AuditTrail
        {
            TableName = request.TableName,
            RecordId = request.RecordId,
            Action = request.Action,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            ChangedBy = request.ChangedBy,
            ChangedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            AppContext = request.AppContext
        };
    }

    [HttpPost(template: "generate-and-import")]
    public async Task<IActionResult> GenerateAndImport()
    {

        var stopwatch = Stopwatch.StartNew();

        const int totalRecords = 1_000_000;

        var requests = GenerateAuditRequests(totalRecords);

        await ImportBatchesAsync(requests);

        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"Elapsed time: {elapsedSeconds}");

        return Ok();
    }

    private static IEnumerable<CreateAuditTrailRequest> GenerateAuditRequests(int totalRecords)
    {
        for (var i = 1; i <= totalRecords; i++)
        {
            yield return new CreateAuditTrailRequest(
                TableName: "orders",
                RecordId: i.ToString(),
                Action: "INSERT",
                OldValues: null,
                NewValues: """{"status":"created"}""",
                ChangedBy: $"user_{(i % 1000) + 1}",
                IpAddress: $"192.168.1.{(i % 254) + 1}",
                AppContext: """{"source":"performance-test"}"""
            );
        }
    }

    private async Task ImportBatchesAsync(IEnumerable<CreateAuditTrailRequest> requests)
    {
        const int batchSize = 10_000;

        foreach (var batch in requests.Chunk(batchSize))
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    var audits = batch.Select(CreateAuditTrail);

                    await _db.AuditTrails.AddRangeAsync(audits);

                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            _db.ChangeTracker.Clear();
        }
    }

    [HttpGet(template: "find-by-status")]
    public async Task<IActionResult> FindByStatus(
    [FromQuery] string oldStatus,
    [FromQuery] string newStatus)
    {
       var audits = await FindAuditsByStatus(oldStatus, newStatus);

        return Ok(audits);
    }

    private async Task<List<AuditTrailResponseByStatus>>FindAuditsByStatus(string oldStatus, string newStatus)
    {
        return await _db.AuditTrails
            .FromSqlRaw("""
                            SELECT audit_id, table_name, record_id, action,
                                   old_values, new_values, changed_by, changed_at,
                                   ip_address, app_context
                            FROM audit_trail
                            WHERE old_values->>'status' = {0}
                              AND new_values->>'status' = {1}
                            ORDER BY changed_at DESC
                            LIMIT 100
                            """, oldStatus, newStatus)
                            .AsNoTracking()
                            .Select(a => new AuditTrailResponseByStatus(
                                a.AuditId,
                                a.TableName,
                                a.RecordId,
                                a.Action,
                                a.ChangedBy,
                                a.ChangedAt))
                            .ToListAsync();
    }
}