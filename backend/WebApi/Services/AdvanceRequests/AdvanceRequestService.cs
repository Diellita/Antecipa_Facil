using Microsoft.EntityFrameworkCore;
using System.Security;
using WebApi.Data;
using WebApi.DTOs.AdvanceRequests;
using WebApi.Models;
using System.Linq;


namespace WebApi.Services.AdvanceRequests
{
    public class AdvanceRequestService : IAdvanceRequestService
    {
        private readonly AppDbContext _db;

        public AdvanceRequestService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AdvanceRequestDetailDto> CreateAsync(
            int contratoId,
            int? parcelaNumero,
            string? notes,
            string clientId,
            CancellationToken ct = default)
        {
            var cliente = await _db.Clientes
                .FirstOrDefaultAsync(c => c.Id.ToString() == clientId, ct);
            if (cliente == null)
                throw new SecurityException("Cliente não encontrado.");

            var contrato = await _db.Contratos
                .Include(c => c.Parcelas)
                .FirstOrDefaultAsync(c => c.Id == contratoId && c.ClienteId == cliente.Id, ct);
            if (contrato == null)
                throw new KeyNotFoundException("Contrato não encontrado para este cliente.");

            var hasPending = await _db.AdvanceRequests
                .AnyAsync(r => r.ContratoId == contrato.Id &&
                            r.Status == AdvanceRequestStatus.PENDENTE, ct);
            if (hasPending)
                throw new InvalidOperationException("Já existe solicitação pendente para este contrato.");

            var limite = DateTime.UtcNow.AddDays(30);
            var elegiveis = contrato.Parcelas
                .Where(p => p.Status == InstallmentStatus.A_VENCER && p.Vencimento > limite)
                .OrderBy(p => p.NumeroParcela)
                .ToList();

            if (!elegiveis.Any())
                throw new InvalidOperationException("Nenhuma parcela elegível para antecipação.");

            if (parcelaNumero.HasValue)
            {
                elegiveis = elegiveis
                    .Where(p => p.NumeroParcela == parcelaNumero.Value)
                    .ToList();

                if (elegiveis.Count == 0)
                    throw new InvalidOperationException("Parcela não elegível ou inexistente para este contrato.");
            }

            var request = new AdvanceRequest
            {
                ClienteId = cliente.Id,
                ContratoId = contrato.Id,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                Status = AdvanceRequestStatus.PENDENTE,
                Items = elegiveis.Select(p => new AdvanceRequestItem
                {
                    ParcelaId = p.Id,
                    ValorNaSolicitacao = p.Valor
                }).ToList()
            };

            foreach (var p in elegiveis)
                p.Status = InstallmentStatus.AGUARDANDO_APROVACAO;

            _db.AdvanceRequests.Add(request);
            await _db.SaveChangesAsync(ct);

            return new AdvanceRequestDetailDto
            {
                Id = request.Id,
                ClienteId = request.ClienteId,
                ContratoId = request.ContratoId,
                Status = request.Status,
                Notes = request.Notes,
                CreatedAt = request.CreatedAt,
                ApprovedAt = request.ApprovedAt,
                Items = request.Items.Select(i => new AdvanceRequestItemDto
                {
                    ParcelaId = i.ParcelaId,
                    ValorNaSolicitacao = i.ValorNaSolicitacao ?? 0
                }).ToList()
            };
        }

        public async Task<AdvanceRequestDetailDto?> GetByIdAsync(int id, string clientId, CancellationToken ct = default)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id.ToString() == clientId, ct);
            if (cliente == null)
                throw new SecurityException("Cliente não encontrado.");

            var request = await _db.AdvanceRequests
                .Include(r => r.Items).ThenInclude(i => i.Parcela)
                .FirstOrDefaultAsync(r => r.Id == id && r.ClienteId == cliente.Id, ct);

            if (request == null)
                return null;

