using Microsoft.AspNetCore.Mvc;
using TodoListApp.Models;
using TodoListApp.Services;
using Microsoft.Extensions.Caching.Memory; // Add this namespace

namespace TodoListApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly ITodoService _todoService;
        private readonly ILogger<TodoItemsController> _logger;
        private readonly IMemoryCache _cache; // Add IMemoryCache

        // Inject IMemoryCache into the constructor
        public TodoItemsController(ITodoService todoService, ILogger<TodoItemsController> logger, IMemoryCache cache)
        {
            _todoService = todoService;
            _logger = logger;
            _cache = cache; // Assign IMemoryCache to the _cache field
        }

        // GET: api/TodoItems?page=1&pageSize=50
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // Disable caching
        public async Task<ActionResult<IEnumerable<TodoItemDTO>>> GetTodoItems([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (pageSize > 200) pageSize = 200; // Limit max page size for performance

                var items = await _todoService.GetAllTodoItemsAsync(page, pageSize);
                var count = await _todoService.GetTotalCountAsync();

                Response.Headers.Add("X-Total-Count", count.ToString());

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching todo items");
                return StatusCode(500, "An error occurred while fetching the todo items.");
            }
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // Disable caching
        public async Task<ActionResult<TodoItemDTO>> GetTodoItem(int id)
        {
            try
            {
                var todoItem = await _todoService.GetTodoItemAsync(id);

                if (todoItem == null)
                {
                    _logger.LogWarning("Todo item with ID {Id} not found", id);
                    return NotFound($"Todo item with ID {id} not found.");
                }

                return Ok(todoItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching todo item with ID {Id}", id);
                return StatusCode(500, "An error occurred while fetching the todo item.");
            }
        }

        // POST: api/TodoItems
        [HttpPost]
        public async Task<ActionResult<TodoItemDTO>> CreateTodoItem(CreateTodoItemDTO createTodoItemDto)
        {
            try
            {
                if (createTodoItemDto == null)
                {
                    return BadRequest("Todo item data is required.");
                }

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
                if (updateTodoItemDto == null)
                {
                    return BadRequest("Todo item update data is required.");
                }

                var result = await _todoService.UpdateTodoItemAsync(id, updateTodoItemDto);

                if (result == null)
                {
                    _logger.LogWarning("Todo item with ID {Id} not found for update", id);
                    return NotFound($"Todo item with ID {id} not found.");
                }

                // Refresh cache for this updated item
                _cache.Remove($"TodoItem_{id}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo item with ID {Id}", id);
                return StatusCode(500, "An error occurred while updating the todo item.");
            }
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            try
            {
                var result = await _todoService.DeleteTodoItemAsync(id);

                if (!result)
                {
                    _logger.LogWarning("Todo item with ID {Id} not found for deletion", id);
                    return NotFound($"Todo item with ID {id} not found.");
                }

                // Invalidate the cache for this deleted item
                _cache.Remove($"TodoItem_{id}");

                return NoContent(); // Successfully deleted
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo item with ID {Id}", id);
                return StatusCode(500, "An error occurred while deleting the todo item.");
            }
        }
    }
}
