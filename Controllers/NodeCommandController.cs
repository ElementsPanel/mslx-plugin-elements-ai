using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;
using MSLX.SDK.Models;

namespace MSLX.Plugin.ElementsAI.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[AiExceptionFilter]
[Route(NodeCommandService.Route)]
public sealed class NodeCommandController : ControllerBase
{
    // MSLX authenticates both local users and requests from a linked master node.
    // This endpoint executes locally and cannot forward a request to another node.
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NodeCommandResult>>> Execute(
        [FromBody] NodeCommandRequest request, CancellationToken cancellationToken)
    {
        NodeCommandService.RequireAdmin(User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        var result = await NodeCommandService.ExecuteLocalAsync(request, cancellationToken);
        return Ok(new ApiResponse<NodeCommandResult> { Code = 200, Message = "success", Data = result });
    }
}
