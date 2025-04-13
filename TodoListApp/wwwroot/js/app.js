document.addEventListener('DOMContentLoaded', function () {
    // API base URL - update this to match your deployment
    const API_URL = '/api/TodoItems';

    // UI elements
    const loadingIndicator = document.getElementById('loadingIndicator');
    const todoList = document.getElementById('todoList');
    const noTasksMessage = document.getElementById('noTasksMessage');
    const addTodoForm = document.getElementById('addTodoForm');
    const editTodoForm = document.getElementById('editTodoForm');
    const currentPageEl = document.getElementById('currentPage');
    const totalPagesEl = document.getElementById('totalPages');
    const prevPageBtn = document.getElementById('prevPage');
    const nextPageBtn = document.getElementById('nextPage');

    // Modal elements
    const editTaskModal = new bootstrap.Modal(document.getElementById('editTaskModal'));
    const saveTaskBtn = document.getElementById('saveTaskBtn');
    const deleteTaskBtn = document.getElementById('deleteTaskBtn');

    // Pagination state
    let currentPage = 1;
    let pageSize = 50;
    let totalItems = 0;
    let totalPages = 1;

    // Show/hide loading indicator
    function showLoading() {
        loadingIndicator.classList.remove('hidden');
    }

    function hideLoading() {
        loadingIndicator.classList.add('hidden');
    }

    // Error handling
    function handleError(error) {
        console.error('Error:', error);
        hideLoading();
        alert('An error occurred: ' + error.message);
    }

    // Format date for display
    function formatDate(dateString) {
        if (!dateString) return 'No due date';
        const date = new Date(dateString);
        return date.toLocaleString();
    }

    // Get priority text and class
    function getPriorityInfo(priority) {
        switch (parseInt(priority)) {
            case 1: return { text: 'Low', class: 'priority-low' };
            case 2: return { text: 'Medium', class: 'priority-medium' };
            case 3: return { text: 'High', class: 'priority-high' };
            default: return { text: 'Medium', class: 'priority-medium' };
        }
    }

    // Load all todo items
    async function loadTodoItems() {
        showLoading();
        try {
            // Add cache-busting query parameter to ensure fresh data
            const response = await fetch(`${API_URL}?page=${currentPage}&pageSize=${pageSize}&_=${new Date().getTime()}`);

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();

            // Get total count from headers
            totalItems = parseInt(response.headers.get('X-Total-Count') || '0');
            totalPages = Math.ceil(totalItems / pageSize) || 1;

            // Update pagination UI
            currentPageEl.textContent = currentPage;
            totalPagesEl.textContent = totalPages;
            prevPageBtn.disabled = currentPage <= 1;
            nextPageBtn.disabled = currentPage >= totalPages;

            // Update list UI
            todoList.innerHTML = '';

            if (data.length === 0) {
                noTasksMessage.style.display = 'block';
            } else {
                noTasksMessage.style.display = 'none';

                data.forEach(item => {
                    const priorityInfo = getPriorityInfo(item.priority);
                    const dueDate = item.dueDate ? new Date(item.dueDate) : null;
                    const isPastDue = dueDate && dueDate < new Date() && !item.isCompleted;

                    const listItem = document.createElement('div');
                    listItem.className = `list-group-item todo-item d-flex justify-content-between align-items-center ${priorityInfo.class} ${item.isCompleted ? 'completed' : ''}`;
                    listItem.dataset.id = item.id;

                    listItem.innerHTML = `
                        <div>
                            <div class="d-flex align-items-center">
                                <div class="form-check me-2">
                                    <input class="form-check-input toggle-complete" type="checkbox" value="" id="check${item.id}" ${item.isCompleted ? 'checked' : ''}>
                                </div>
                                <div>
                                    <h5 class="mb-1">${item.title}</h5>
                                    <p class="mb-1 text-muted">${item.description || 'No description'}</p>
                                </div>
                            </div>
                        </div>
                        <div class="text-end">
                            <span class="badge bg-${priorityInfo.text.toLowerCase()}">${priorityInfo.text}</span>
                            <div class="text-muted ${isPastDue ? 'text-danger' : ''}">${formatDate(item.dueDate)}</div>
                            <button class="btn btn-sm btn-outline-primary edit-btn mt-2">Edit</button>
                        </div>
                    `;

                    todoList.appendChild(listItem);
                });

                // Add event listeners to the new elements
                document.querySelectorAll('.toggle-complete').forEach(checkbox => {
                    checkbox.addEventListener('change', toggleComplete);
                });

                document.querySelectorAll('.edit-btn').forEach(button => {
                    button.addEventListener('click', editTask);
                });
            }
        } catch (error) {
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Add a new todo item
    async function addTodoItem(event) {
        event.preventDefault();
        showLoading();

        try {
            const newItem = {
                title: document.getElementById('title').value,
                description: document.getElementById('description').value,
                dueDate: document.getElementById('dueDate').value || null,
                priority: parseInt(document.getElementById('priority').value)
            };

            const response = await fetch(API_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(newItem)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            // Reset form
            addTodoForm.reset();

            // Reload the list to show the new item
            await loadTodoItems();

        } catch (error) {
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Toggle complete status
    async function toggleComplete(event) {
        const checkbox = event.target;
        const listItem = checkbox.closest('.todo-item');
        const id = listItem.dataset.id;

        showLoading();

        try {
            const response = await fetch(`${API_URL}/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    isCompleted: checkbox.checked
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            // Update UI
            if (checkbox.checked) {
                listItem.classList.add('completed');
            } else {
                listItem.classList.remove('completed');
            }

        } catch (error) {
            // Revert the checkbox state on error
            checkbox.checked = !checkbox.checked;
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Open edit modal for a task
    async function editTask(event) {
        const button = event.target;
        const listItem = button.closest('.todo-item');
        const id = listItem.dataset.id;

        showLoading();

        try {
            const response = await fetch(`${API_URL}/${id}`);

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const item = await response.json();

            // Populate the edit form
            document.getElementById('editId').value = item.id;
            document.getElementById('editTitle').value = item.title;
            document.getElementById('editDescription').value = item.description || '';
            document.getElementById('editIsCompleted').checked = item.isCompleted;
            document.getElementById('editPriority').value = item.priority;

            // Format date for datetime-local input
            if (item.dueDate) {
                const dueDate = new Date(item.dueDate);
                const year = dueDate.getFullYear();
                const month = String(dueDate.getMonth() + 1).padStart(2, '0');
                const day = String(dueDate.getDate()).padStart(2, '0');
                const hours = String(dueDate.getHours()).padStart(2, '0');
                const minutes = String(dueDate.getMinutes()).padStart(2, '0');

                document.getElementById('editDueDate').value = `${year}-${month}-${day}T${hours}:${minutes}`;
            } else {
                document.getElementById('editDueDate').value = '';
            }

            // Show the modal
            editTaskModal.show();

        } catch (error) {
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Update a task
    async function updateTask() {
        const id = document.getElementById('editId').value;

        showLoading();

        try {
            const updatedItem = {
                title: document.getElementById('editTitle').value,
                description: document.getElementById('editDescription').value,
                isCompleted: document.getElementById('editIsCompleted').checked,
                dueDate: document.getElementById('editDueDate').value || null,
                priority: parseInt(document.getElementById('editPriority').value)
            };

            const response = await fetch(`${API_URL}/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updatedItem)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            // Close the modal
            editTaskModal.hide();

            // Reload the list to show the updated item
            await loadTodoItems();

        } catch (error) {
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Delete a task
    async function deleteTask() {
        const id = document.getElementById('editId').value;

        if (!confirm('Are you sure you want to delete this task?')) {
            return;
        }

        showLoading();

        try {
            const response = await fetch(`${API_URL}/${id}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            // Close the modal
            editTaskModal.hide();

            // Reload the list
            await loadTodoItems();

        } catch (error) {
            handleError(error);
        } finally {
            hideLoading();
        }
    }

    // Handle pagination
    function handlePrevPage() {
        if (currentPage > 1) {
            currentPage--;
            loadTodoItems();
        }
    }

    function handleNextPage() {
        if (currentPage < totalPages) {
            currentPage++;
            loadTodoItems();
        }
    }

    // Set up event listeners
    addTodoForm.addEventListener('submit', addTodoItem);
    saveTaskBtn.addEventListener('click', updateTask);
    deleteTaskBtn.addEventListener('click', deleteTask);
    prevPageBtn.addEventListener('click', handlePrevPage);
    nextPageBtn.addEventListener('click', handleNextPage);

    // Load initial data
    loadTodoItems();
});
