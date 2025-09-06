using Microsoft.EntityFrameworkCore;
using WebApi.Models;

namespace WebApi.Data
{
    public static class DbSeeder
    {
        public static async Task Seed(IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(ct);

            var clientesSeed = new List<Cliente>
            {
                new() { Nome = "APROVADOR",        Email = "aprovador.demo@antecipafacil.com",   Senha = "123456" },
                new() { Nome = "Ana Sousa",        Email = "ana.sousa@antecipafacil.com",        Senha = "as123456" },
                new() { Nome = "João Ribeiro",     Email = "joao.ribeiro@antecipafacil.com",     Senha = "jr123456" },
                new() { Nome = "Regina Falange",   Email = "regina.falange@antecipafacil.com",   Senha = "rf123456" },
                new() { Nome = "Gabriel Alves",    Email = "gabriel.alves@antecipafacil.com",    Senha = "ga123456" },
                new() { Nome = "Lucas Machado",    Email = "lucas.machado@antecipafacil.com",    Senha = "lm123456" },
                new() { Nome = "Pedro Rocha",      Email = "pedro.rocha@antecipafacil.com",      Senha = "pr123456" },
                new() { Nome = "Renato Santos",    Email = "renato.santos@antecipafacil.com",    Senha = "rs123456" },
                new() { Nome = "Fátima Mohamad",   Email = "fatima.mohamad@antecipafacil.com",   Senha = "fm123456" },
                new() { Nome = "Ibrahim Mustafa",  Email = "ibrahim.mustafa@antecipafacil.com",  Senha = "im123456" },
                new() { Nome = "Hideki Suzuki",    Email = "hideki.suzuki@antecipafacil.com",    Senha = "hs123456" },
            };

            foreach (var cli in clientesSeed)
            {
                var existing = await db.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Email == cli.Email, ct);

                var isAprovador = string.Equals(cli.Nome, "APROVADOR", StringComparison.OrdinalIgnoreCase);

                if (existing is null)
                {
                    var novoUsuario = new Usuario { TipoUsuario = isAprovador ? TipoUsuario.APROVADOR : TipoUsuario.CLIENTE };
                    db.Usuarios.Add(novoUsuario);
                    await db.SaveChangesAsync(ct);

                    cli.Id = 0;
                    cli.UsuarioId = novoUsuario.Id;
                    db.Clientes.Add(cli);
                }
                else
                {
                    existing.Nome  = cli.Nome;
                    existing.Senha = cli.Senha;
                    db.Clientes.Update(existing);

                    var u = await db.Usuarios.FindAsync(new object?[] { existing.UsuarioId }, ct);
                    if (u is not null)
                        u.TipoUsuario = isAprovador ? TipoUsuario.APROVADOR : TipoUsuario.CLIENTE;
                }
            }
            await db.SaveChangesAsync(ct);

            var clientes = await db.Clientes.AsNoTracking().ToListAsync(ct);
            var agora = DateTime.UtcNow;

            foreach (var c in clientes)
            {
                if (string.Equals(c.Nome, "APROVADOR", StringComparison.OrdinalIgnoreCase))
                    continue;

                var contratosDoCliente = await db.Contratos
                    .Where(x => x.ClienteId == c.Id)
                    .OrderBy(x => x.Id)
                    .ToListAsync(ct);

                var existentes = contratosDoCliente.Count;
                var faltantes = Math.Max(0, 6 - existentes);

                static string Iniciais(string nome) =>
                    string.Join("", (nome ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => char.ToUpperInvariant(p[0])));

                var iniciais = Iniciais(c.Nome);

                for (int i = 1; i <= faltantes; i++)
                {
                    var seq = existentes + i;
                    var code = $"{iniciais}_{c.Id}_CONTRATO_{seq}";

                    var contrato = new Contrato
                    {
                        NomeContrato       = code,
                        ClienteId          = c.Id,
                        Status             = ContractStatus.PENDENTE,
                        DataInsercao       = agora,
                        DataAlteracao      = agora,
                        NumeroParcelas     = 12,
                        VencimentoContrato = agora.AddMonths(12),
                    };

                    db.Contratos.Add(CorrigirNulos(contrato));
                    await db.SaveChangesAsync(ct);

                    var parcelas = new List<Parcela>();
                    for (int np = 1; np <= 12; np++)
                    {
                        DateTime venc;
                        var status = InstallmentStatus.A_VENCER;

                        if (np == 1)
                        {
                            venc = agora.AddDays(-10);
                            status = InstallmentStatus.PAGO;
                        }
                        else if (np == 2)
                        {
                            venc = agora.AddDays(20);
                        }
                        else if (np == 3)
                        {
                            venc = agora.AddDays(45);
                        }
                        else
                        {
                            venc = new DateTime(agora.Year, agora.Month, 10, 0, 0, 0, DateTimeKind.Utc).AddMonths(np - 1);
                        }

                        parcelas.Add(new Parcela
                        {
                            ContratoId     = contrato.Id,
                            ClienteId      = c.Id,
                            NumeroParcela  = np,
                            Valor          = 1600m,
                            Vencimento     = venc,
                            Status         = status
                        });
                    }

                    db.Parcelas.AddRange(parcelas);
                    await db.SaveChangesAsync(ct);
                }
            }

            var todosContratos = await db.Contratos
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync(ct);

            var primeiroPorCliente = todosContratos
                .GroupBy(c => c.ClienteId)
                .Select(g => g.First().Id)
                .ToHashSet();

            foreach (var contratoId in primeiroPorCliente)
            {
                var p4 = await db.Parcelas.FirstOrDefaultAsync(
                    x => x.ContratoId == contratoId && x.NumeroParcela == 4, ct);

                if (p4 != null && p4.Status == InstallmentStatus.A_VENCER)
                {
                    p4.Status = InstallmentStatus.AGUARDANDO_APROVACAO;
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        private static Contrato CorrigirNulos(Contrato c)
        {
            c.NomeContrato ??= $"CONTRATO_{c.ClienteId}_{DateTime.UtcNow.Ticks}";
            return c;
        }
    }
}