            return new AdvanceRequestDetailDto
            {
                Id = request.Id,
                ClienteId = request.ClienteId,
                ContratoId = request.ContratoId,
                Status = request.Status,
                Notes = request.Notes,
                CreatedAt = request.CreatedAt,
                ApprovedAt = request.ApprovedAt,
                Items = request.Items.Select(i => new AdvanceRequestItemDto
                {
                    ParcelaId = i.ParcelaId,
                    ValorNaSolicitacao = i.ValorNaSolicitacao ?? 0
                }).ToList()
            };
        }

        public async Task<IEnumerable<AdvanceRequestDetailDto>> GetAdvanceRequestsAsync(string clientId, AdvanceRequestStatus? status, DateTime? startDate, DateTime? endDate, int page, int pageSize, CancellationToken ct = default)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id.ToString() == clientId, ct);
            if (cliente == null)
                throw new SecurityException("Cliente não encontrado.");

            var query = _db.AdvanceRequests
                .Include(r => r.Items).ThenInclude(i => i.Parcela)
                .Where(r => r.ClienteId == cliente.Id)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return requests.Select(r => new AdvanceRequestDetailDto
            {
                Id = r.Id,
                ClienteId = r.ClienteId,
                ContratoId = r.ContratoId,
                Status = r.Status,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                Items = r.Items.Select(i => new AdvanceRequestItemDto
                {
                    ParcelaId = i.ParcelaId,
                    ValorNaSolicitacao = i.ValorNaSolicitacao ?? 0
                }).ToList()
            });
        }

        public async Task ApproveAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0)
                throw new InvalidOperationException("Nenhuma solicitação informada para aprovação.");

            var requests = await _db.AdvanceRequests
                .Include(r => r.Items).ThenInclude(i => i.Parcela)
                .Where(r => idList.Contains(r.Id))
                .ToListAsync(ct);

            var notFound = idList.Except(requests.Select(r => r.Id)).ToList();
            if (notFound.Any())
                throw new KeyNotFoundException($"Solicitação(ões) não encontrada(s): {string.Join(", ", notFound)}");

            var errors = new List<string>();
            var nowUtc = DateTime.UtcNow;
            var minDue = nowUtc.AddDays(30);

            foreach (var r in requests)
            {
                if (r.Status != AdvanceRequestStatus.PENDENTE)
                {
                    errors.Add($"Solicitação {r.Id} não está PENDENTE (status atual: {r.Status}).");
                    continue;
                }

                var invalidInstallments = r.Items
                    .Select(i => i.Parcela)
                    .Where(p => p == null ||
                                p.Status != InstallmentStatus.AGUARDANDO_APROVACAO ||
                                p.Vencimento < minDue)
                    .Select(p => p?.Id.ToString() ?? "parcela nula")
                    .ToList();

                if (invalidInstallments.Any())
                    errors.Add($"Solicitação {r.Id} possui parcelas inválidas para aprovação: {string.Join(", ", invalidInstallments)}");
            }

            if (errors.Any())
                throw new InvalidOperationException(string.Join(" | ", errors));

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var r in requests)
                {
                    r.Status = AdvanceRequestStatus.APROVADO;
                    r.ApprovedAt = nowUtc;

                    foreach (var item in r.Items)
                    {
                        var parcela = item.Parcela;
                        if (parcela is null) continue;
                        parcela.Status = InstallmentStatus.ANTECIPADA;
                    }
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task RejectAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0)
                throw new InvalidOperationException("Nenhuma solicitação informada para rejeição.");

            var requests = await _db.AdvanceRequests
                .Include(r => r.Items).ThenInclude(i => i.Parcela)
                .Where(r => idList.Contains(r.Id))
                .ToListAsync(ct);

            var notFound = idList.Except(requests.Select(r => r.Id)).ToList();
            if (notFound.Any())
                throw new KeyNotFoundException($"Solicitação(ões) não encontrada(s): {string.Join(", ", notFound)}");

            var invalid = requests.Where(r => r.Status != AdvanceRequestStatus.PENDENTE).Select(r => r.Id).ToList();
            if (invalid.Any())
                throw new InvalidOperationException($"Só é possível rejeitar solicitações PENDENTE. Inválidas: {string.Join(", ", invalid)}");

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var r in requests)
                {
                    r.Status = AdvanceRequestStatus.REPROVADO;
                    r.ApprovedAt = null; 

                    foreach (var item in r.Items)
                    {
                        var parcela = item.Parcela;
                        if (parcela is null) continue;

                        if (parcela.Status == InstallmentStatus.AGUARDANDO_APROVACAO)
                            parcela.Status = InstallmentStatus.A_VENCER;
                    }
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IEnumerable<AdvanceRequestDetailDto>> GetAdvanceRequestsAdminAsync(AdvanceRequestStatus? status,DateTime? startDate,DateTime? endDate,int page,int pageSize,CancellationToken ct = default)
        {
            var query = _db.AdvanceRequests
                .Include(r => r.Items).ThenInclude(i => i.Parcela)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return requests.Select(r => new AdvanceRequestDetailDto
            {
                Id = r.Id,
                ClienteId = r.ClienteId,
                ContratoId = r.ContratoId,
                Status = r.Status,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                Items = r.Items.Select(i => new AdvanceRequestItemDto
                {
                    ParcelaId = i.ParcelaId,
                    ValorNaSolicitacao = i.ValorNaSolicitacao ?? 0
                }).ToList()
            });
        }

    }
}
