// Controllers/TodoItemsController.cs
using Microsoft.AspNetCore.Mvc;
using TodoListApp.Models;
using TodoListApp.Services;

namespace TodoListApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly ITodoService _todoService;
        private readonly ILogger<TodoItemsController> _logger;

        public TodoItemsController(ITodoService todoService, ILogger<TodoItemsController> logger)
        {
            _todoService = todoService;
            _logger = logger;
        }

        // GET: api/TodoItems?page=1&pageSize=50
        [HttpGet]
        [ResponseCache(Duration = 60)] // Cache for 1 minute
        public async Task<ActionResult<IEnumerable<TodoItemDTO>>> GetTodoItems([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (pageSize > 200) pageSize = 200; // Limit max page size for performance

            var items = await _todoService.GetAllTodoItemsAsync(page, pageSize);
            var count = await _todoService.GetTotalCountAsync();

            Response.Headers.Add("X-Total-Count", count.ToString());

            return Ok(items);
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<TodoItemDTO>> GetTodoItem(int id)
        {
            var todoItem = await _todoService.GetTodoItemAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            return todoItem;
        }

        // POST: api/TodoItems
        [HttpPost]
        public async Task<ActionResult<TodoItemDTO>> CreateTodoItem(CreateTodoItemDTO createTodoItemDto)
        {
            try
            {
                var todoItem = await _todoService.CreateTodoItemAsync(createTodoItemDto);

                return CreatedAtAction(
                    nameof(GetTodoItem),
                    new { id = todoItem.Id },
                    todoItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo item");
                return StatusCode(500, "An error occurred while creating the todo item.");
            }
        }

        // PUT: api/TodoItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoItem(int id, UpdateTodoItemDTO updateTodoItemDto)
        {
            try
            {
                var result = await _todoService.UpdateTodoItemAsync(id, updateTodoItemDto);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo item {Id}", id);
                return StatusCode(500, "An error occurred while updating the todo item.");
            }
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            var result = await _todoService.DeleteTodoItemAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}