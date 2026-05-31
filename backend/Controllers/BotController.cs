using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using backend.Commands;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly CommandExecutor _commandExecutor;
        public BotController(CommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            await _commandExecutor.ExecuteAsync(update);
            return Ok();
        }
    }
}