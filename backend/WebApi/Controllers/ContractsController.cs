using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] 
    public class ContractsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ContractsController(AppDbContext db)
        {
            _db = db;
        }

        public sealed class ContractListItemDto
        {
            public int id { get; set; }
            public string code { get; set; } = "";
            public int ownerId { get; set; }
            public string ownerName { get; set; } = "";
        }

        public sealed class ParcelaDto
        {
            public int id { get; set; }
            public int numero { get; set; }
            public decimal valor { get; set; }
            public DateTime dueDate { get; set; }
            public string status { get; set; } = "";
        }

        public sealed class ContractDetailDto
        {
            public int id { get; set; }
            public string code { get; set; } = "";
            public int ownerId { get; set; }
            public string ownerName { get; set; } = "";
            public IEnumerable<ParcelaDto> parcelas { get; set; } = Array.Empty<ParcelaDto>();
        }

        private static string? GetRole(ClaimsPrincipal u) =>
            u.FindFirstValue(ClaimTypes.Role) ?? u.FindFirstValue("role");

        private static int? GetClientId(ClaimsPrincipal u)
        {
            var claimClientId = u.Claims
                .FirstOrDefault(c => c.Type.Equals("clientId", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (int.TryParse(claimClientId, out var idParsed))
                return idParsed;

            var nameId = u.FindFirstValue(ClaimTypes.NameIdentifier) ?? u.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(nameId))
            {
                var digits = new string(nameId.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var id2)) return id2;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyContracts()
        {
            var role = GetRole(User);
            var clienteId = GetClientId(User);

            if (role == "APROVADOR")
                return Forbid(); 

            if (clienteId is null)
                return Unauthorized("clientId ausente no token.");

            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId.Value);
            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            var contratos = await _db.Contratos
                .Where(c => c.ClienteId == cliente.Id)
                .Select(c => new ContractListItemDto
                {
                    id = c.Id,
                    code = c.NomeContrato,
                    ownerId = c.ClienteId,
                    ownerName = cliente.Nome
                })
                .ToListAsync();

            return Ok(contratos);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllContracts()
        {
            var list = await _db.Contratos
                .Join(_db.Clientes, c => c.ClienteId, cl => cl.Id, (c, cl) => new ContractListItemDto
                {
                    id = c.Id,
                    code = c.NomeContrato,
                    ownerId = c.ClienteId,
                    ownerName = cl.Nome
                })
                .OrderBy(x => x.id)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}/detail")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var role = GetRole(User);
            var clienteId = GetClientId(User);

            var contrato = await _db.Contratos.FirstOrDefaultAsync(c => c.Id == id);
            if (contrato is null)
                return NotFound("Contrato não encontrado.");

            if (role == "CLIENTE")
            {
                if (clienteId is null) return Unauthorized("clientId ausente no token.");
                if (contrato.ClienteId != clienteId.Value) return Forbid();
            }
            else if (role != "APROVADOR")
            {
                return Forbid();
            }

            var parcelas = await _db.Parcelas
                .Where(p => p.ContratoId == id)
                .OrderBy(p => p.NumeroParcela)
                .Select(p => new ParcelaDto
                {
                    id = p.Id,
                    numero = p.NumeroParcela,
                    valor = p.Valor,
                    dueDate = p.Vencimento,
                    status = p.Status.ToString()
                })
                .ToListAsync();

            var ownerName = await _db.Clientes
                .Where(cl => cl.Id == contrato.ClienteId)
                .Select(cl => cl.Nome)
                .FirstOrDefaultAsync() ?? "—";

            var dto = new ContractDetailDto
            {
                id = contrato.Id,
                code = contrato.NomeContrato,
                ownerId = contrato.ClienteId,
                ownerName = ownerName,
                parcelas = parcelas
            };

            return Ok(dto);
        }
    }
}
