// Services/ITodoService.cs
using TodoListApp.Models;

namespace TodoListApp.Services
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoItemDTO>> GetAllTodoItemsAsync(int page = 1, int pageSize = 50);
        Task<TodoItemDTO?> GetTodoItemAsync(int id);
        Task<TodoItemDTO> CreateTodoItemAsync(CreateTodoItemDTO createTodoItemDto);
        Task<TodoItemDTO?> UpdateTodoItemAsync(int id, UpdateTodoItemDTO updateTodoItemDto);
        Task<bool> DeleteTodoItemAsync(int id);
        Task<int> GetTotalCountAsync();
    }
}