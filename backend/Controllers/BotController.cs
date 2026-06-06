using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using backend.Commands;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController(CommandExecutor commandExecutor) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
                return BadRequest();

            await commandExecutor.ExecuteAsync(update);
            return Ok();
        }
    }
}