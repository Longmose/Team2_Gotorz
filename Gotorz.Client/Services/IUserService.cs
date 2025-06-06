﻿using Gotorz.Shared.DTOs;
using static System.Net.WebRequestMethods;

namespace Gotorz.Client.Services
{
    /// <summary>
    /// Provides methods for accessing information about the currently authenticated user in the client.
    /// </summary>
    /// <remarks>
    /// Typically used in Blazor WebAssembly to retrieve and interact with identity and role information exposed by the backend.
    /// </remarks>
    /// <author>Eske</author>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves the full current user object from the backend.
        /// </summary>
        /// <returns>
        /// A <see cref="CurrentUserDto"/> containing user details and claims, or <c>null</c> if the user is not authenticated.
        /// </returns>
        Task<UserDto?> GetCurrentUserAsync();

        /// <summary>
        /// Determines whether the user is currently authenticated.
        /// </summary>
        /// <returns><c>true</c> if the user is authenticated; otherwise, <c>false</c>.</returns>
        Task<bool> IsLoggedInAsync();

        /// <summary>
        /// Checks whether the current user is in the specified role.
        /// </summary>
        /// <param name="role">The name of the role to check for (e.g., "admin", "sales").</param>
        /// <returns><c>true</c> if the user has the role; otherwise, <c>false</c>.</returns>
        Task<bool> IsUserInRoleAsync(string role);

        /// <summary>
        /// Retrieves the role of the currently authenticated user based on claims.
        /// </summary>
        /// <returns>
        /// The user's role as a string if available; otherwise, <c>null</c>.
        /// </returns>
        Task<string?> GetUserRoleAsync();

        /// <summary>
        /// Retrieves a user's profile information by their unique identifier
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>
        /// A <see cref="CurrentUserDto"/> containing user details if successful; otherwise, <c>null</c>.
        /// </returns>
        Task<UserDto?> GetUserByIdAsync(string userId);

        /// <summary>
        /// Retrieves the unique identifier of the current user.
        /// </summary>
        /// <returns>The user ID as a string, or <c>null</c> if not found.</returns>
        Task<string?> GetUserIdAsync();

        /// <summary>
        /// Retrieves the user's first name, or falls back to email if not available.
        /// </summary>
        /// <returns>The first name or email address of the current user.</returns>
        Task<string?> GetFirstNameAsync();

        /// <summary>
        /// Retrieves the user's last name.
        /// </summary>
        /// <returns>The last name of the current user, or <c>null</c> if not set.</returns>
        Task<string?> GetLastNameAsync();

        /// <summary>
        /// Retrieves the user's email address.
        /// </summary>
        /// <returns>The email address of the current user, or <c>null</c> if not set.</returns>
        Task<string?> GetEmailAsync();

        /// <summary>
        /// Registers a new user with the provided registration model.
        /// </summary>
        /// <param name="registerModel"></param>
        /// <returns></returns>
        Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// Authenticates the user with the provided login credentials.
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        Task<(bool Success, string? ErrorMessage)> LoginAsync(LoginDto loginModel);

        /// <summary>
        /// Signs out the currently logged-in user.
        /// </summary>
        /// <returns></returns>
        Task LogoutAsync();

        /// <summary>
        /// Deletes the user with the specified unique identifier.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<bool> DeleteUserAsync(string userId);

        /// <summary>
        /// Deletes the currently logged-in user.
        /// </summary>
        /// <returns></returns>
        Task<bool> DeleteCurrentUserAsync();

        /// <summary>
        /// Updates the profile of the currently logged-in user.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(UpdateUserDto dto);

        /// <summary>
        /// Updates the user information for a given user ID, and based on the given parameters.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        Task<(bool Success, string? ErrorMessage)> UpdateUserByIdAsync(string userId, UpdateUserDto dto);

        /// <summary>
        /// Retrieves a list of all users in the system.
        /// </summary>
        /// <returns></returns>
        Task<List<UserDto>> GetAllUsersAsync();

    }
}
