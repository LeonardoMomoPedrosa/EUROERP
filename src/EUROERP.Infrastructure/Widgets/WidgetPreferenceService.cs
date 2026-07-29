using System.Data;
using Dapper;
using EUROERP.Application.Widgets;

namespace EUROERP.Infrastructure.Widgets;

public sealed class WidgetPreferenceService : IWidgetPreferenceService
{
    private readonly IDbConnection _connection;

    public WidgetPreferenceService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<string>> GetEnabledWidgetCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = "SELECT WidgetCode FROM USER_WIDGET WHERE UserId = @UserId ORDER BY WidgetCode";
        try
        {
            var list = (await _connection.QueryAsync<string>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            return list;
        }
        catch
        {
            // USER_WIDGET may not exist yet (see docs/sql/user_widget_create.sql) — dashboard shows no widgets.
            return Array.Empty<string>();
        }
    }

    public async Task SetEnabledWidgetsAsync(Guid userId, IReadOnlyList<string> widgetCodes, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM USER_WIDGET WHERE UserId = @UserId", new { UserId = userId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (widgetCodes == null || widgetCodes.Count == 0)
            return;

        const string insert = "INSERT INTO USER_WIDGET (UserId, WidgetCode) VALUES (@UserId, @WidgetCode)";
        foreach (var code in widgetCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(insert, new { UserId = userId, WidgetCode = code.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }

    public IReadOnlyList<WidgetDefinitionDto> GetAvailableWidgets()
    {
        return new[]
        {
            new WidgetDefinitionDto
            {
                Code = WidgetCodes.DailySales,
                Label = "Faturamento por dia",
                Description = "Gráfico de barras com o faturamento diário do mês atual (Loja, Site e ML). Dados em cache por 2 horas."
            },
            new WidgetDefinitionDto
            {
                Code = WidgetCodes.DailySalesAccumulated,
                Label = "Faturamento por dia (acumulado)",
                Description = "Gráfico de linhas com o faturamento acumulado dia a dia no mês atual. Dados em cache por 2 horas."
            },
            new WidgetDefinitionDto
            {
                Code = WidgetCodes.LastNfesGenerated,
                Label = "Últimas NFes geradas",
                Description = "Lista das últimas 7 NF-e emitidas, com links para abrir PDF e XML de cada nota."
            },
            new WidgetDefinitionDto
            {
                Code = WidgetCodes.Shortcuts,
                Label = "Atalhos",
                Description = "Links rápidos para telas do sistema que você mais usa (ex.: Nova OS, Cadastro de produtos, Imprimir NFe). Você escolhe quais atalhos exibir."
            }
        };
    }
}
