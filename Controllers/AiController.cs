using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MSLX.Plugin.ElementsAI.Models;
using MSLX.Plugin.ElementsAI.Services;
using MSLX.SDK;
using MSLX.SDK.Models;

namespace MSLX.Plugin.ElementsAI.Controllers;

[ApiController]
[Authorize]
[AiExceptionFilter]
[Route("api/plugins/mslx-plugin-elements-ai/ai")]
public sealed class AiController(AiDataStore store, AiChatService chat, TaskProgressService tasks) : ControllerBase
{
    [HttpGet("tasks/{taskId}")]
    public ActionResult<ApiResponse<AiTaskProgress>> TaskProgress(string taskId) =>
        Ok(Packet(tasks.Read(taskId, CurrentUser())));

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<AiStatus>>> Status(CancellationToken cancellationToken)
    {
        return Ok(Packet(await store.GetStatusAsync(CurrentUser(), cancellationToken)));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<bool>>> SavePreferences(
        [FromBody] ChatPreferences preferences,
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        await store.SavePreferencesAsync(user.UserId, preferences, cancellationToken);
        return Ok(Packet(true));
    }

    [HttpPut("models")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveModel(
        [FromBody] ModelInput input,
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        await store.SavePersonalModelAsync(user.UserId, input, cancellationToken);
        return Ok(Packet(true));
    }

    [HttpDelete("models/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteModel(
        string id,
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        await store.DeletePersonalModelAsync(user.UserId, id, cancellationToken);
        return Ok(Packet(true));
    }

    [HttpGet("presets")]
    public async Task<ActionResult<ApiResponse<List<ModelOption>>>> Presets(CancellationToken cancellationToken)
    {
        RequireAdmin();
        return Ok(Packet(await store.ListPresetsAsync(cancellationToken)));
    }

    [HttpPut("presets")]
    public async Task<ActionResult<ApiResponse<bool>>> SavePreset(
        [FromBody] ModelInput input,
        CancellationToken cancellationToken)
    {
        RequireAdmin();
        await store.SavePresetAsync(input, cancellationToken);
        return Ok(Packet(true));
    }

    [HttpDelete("presets/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePreset(
        string id,
        CancellationToken cancellationToken)
    {
        RequireAdmin();
        await store.DeletePresetAsync(id, cancellationToken);
        return Ok(Packet(true));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<List<ConversationSummary>>>> Conversations(
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        return Ok(Packet(await chat.ListHistoryAsync(user, cancellationToken)));
    }

    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ApiResponse<ConversationDetail>>> Conversation(
        string id,
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        return Ok(Packet(await chat.ReadHistoryAsync(user, id, cancellationToken)));
    }

    [HttpDelete("conversations")]
    public async Task<ActionResult<ApiResponse<int>>> DeleteConversations(
        [FromBody] DeleteConversationsInput input,
        CancellationToken cancellationToken)
    {
        var user = CurrentUser();
        return Ok(Packet(await chat.DeleteHistoryAsync(user, input.Ids, cancellationToken)));
    }

    [HttpPost("chat")]
    public async Task Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        await chat.StreamChatAsync(request, CurrentUser(), Response, cancellationToken);
    }

    [HttpPost("approvals/{id}")]
    public ActionResult<ApiResponse<bool>> Approval(string id, [FromBody] ApprovalInput input)
    {
        if (!chat.RespondToApproval(CurrentUser(), id, input.Approved))
            return NotFound(Failure<bool>("操作确认不存在、已结束或账号权限已变化。", 404));
        return Ok(Packet(true));
    }

    [HttpPost("questions/{id}")]
    public ActionResult<ApiResponse<bool>> Question(string id, [FromBody] QuestionInput input)
    {
        if (!chat.RespondToQuestion(CurrentUser(), id, input.Answer, Request.Headers["x-mslfrp-token"].FirstOrDefault()))
            return NotFound(Failure<bool>("交互问题不存在、已结束或账号权限已变化。", 404));
        return Ok(Packet(true));
    }

    [HttpGet("interactions")]
    public ActionResult<ApiResponse<InteractionStatus>> Interactions() => Ok(Packet(chat.GetInteractionStatus(CurrentUser())));

    private UserContext CurrentUser()
    {
        var claimId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(claimId)) throw new UnauthorizedAccessException("无法识别当前账号。");
        var current = SDK.MSLX.Config.Users.GetUserById(claimId)
                      ?? throw new UnauthorizedAccessException("当前账号不存在。");
        var admin = current.Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        var scope = UserContext.AuthorizationScope(current.Id, current.Role, current.Resources);
        return new UserContext(current.Id, admin, scope);
    }

    private void RequireAdmin()
    {
        if (!CurrentUser().IsAdmin) throw new UnauthorizedAccessException("仅管理员可以管理预设模型。");
    }

    private static ApiResponse<T> Packet<T>(T data) => new() { Code = 200, Message = "success", Data = data };
    private static ApiResponse<T> Failure<T>(string message, int code) => new() { Code = code, Message = message };
}

public sealed class AiExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.HttpContext.Response.HasStarted) return;
        var (status, message) = context.Exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, context.Exception.Message),
            AiValidationException => (StatusCodes.Status400BadRequest, context.Exception.Message),
            ToolException => (StatusCodes.Status400BadRequest, context.Exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Elements AI 请求失败。")
        };
        context.Result = new ObjectResult(new ApiResponse<object>
        {
            Code = status,
            Message = message,
            Data = null
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
