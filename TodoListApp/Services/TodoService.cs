// Services/TodoService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TodoListApp.Data;
using TodoListApp.Models;

namespace TodoListApp.Services
{
    public class TodoService : ITodoService
    {
        private readonly TodoContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TodoService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public TodoService(TodoContext context, IMemoryCache cache, ILogger<TodoService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<TodoItemDTO>> GetAllTodoItemsAsync(int page = 1, int pageSize = 50)
        {
            string cacheKey = $"TodoItems_Page{page}_Size{pageSize}";

            if (!_cache.TryGetValue(cacheKey, out List<TodoItemDTO> items))
            {
                _logger.LogInformation("Fetching todo items from database for page {Page}", page);

                items = await _context.TodoItems
                    .AsNoTracking()
                    .OrderByDescending(t => t.DueDate)
                    .ThenByDescending(t => t.Priority)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TodoItemDTO
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        IsCompleted = t.IsCompleted,
                        DueDate = t.DueDate,
                        Priority = t.Priority
                    })
                    .ToListAsync();

                _cache.Set(cacheKey, items, _cacheDuration);
            }

            return items;
        }

        public async Task<TodoItemDTO?> GetTodoItemAsync(int id)
        {
            string cacheKey = $"TodoItem_{id}";

            if (!_cache.TryGetValue(cacheKey, out TodoItemDTO? item))
            {
                var todoItem = await _context.TodoItems.FindAsync(id);

                if (todoItem == null)
                {
                    return null;
                }

                item = new TodoItemDTO
                {
                    Id = todoItem.Id,
                    Title = todoItem.Title,
                    Description = todoItem.Description,
                    IsCompleted = todoItem.IsCompleted,
                    DueDate = todoItem.DueDate,
                    Priority = todoItem.Priority
                };

                _cache.Set(cacheKey, item, _cacheDuration);
            }

            return item;
        }

        public async Task<TodoItemDTO> CreateTodoItemAsync(CreateTodoItemDTO createTodoItemDto)
        {
            var todoItem = new TodoItem
            {
                Title = createTodoItemDto.Title,
                Description = createTodoItemDto.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                DueDate = createTodoItemDto.DueDate,
                Priority = createTodoItemDto.Priority
            };

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            // Invalidate relevant cache entries
            _cache.Remove("TodoItemsCount");

            return new TodoItemDTO
            {
                Id = todoItem.Id,
                Title = todoItem.Title,
                Description = todoItem.Description,
                IsCompleted = todoItem.IsCompleted,
                DueDate = todoItem.DueDate,
                Priority = todoItem.Priority
            };
        }

        public async Task<TodoItemDTO?> UpdateTodoItemAsync(int id, UpdateTodoItemDTO updateTodoItemDto)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return null;
            }

            if (updateTodoItemDto.Title != null)
                todoItem.Title = updateTodoItemDto.Title;

            if (updateTodoItemDto.Description != null)
                todoItem.Description = updateTodoItemDto.Description;

            if (updateTodoItemDto.IsCompleted.HasValue)
                todoItem.IsCompleted = updateTodoItemDto.IsCompleted.Value;

            if (updateTodoItemDto.DueDate.HasValue)
                todoItem.DueDate = updateTodoItemDto.DueDate.Value;

            if (updateTodoItemDto.Priority.HasValue)
                todoItem.Priority = updateTodoItemDto.Priority.Value;

            await _context.SaveChangesAsync();

            // Invalidate cache for this item
            _cache.Remove($"TodoItem_{id}");

            // Return updated item
            return new TodoItemDTO
            {
                Id = todoItem.Id,
                Title = todoItem.Title,
                Description = todoItem.Description,
                IsCompleted = todoItem.IsCompleted,
                DueDate = todoItem.DueDate,
                Priority = todoItem.Priority
            };
        }

        public async Task<bool> DeleteTodoItemAsync(int id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return false;
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove($"TodoItem_{id}");
            _cache.Remove("TodoItemsCount");

            return true;
        }

        public async Task<int> GetTotalCountAsync()
        {
            if (!_cache.TryGetValue("TodoItemsCount", out int count))
            {
                count = await _context.TodoItems.CountAsync();
                _cache.Set("TodoItemsCount", count, _cacheDuration);
            }

            return count;
        }
    }
}