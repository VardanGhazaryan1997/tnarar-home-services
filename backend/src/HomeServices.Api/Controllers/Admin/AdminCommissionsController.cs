using HomeServices.Api.Authorization;
using HomeServices.Application.Commissions;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Commission rates, partners' weekly statements and the settlements recorded against them.</summary>
[ApiController]
[Route("api/v1/admin")]
public sealed class AdminCommissionsController : ControllerBase
{
    public sealed record PercentBody(decimal Percent);

    public sealed record SettlementBody(int Amount, SettlementMethod Method, DateOnly PaidOn, string? Reference);

    /// <summary>The default rate and every category with its own and its effective rate.</summary>
    [HttpGet("commission-rates")]
    [HasPermission(Permissions.CommissionsView)]
    public Task<CommissionRatesDto> Rates([FromServices] IQueryHandler<GetCommissionRates, CommissionRatesDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetCommissionRates(), cancellationToken);

    /// <summary>Changes the rate used where no category has its own (0–50, at most 2 decimals).</summary>
    [HttpPut("commission-rates/default")]
    [HasPermission(Permissions.CommissionsManage)]
    public Task<CommissionRatesDto> SetDefault(
        PercentBody body, [FromServices] ICommandHandler<SetDefaultCommissionRate, CommissionRatesDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetDefaultCommissionRate(body.Percent), cancellationToken);

    /// <summary>Gives a category its own rate; it also applies to subcategories without one.</summary>
    [HttpPut("commission-rates/categories/{categoryId:guid}")]
    [HasPermission(Permissions.CommissionsManage)]
    public Task<CommissionRatesDto> SetCategory(
        Guid categoryId, PercentBody body, [FromServices] ICommandHandler<SetCategoryCommissionRate, CommissionRatesDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCategoryCommissionRate(categoryId, body.Percent), cancellationToken);

    /// <summary>Removes a category's own rate, so it falls back to its parent's or the default.</summary>
    [HttpDelete("commission-rates/categories/{categoryId:guid}")]
    [HasPermission(Permissions.CommissionsManage)]
    public Task<CommissionRatesDto> ClearCategory(
        Guid categoryId, [FromServices] ICommandHandler<SetCategoryCommissionRate, CommissionRatesDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCategoryCommissionRate(categoryId, null), cancellationToken);

    /// <summary>Statements, filtered by <c>?filter=Open|Overdue|Paid</c> and <c>?partnerId</c>.</summary>
    [HttpGet("commission-statements")]
    [HasPermission(Permissions.CommissionsView)]
    public Task<PagedResult<AdminStatementDto>> Statements(
        [FromServices] IQueryHandler<GetAdminStatements, PagedResult<AdminStatementDto>> handler,
        [FromQuery] StatementFilter? filter,
        [FromQuery] Guid? partnerId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminStatements(filter, partnerId, page ?? 1, pageSize ?? GetAdminStatements.DefaultPageSize), cancellationToken);

    /// <summary>One statement with its orders and settlements.</summary>
    [HttpGet("commission-statements/{id:guid}")]
    [HasPermission(Permissions.CommissionsView)]
    public Task<AdminStatementDetailDto> Statement(
        Guid id, [FromServices] IQueryHandler<GetAdminStatement, AdminStatementDetailDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminStatement(id), cancellationToken);

    /// <summary>Records money the partner paid; a fully paid statement lifts a pause for debt.</summary>
    [HttpPost("commission-statements/{id:guid}/settlements")]
    [HasPermission(Permissions.CommissionsManage)]
    public Task<AdminStatementDetailDto> Settle(
        Guid id, SettlementBody body, [FromServices] ICommandHandler<RecordSettlement, AdminStatementDetailDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RecordSettlement(id, body.Amount, body.Method, body.PaidOn, body.Reference), cancellationToken);
}
